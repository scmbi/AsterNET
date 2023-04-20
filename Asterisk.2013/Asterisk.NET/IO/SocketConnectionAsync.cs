using System.IO;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System;
using System.Collections.Generic;
using Sufficit.Asterisk;
using AsterNET.Manager;
using Microsoft.Extensions.Logging;
using AsterNET.FastAGI;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices.ComTypes;

namespace AsterNET.IO
{
	/// <summary>
	/// Socket connection to asterisk.
	/// </summary>
	public class SocketConnectionAsync : TcpClient, ISocketConnection
    {
		public const int RECEIVE_TIMEOUT = 10000;
        public const int SEND_TIMEOUT = 5000;

        public const string AGI_REPLY_HANGUP = "HANGUP";

        #region DISPOSING

        /// <summary>
        /// Monitor dispose event
        /// </summary>
        public event EventHandler<bool>? OnDisposing;

		private bool _disposed;

        protected override void Dispose(bool disposing)
        {
			if (!_disposed)
			{
				_disposed = true;
				_logger.LogTrace("disposing");

				// invoking before dispose internals, to grant availability
				OnDisposing?.Invoke(this, disposing);

				_hgcts.Cancel();
				_hgcts.Dispose();

				// Avoid new remote commands
				if (writer != null)
					writer.Dispose();

				// Closing client after disconnect remote
				if (Client != null)
				{
					if (Client.Connected)
						Client.Close();
				}
			}
            base.Dispose(disposing);
        }

		#endregion
		#region HANGUP CONTROL

        /// <summary>
        ///  Indicates that hangup message is received
        /// </summary>
        public bool IsHangUp { get; internal set; }

        public event EventHandler? OnHangUp;

        protected void HangUpTrigger()
        {
            IsHangUp = true;
			_logger.LogTrace("hangup");
            OnHangUp?.Invoke(this, EventArgs.Empty);
        }

		#endregion

		public Encoding Encoding { get; internal set; } 
			= Encoding.ASCII; // default value

        private readonly ILogger _logger;
		private readonly BinaryWriter writer;
		private readonly CancellationTokenSource _hgcts;
		private readonly Queue<string?> _queue;
		private bool initial;

        #region Constructor - SocketConnection(socket) 

        public SocketConnectionAsync(Socket socket, Encoding? encoding = null, ILogger? logger = null)   
        {
			Client = socket;

            if (encoding != null)
                Encoding = encoding;

            this.writer = new BinaryWriter(this.GetStream(), Encoding);
			
			SendTimeout = SEND_TIMEOUT;

            _logger = logger ?? new LoggerFactory().CreateLogger<ISocketConnection>();

			_queue = new Queue<string?>();
            _hgcts = new CancellationTokenSource();			
        }

        public SocketConnectionAsync(string host, int port, int receiveBufferSize = 0, Encoding? encoding = null, ILogger? logger = null) : base(host, port) 
		{
			if (receiveBufferSize > 0)
				ReceiveBufferSize = receiveBufferSize;
			
			if (encoding != null)
				Encoding = encoding;

            this.writer = new BinaryWriter(this.GetStream(), Encoding);

            SendTimeout = SEND_TIMEOUT;

            _logger = logger ?? new LoggerFactory().CreateLogger<ISocketConnection>();

            initial = true;
		}

        #endregion

        public bool Initial
		{
			get { return initial; }
			set { initial = value; }
		}

		private bool? isRemoteRequest;

		public bool IsRemoteRequest
		{
			get
			{
                if(isRemoteRequest.HasValue) 
					return isRemoteRequest.Value;

                var remote = Client.RemoteEndPoint;
				var local = Client.LocalEndPoint;

                isRemoteRequest = SocketExtensions.IsRemoteRequest(remote, local);
				_logger.LogDebug("comparing for remote => (remote: {remote}, local: {local}) => {result}", remote, local, isRemoteRequest);

				return isRemoteRequest.Value;
            }
		}

        public bool IsConnected()
            => !_disposed && Connected;

        #region LocalAddress 
        public IPAddress LocalAddress
		{
			get
			{
				return ((IPEndPoint)(Client.LocalEndPoint)).Address;
			}
		}
		#endregion

		#region LocalPort 
		public int LocalPort
		{
			get
			{
				return ((IPEndPoint)(Client.LocalEndPoint)).Port;
			}
		}
		#endregion

		#region RemoteAddress 
		public IPAddress RemoteAddress
		{
			get
			{
				return ((IPEndPoint)(Client.RemoteEndPoint)).Address;
			}
		}
		#endregion

		#region RemotePort 
		public int RemotePort
		{
			get
			{
				return ((IPEndPoint)(Client.LocalEndPoint)).Port;
			}
		}
		#endregion

		#region ReadLine()

		/// <summary>
		/// Reads a line of text from the socket connection. The current thread is
		/// blocked until either the next line is received or an IOException
		/// encounters.
		/// </summary>
		/// <returns>the line of text received excluding any newline character</returns>
		/// <throws>  IOException if the connection has been closed. </throws>
		public string? ReadLine()
			=> InternalReadLine();

