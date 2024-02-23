using System.IO;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System;
using System.Collections.Generic;
using Sufficit;
using Sufficit.Asterisk;
using Sufficit.Asterisk.IO;
using AsterNET.Manager;
using Microsoft.Extensions.Logging;
using AsterNET.FastAGI;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;
using System.Linq;

namespace AsterNET.IO
{
    /// <summary>
    ///     Accepted/Active socket connection handler for asterisk manager interface.
    /// </summary>
    /// <remarks>
    ///     Used after agi server accepted a request <br />
    ///     Or when trying to active connect to asterisk
    /// </remarks>
    public class AMISingleSocketHandler : ISocketConnection, IDisposable
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
			=> !IsDisposeRequested ? new NetworkStream(_socket) : null;		

        #region DISPOSING

        /// <inheritdoc cref="ISocketConnection.OnDisposing" />
        public event EventHandler? OnDisposing;

        public bool IsDisposeRequested { get; internal set; }

        public void Dispose()
        {
			if (!IsDisposeRequested)
            {
                // invoking before dispose internals, to grant availability
                OnDisposing?.Invoke(this, EventArgs.Empty);

                IsDisposeRequested = true;
				_logger.LogTrace("disposing requested");

                // Closing socket if connected
                // checking object was not disposed yet
                if (_socket != null)
                {
                    if (_socket.Connected)
                    {
                        _socket.Shutdown(SocketShutdown.Both);
                        _socket.Close();
                    }

                    _socket.Dispose();
                }

                TriggerCancellation();

                if (BackgroundReadingTask.IsCompleted)
                    BackgroundReadingTask.Dispose();
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

        /// <inheritdoc cref="ISocketConnection.OnDisconnected" />
        public event EventHandler<string?>? OnDisconnected;

        public bool IsDisconnectRequested { get; internal set; }

        protected virtual void DisconnectedTrigger(AGISocketReason reason)
        {
            if (!IsDisconnectRequested)
            {
                IsDisconnectRequested = true;

                // checking object was not disposed yet
                if (_socket != null)
                {
                    if (_socket.Connected)
                    {
                        _socket.Shutdown(SocketShutdown.Both);
                        _socket.Close();
                    }
                }

                if (!reason.HasFlag(AGISocketReason.NORMALENDING))
                    _logger.LogWarning("({hash}) disconnected triggered, reason: {reason}", GetHashCode(), reason);

                OnDisconnected?.Invoke(this, reason.ToString());
            }
        }

        protected virtual void DisconnectedTrigger(string? reason = null)
        {
            if (!IsDisconnectRequested)
            {
                IsDisconnectRequested = true;

                // checking object was not disposed yet
                if (_socket != null)
                {
                    if (_socket.Connected)
                    {
                        _socket.Shutdown(SocketShutdown.Both);
                        _socket.Close();
                    }
                }

                if (!string.IsNullOrWhiteSpace(reason))
                    _logger.LogWarning("({hash}) disconnected triggered, reason: {reason}", GetHashCode(), reason);

                OnDisconnected?.Invoke(this, reason);
            }
        }

        #endregion
        #region CANCEL REQUEST

        public bool IsCancellationRequested => CTSBackgroundReading?.IsCancellationRequested ?? false;

        private void TriggerCancellation()
        {
            if (CTSBackgroundReading != null && !CTSBackgroundReading.IsCancellationRequested)
            {
                CTSBackgroundReading.Cancel();

                // avoiding read error
                Task.Delay(50).Wait();
            }

            CTSBackgroundReading = null;
        }

        #endregion
        #region SOCKET EXCEPTION CONTROL

        private void TriggerSocketException(SocketException ex)
        {
            string cause;
            if (ex.ErrorCode == 103)
            {
                _logger.LogTrace("({hash}) receiving raw data from socket aborted: {code}", GetHashCode(), ex.SocketErrorCode);
                cause = "aborted";
            }
            else if (ex.Message.Contains("WSACancelBlockingCall"))
            {
                _logger.LogTrace("({hash}) receiving raw data from socket cancelled requested at buffering: {code}", GetHashCode(), ex.SocketErrorCode);
                cause = "cancellation requested";
            }
            else if (ex.ErrorCode == 104)
            {
                _logger.LogError(ex, "({hash}) socket reseted by peer: {code}", GetHashCode(), ex.SocketErrorCode);
                cause = "reset by peer";
            }
            else
            {
                _logger.LogError(ex, "({hash}) error on receiving raw data from socket: {code}", GetHashCode(), ex.SocketErrorCode);
                cause = $"unhandled: {ex.SocketErrorCode}";
            }

            DisconnectedTrigger(cause);
        }

        #endregion

        public AGISocketOptions Options { get; }

        private readonly ILogger _logger;
        private readonly Socket _socket;
		private readonly BlockingCollection<string?> _buffer;
		private bool initial;

        #region CONSTRUCTORS

		public AMISingleSocketHandler(AGISocketOptions options, Socket socket) : 
			this(new LoggerFactory().CreateLogger<AMISingleSocketHandler>(), options, socket){ }

        public AMISingleSocketHandler(ILogger logger, AGISocketOptions options, Socket socket)   
        {			
			// use that only for already connected sockets
			if (!socket.Connected)
				throw new Exception("invalid socket not connected");

            _buffer = new BlockingCollection<string?>();
            _logger = logger;
            _socket = socket;
            Options = options;

			initial = true;

            _logger.LogDebug("({hash}) socket handler instantiated, thread id: {thread}, socket id: {socket}, auto start: {start}", GetHashCode(), Thread.CurrentThread.ManagedThreadId, socket.Handle, Options.Start);

            BackgroundReadingTask = new Task(BackgroundReading);

            // auto start reading
            if (Options.Start)
                Background(CancellationToken.None);
        }

        #endregion
        #region BACKGROUND RECEIVING
                
        public Task BackgroundReadingTask { get; }

        /// <summary>
        ///     Starts reading from background 
        /// </summary>
        public void Background(CancellationToken cancellationToken)
        {
            if (BackgroundReadingTask.Status == TaskStatus.Created)
            {
                if (!IsConnected())
                    throw new Exception($"({GetHashCode()}) socket must be connect before starts reading");

                CTSBackgroundReading = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                BackgroundReadingTask.Start(TaskScheduler.Default);
            }
        }

        private CancellationTokenSource? CTSBackgroundReading;

        private void BackgroundReading()
        {
            try
            {
                _logger.LogInformation("({hash}) starting receiving, thread id: {thread_id}, thread name: {thread_name}, socket id: {socket}", 
                GetHashCode(),
                Thread.CurrentThread.ManagedThreadId,
                Thread.CurrentThread.Name,
                _socket.Handle);

                // setting the cancellation source
                var token = CTSBackgroundReading?.Token ?? CancellationToken.None;

                while (IsReceiving(_socket))
                {
                    token.ThrowIfCancellationRequested();

                    var buffer = new byte[Options.BufferSize];
                    var bytesRead = _socket.Receive(buffer);

                    if (bytesRead > 0)
                    {
                        // creating an array of exact size for received content
                        // trimming empty spaces at end of the buffer array, in order to dispatch
                        var actualData = new byte[bytesRead];
                        Array.Copy(buffer, actualData, bytesRead);

                        // dispatching received data event
                        OnDataReceived(actualData);
                    }
                    else break;
                }                

                DisconnectedTrigger(AGISocketReason.NOTRECEIVING);
            }
            catch (OperationCanceledException) 
            {
                _logger.LogTrace("({hash}) receiving raw data from socket cancelled requested", GetHashCode());
            } 
            catch (SocketException ex)
            {
                TriggerSocketException(ex);                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"({GetHashCode()}) unknown error on receiving raw data from socket");
                throw;
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
        private static bool IsReceiving(Socket? socket)
        {
            if (socket == null)
                return false;

            return !(socket.Poll(1000, SelectMode.SelectRead) && socket.Available == 0);
        }

        #endregion

        public bool Initial
		{
			get { return initial; }
			set { initial = value; }
		}

		public bool IsConnected()
            => !IsDisposeRequested && _socket.Connected;
				
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
            var bytes = Options.Encoding.GetBytes(s);

            try
            {
                _socket.Send(bytes);
            } 
            catch (SocketException ex) { TriggerSocketException(ex); throw; }
		}

		#endregion

		#region Close

        /// <summary>
        ///     Indicates that <see cref="Close(string?)"></see> was already called />
        /// </summary>
        public bool IsCloseRequested { get; private set; }

        public void Close(AGISocketReason reason)
        {
            if (!IsCloseRequested)
            {
                IsCloseRequested = true;
                
                if (!reason.HasFlag(AGISocketReason.NORMALENDING))
                    _logger.LogWarning("({hash}) forcing to close connection, cause: {cause}", GetHashCode(), reason);

                TriggerCancellation();
                DisconnectedTrigger(reason);
            }
        }

        /// <summary>
        /// Closes the socket connection including its input and output stream and
        /// frees all associated ressources.<br/>
        /// When calling close() any Thread currently blocked by a call to readLine()
        /// will be unblocked and receive an IOException.
        /// </summary>
        /// <throws>  IOException if the socket connection cannot be closed. </throws>
        public void Close(string? reason = null)
		{
            if (!IsCloseRequested)
            {
                IsCloseRequested = true;

                if (!string.IsNullOrWhiteSpace(reason))
                    _logger.LogWarning("({hash}) forcing to close connection, generic cause: {cause}", GetHashCode(), reason);

                TriggerCancellation();
                DisconnectedTrigger(reason);
            }
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
