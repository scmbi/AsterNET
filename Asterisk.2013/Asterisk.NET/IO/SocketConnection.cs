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
using System.Runtime.CompilerServices;
using Sufficit.Asterisk.IO;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;
using System.Linq;

namespace AsterNET.IO
{
	/// <summary>
	/// Socket connection to asterisk.
	/// </summary>
	public class SocketConnection : ISocketConnection, IDisposable
    {
        /// <summary>
        /// Named Group (code) http status code
        /// </summary>
        public static Regex AGI_STATUS_PATTERN_NAMED = new Regex(@"^(?<code>\d{3})[ -]", RegexOptions.Compiled);

        /// <summary>
        /// Expected message received for hangup channels
        /// </summary>
        public const string AGI_REPLY_HANGUP = "HANGUP";

		public NetworkStream? GetStream()
			=> !_disposed ? new NetworkStream(_socket) : null;		

        #region DISPOSING

        /// <inheritdoc cref="ISocketConnection.OnDisposing" />
        public event EventHandler? OnDisposing;

		private bool _disposed;
        		
        public void Dispose()
        {
			if (!_disposed)
            {
                // invoking before dispose internals, to grant availability
                OnDisposing?.Invoke(this, EventArgs.Empty);

                _disposed = true;
				_logger.LogTrace("disposing requested");

                // Closing socket if connected
                Close();
                
                if (BackgroundReadingTask != null)
                {
                    BackgroundReadingTask.Dispose();
                    BackgroundReadingTask = null;
                }
			}
        }

        #endregion
        #region HANGUP CONTROL

        /// <summary>
        ///  Indicates that hangup message is received
        /// </summary>
        public bool IsHangUp { get; internal set; }

        /// <inheritdoc cref="ISocketConnection.OnHangUp" />
        public event EventHandler? OnHangUp;

        protected void HangUpTrigger()
        {
            IsHangUp = true;
			_logger.LogTrace("hangup");
            OnHangUp?.Invoke(this, EventArgs.Empty);
        }

        #endregion
        #region DISCONNECTED

        /// <inheritdoc cref="ISocketConnection.OnDisconnected"/>
        public event EventHandler<string?>? OnDisconnected;

        protected virtual void DisconnectedTrigger(string? cause = null)
        {
			// checking object was not disposed yet
            if (_socket != null)
            {
                _socket.Shutdown(SocketShutdown.Both);
                _socket.Close();
            }
            
			if (!string.IsNullOrWhiteSpace(cause))
				_logger.LogWarning("disconnected, it should not happen, cause: {cause}", cause);

            try
            {
                OnDisconnected?.Invoke(this, cause);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "error at invoke disconnect events");
            }
        }

        #endregion

		public AGISocketOptions Options { get; }

        private readonly ILogger _logger;
        private readonly Socket _socket;
		private readonly BlockingCollection<string?> _buffer;
		private bool initial;

        #region CONSTRUCTORS

		public SocketConnection(AGISocketOptions options, Socket socket) : 
			this(new LoggerFactory().CreateLogger<SocketConnection>(), options, socket){ }

        public SocketConnection(ILogger logger, AGISocketOptions options, Socket socket)   
        {			
			// use that only for already connected sockets
			if (!socket.Connected)
				throw new Exception("invalid socket not connected");

            _buffer = new BlockingCollection<string?>();
            _logger = logger;
            _socket = socket;
            Options = options;

			_logger.LogDebug("starting connection, thread id: {thread}, socket id: {socket}", Thread.CurrentThread.ManagedThreadId, socket.Handle); 
			initial = true;

            // auto start reading
            if (Options.Start)
                Background(CancellationToken.None);
        }

        #endregion
        #region BACKGROUND RECEIVING
                
        public Task? BackgroundReadingTask;

        /// <summary>
        ///     Starts reading from background 
        /// </summary>
        public void Background(CancellationToken cancellationToken)
        {
            if (!IsConnected())
                throw new Exception("socket must be connect before starts reading");

            if (CTSBackgroundReceiving != null && !CTSBackgroundReceiving.IsCancellationRequested)
                CTSBackgroundReceiving.Cancel();

            BackgroundReadingTask = Task.Factory.StartNew(() => StartReceiving(cancellationToken));
        }

        private CancellationTokenSource? CTSBackgroundReceiving;

        private void StartReceiving(CancellationToken cancellationToken)
        {
			// setting the cancellation source
            CTSBackgroundReceiving = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            try
            {
                while (!CTSBackgroundReceiving.IsCancellationRequested)
                {
                    var buffer = new byte[Options.BufferSize];
                    var bytesRead = _socket.Receive(buffer);

					if (IsReceiving(_socket))
					{
						// creating an array of exact size for received content
						var actualData = new byte[bytesRead];
						Array.Copy(buffer, actualData, bytesRead);

						// dispaching received data event
						OnDataReceived(actualData);
					}
					else
					{
                        DisconnectedTrigger();
						break;
					}
                }
            }
            catch (OperationCanceledException) 
            {
                _logger.LogTrace("receiving raw data from socket cancelled requested");
            } 
            catch (SocketException ex) 
            {
                if (ex.ErrorCode == 103)
                    _logger.LogTrace("receiving raw data from socket aborted");
                
                else if (ex.Message.Contains("WSACancelBlockingCall"))
                    _logger.LogTrace("receiving raw data from socket cancelled requested at buffering");
                
                else if (ex.Message.Contains("reset by peer"))
                    DisconnectedTrigger("reset by peer");
                
                else
                    _logger.LogError(ex, "error on receiving raw data from socket");
            }
        }