        public IEnumerable<string> ReadLines(int? timeoutms = null)
        {
            string? line = string.Empty;
            do
            {                
                line = InternalReadLine(timeoutms);
				if (line == null) break;               

				// returning
                yield return line;
            } while (Reading(Peek(), line));			
        }

		public int Peek()
		{
            if (_queue.Count > 0)
                return _queue.Count;
			return -1;
        }

		private string? InternalReadLine(int? timeoutms = null)
		{
			if (timeoutms == null || timeoutms < 0)
				timeoutms = RECEIVE_TIMEOUT;

            var stopwatch = Stopwatch.StartNew();
			var limit = TimeSpan.FromMilliseconds(timeoutms.Value);
            while (stopwatch.Elapsed < limit)
            {
				if (_queue.Count > 0)				
					return _queue.Dequeue();                
            }

            return null;
		}

		/// <summary>
		/// starts listening
		/// </summary>
		public SocketConnectionAsync Start()
		{
			_ = ListenerAsync(_hgcts.Token).ConfigureAwait(false);
			return this;
		}			 

        /// <summary>
        /// Reads all buffered content into lines and enqueue then
        /// </summary>
        private async Task ListenerAsync(CancellationToken cancellationToken)
        {
			// returning for default context continues
            await Task.Yield();

            using (var reader = new StreamReader(GetStream(), Encoding))
			{
				while (!cancellationToken.IsCancellationRequested)
				{
					// checking if was disconnected
					try { if (reader.EndOfStream) continue; } catch { break; }
					try { 
						string? line = await reader.ReadLineAsync();

						// turning blank to null, to facilitate
						if (string.IsNullOrWhiteSpace(line)) 
							line = null;

						// line null means end of a multiline command like the header
						_logger.LogTrace("enqueue: {line}", line ?? "null");

						if (line == AGI_REPLY_HANGUP)
						{
							HangUpTrigger();
							continue;
						}

						// enqueing
						_queue.Enqueue(line);
                    }
                    catch(Exception ex) {
						_logger.LogError(ex, "error on reading");
						break; 
					}
                }
			}
        }

        /// <summary>
        /// Continue if still contains bytes or received a 100 status code
        /// </summary>
        private bool Reading(int peek, string? line)
		{
			// returns true, continue reading, if ....
			
			// is a valid next available caracter
			if (peek > 0) return true;

			if (!string.IsNullOrWhiteSpace(line))
			{
				// was hangup
				if (line == AGI_REPLY_HANGUP) return true;

				var matcher = Common.AGI_STATUS_PATTERN_NAMED.Match(line);
				if (matcher.Groups["code"].Success)
				{
					if (int.TryParse(matcher.Groups["code"].Value, out int status))
					{
						if (
							status == 100 ||    // CONTINUE STATUS LIKE GOSUB COMMAND
							status == 520       // SC_INVALID_COMMAND_SYNTAX
							)
						{
							return true;
						}
					}
				}
			}

			// otherwise, stops ...
            return false;
        }

		#endregion

		#region Write(string s)

		/// <summary>
		/// Sends a given String to the socket connection.
		/// </summary>
		/// <param name="s">the String to send.</param>
		/// <throws> IOException if the String cannot be sent, maybe because the </throws>
		/// <summary>connection has already been closed.</summary>
		public void Write(string s)
		{
			writer.Write(Encoding.GetBytes(s));
			writer.Flush();
		}

		/*
		/// <summary>
		/// Sends a given String to the socket connection.
		/// </summary>
		/// <param name="msg">the String to send.</param>
		/// <throws> IOException if the String cannot be sent, maybe because the </throws>
		/// <summary>connection has already been closed.</summary>
		public void WriteEx(string msg)
		{
			byte[] data = Encoding.GetBytes(msg);
			var stream = GetStream();
            stream.BeginWrite(data, 0, data.Length, onWriteFinished, stream);
            stream.Flush();
		}

		private void onWriteFinished(IAsyncResult ar)
		{
			var stream = (NetworkStream)ar.AsyncState;
			stream.EndWrite(ar);
		}

		*/

		#endregion

		#region Close

		/// <summary>
		/// Closes the socket connection including its input and output stream and
		/// frees all associated ressources.<br/>
		/// When calling close() any Thread currently blocked by a call to readLine()
		/// will be unblocked and receive an IOException.
		/// </summary>
		/// <throws>  IOException if the socket connection cannot be closed. </throws>
		public new void Close()
		{
			try
			{				
                Client.Shutdown(SocketShutdown.Both);
                Client.Close();

                base.Close();                
			}
			catch { }
		}
        #endregion

        /// <summary>
        /// Recover the underlaying log system to use on extensions
        /// </summary>
        /// <returns></returns>
        public ILogger GetLogger() => _logger;
    }
}
