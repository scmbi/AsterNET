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

namespace AsterNET.IO
{
	/// <summary>
	/// Socket connection to asterisk.
	/// </summary>
	public class SocketConnection : TcpClient, ISocketConnection
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
			_disposed = true;
            _logger.LogTrace("disposing");
            OnDisposing?.Invoke(this, disposing);
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
        private readonly StreamReader reader;
		private readonly BinaryWriter writer;
		private bool initial;

        #region Constructor - SocketConnection(socket) 

        public SocketConnection(Socket socket, Encoding? encoding = null, ILogger? logger = null)   
        {
			Client = socket;

            if (encoding != null)
                Encoding = encoding;

            this.reader = new StreamReader(this.GetStream(), Encoding);
            this.writer = new BinaryWriter(this.GetStream(), Encoding);
			
            ReceiveTimeout = RECEIVE_TIMEOUT; 
			SendTimeout = SEND_TIMEOUT;

            _logger = logger ?? new LoggerFactory().CreateLogger<SocketConnection>();
        }

        public SocketConnection(string host, int port, int receiveBufferSize = 0, Encoding? encoding = null, ILogger? logger = null) : base(host, port) 
		{
			if (receiveBufferSize > 0)
				ReceiveBufferSize = receiveBufferSize;
			
			if (encoding != null)
				Encoding = encoding;

            this.reader = new StreamReader(this.GetStream(), Encoding);
            this.writer = new BinaryWriter(this.GetStream(), Encoding);

			ReceiveTimeout = RECEIVE_TIMEOUT;
            SendTimeout = SEND_TIMEOUT;

            _logger = logger ?? new LoggerFactory().CreateLogger<SocketConnection>();

            initial = true;		
		}

		#endregion

		public bool Initial
		{
			get { return initial; }
			set { initial = value; }
		}

		public bool IsRemoteRequest
			=> Client.IsRemoteRequest();
		
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
		{
			try
			{
				return reader.ReadLine();
			}
			catch
			{
				return null;
			}
		}


		public IEnumerable<string> ReadLines(int? timeoutms = null)
		{
			if (timeoutms.HasValue)
			{
				// saving previous value
				var defaultms = ReceiveTimeout;
				ReceiveTimeout = timeoutms.Value;

				foreach (var line in ReadLines())
					yield return line;

				// restoring previous value
				ReceiveTimeout = defaultms;
			} else
			{
                foreach (var line in ReadLines())
                    yield return line;
            }
        }

        public IEnumerable<string> ReadLines()
        {	
            string line = string.Empty;
            do
            {
                try
                {
                    line = reader.ReadLine();
					if (line == AGI_REPLY_HANGUP)
					{
						HangUpTrigger();
						continue;
					}
                }
                catch(Exception ex) { 
					break;
					_logger.LogError(ex, "error reading lines");
				}
		
				yield return line;				
            } while (Reading(reader.Peek(), line));
        }

		/// <summary>
		/// Continue if still contains bytes or received a 100 status code
		/// </summary>
		private bool Reading(int peek, string line)
		{
			// returns true, continue reading, if ....
			
			// is a valid next available caracter
			if (peek >= 0) return true;

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
						/*
						else
						{
                            _logger.LogTrace("expected result code: {code}", status);
                        }
						*/
					}
					/*
					else
					{
                        _logger.LogWarning("no int result code: {code}", matcher.Groups["code"].Value);
                    }
					*/
				}
				/*
				else
				{
                    _logger.LogWarning("no valid result code, line: {line}", line);
                }
				*/
			}
			/*
			else {
                _logger.LogWarning("reading empty line, continue");                
			}
			*/

			// otherwise, stops ...
            return false;
        }

		/*
        public string? ReadToEnd()
		{
			string line = string.Empty;
			do
			{
				try
				{					
					line += reader.ReadLine();
				}
				catch { line = null; break; }
			} while (reader.Peek() >= 0);
			return line;
		}
		*/

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
		public void Close()
		{
			try
			{				
                Client.Shutdown(SocketShutdown.Both);
                Client.Close();
                Close();                
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