		// not sure if all received events fix at default buffer size
		// so may crash or split a response line
		// must be observed !
		//
		// is not safe to use only text lines for response
        private void OnDataReceived(byte[] data)
        {
			using(var ms = new MemoryStream(data)) 
			{
				using (var reader = new StreamReader(ms))
				{
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        if (line == AGI_REPLY_HANGUP)
                        {
                            HangUpTrigger();
                            continue;
                        }

                        _buffer.Add(line);
					}
				}
            }
        }

        #endregion
        #region READING FROM QUEUE

        public IEnumerable<string> ReadQueue(CancellationToken cancellationToken, uint? timeoutms = null)
        {
            if (!timeoutms.HasValue || timeoutms == 0)
                timeoutms = Options.ReceiveTimeout > 0 ? Options.ReceiveTimeout : AGISocketOptions.RECEIVE_TIMEOUT;

            var byTimeOut = new CancellationTokenSource((int)timeoutms.Value);
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, byTimeOut.Token);
            return ReadQueue(cts.Token);
        }

        public IEnumerable<string> ReadQueue(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var line = _buffer.Take(cancellationToken);
                if (string.IsNullOrWhiteSpace(line)) break;
                yield return line;
            }
        }

        /// <summary>
        ///		Read Initial Request. <br />
        ///		Means all information until the first empty line. <br />
        /// </summary>
        public IEnumerable<string> ReadRequest(CancellationToken cancellationToken)
        {
            foreach (var line in ReadQueue(cancellationToken))            
                yield return line;            
        }

        public IEnumerable<string> ReadReply(uint? timeoutms = null)
        {
            using (var cts = new CancellationTokenSource())
            {
                foreach (var line in ReadQueue(cts.Token, timeoutms))
                {
                    yield return line;

                    var matcher = AGI_STATUS_PATTERN_NAMED.Match(line);
                    if (matcher.Groups["code"].Success)
                    {
                        if (int.TryParse(matcher.Groups["code"].Value, out int status))
                        {
                            if (
                                status == 100 ||    // CONTINUE STATUS LIKE GOSUB COMMAND
                                status == 520       // SC_INVALID_COMMAND_SYNTAX
                                )
                            {
                                continue;
                            }
                        }
                    }

                    break;
                }
            }
        }

        /// <summary>
        ///		Check if the socket still connected and receiving
        /// </summary>
        private static bool IsReceiving(Socket socket)
        {
            return !(socket.Poll(1000, SelectMode.SelectRead) && socket.Available == 0);
        }

        #endregion

        public bool Initial
		{
			get { return initial; }
			set { initial = value; }
		}

		public bool IsConnected()
            => !_disposed && _socket.Connected;
				
        #region LocalAddress 

        public IPAddress LocalAddress
		{
			get
			{
				return ((IPEndPoint)(_socket.LocalEndPoint)).Address;
			}
		}

		#endregion
		#region LocalPort 

		public int LocalPort
		{
			get
			{
				return ((IPEndPoint)(_socket.LocalEndPoint)).Port;
			}
		}

		#endregion
		#region RemoteAddress 

		public IPAddress RemoteAddress
		{
			get
			{
				return ((IPEndPoint)(_socket.RemoteEndPoint)).Address;
			}
		}

		#endregion
		#region RemotePort 

		public int RemotePort
		{
			get
			{
				return ((IPEndPoint)(_socket.LocalEndPoint)).Port;
			}
		}

        #endregion
        #region IsRemoteRequest

        private bool? isRemoteRequest;

        public bool IsRemoteRequest
        {
            get
            {
                if (isRemoteRequest.HasValue)
                    return isRemoteRequest.Value;

                var remote = _socket.RemoteEndPoint;
                var local = _socket.LocalEndPoint;

                isRemoteRequest = SocketExtensions.IsRemoteRequest(remote, local);
                _logger.LogDebug("comparing for remote => (remote: {remote}, local: {local}) => {result}", remote, local, isRemoteRequest);

                return isRemoteRequest.Value;
            }
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
			_socket.Send(Options.Encoding.GetBytes(s));
		}

		#endregion

		#region Close

		public new void Close() => Close(string.Empty);

		/// <summary>
		/// Closes the socket connection including its input and output stream and
		/// frees all associated ressources.<br/>
		/// When calling close() any Thread currently blocked by a call to readLine()
		/// will be unblocked and receive an IOException.
		/// </summary>
		/// <throws>  IOException if the socket connection cannot be closed. </throws>
		public void Close(string cause)
		{
            if (!string.IsNullOrWhiteSpace(cause))
			    _logger.LogWarning("forcing to close connection, cause: {cause}", cause);

			if (CTSBackgroundReceiving != null && !CTSBackgroundReceiving.IsCancellationRequested)
			{
				CTSBackgroundReceiving.Cancel();
                CTSBackgroundReceiving = null;

                // avoiding read error
                Task.Delay(50).Wait();
			}

			DisconnectedTrigger();
		}

        #endregion

        public IntPtr Handle => _socket.Handle;

        /// <summary>
        /// Recover the underlaying log system to use on extensions
        /// </summary>
        /// <returns></returns>
        public ILogger GetLogger() => _logger;
    }
}
