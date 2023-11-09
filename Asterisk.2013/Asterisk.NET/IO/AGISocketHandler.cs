using AsterNET.FastAGI.Command;
using AsterNET.FastAGI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sufficit.Asterisk;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace AsterNET.IO
{
    /// <summary>
    ///     SocketWrapper using standard socket classes.
    /// </summary>
    public class AGISocketHandler
    {
        private readonly ILogger _logger;
        private readonly TcpListener _listener;
		private readonly ListenerOptions _options;

        public AGISocketHandler(ILogger<AGISocketHandler> logger, IOptions<ListenerOptions> options)
		{
            _logger = logger;
            _options = options.Value;
            
            _listener = new TcpListener(_options.Address, (int)_options.Port);
            _listener.Server.DualMode = _options.DualMode;
		}

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            if (_listener.Server.Connected)
                throw new Exception("already started");

            //_token = cancellationToken;
            _listener.Start((int)_options.BackLog);
            _logger.LogInformation("started agi socket handler executing async");
            
            Int64 count = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                var ar = _listener.BeginAcceptSocket(PerformListenAsync, _listener);
                ar.AsyncWaitHandle.WaitOne();
                _logger.LogInformation("started listening, accept counter: {count}", ++count);
            }
        }

        private int count;
        private int simultaneous;
        public event EventHandler<SocketConnection>? OnRequest;

        void RequestReceived(Socket socket)
        {
            count++;
            try
            {
                // simultaneous count here because socket maybe cancelled, so can throw a exception
                simultaneous++;

                var listener = new SocketConnection(_logger, _options, socket);
                _logger.LogInformation("dispatching request received, thread id: {thread_id}, thread name: {thread_name}, socket id: {socket}, simultaneous: {simultaneous}",
                        Thread.CurrentThread.ManagedThreadId,
                        Thread.CurrentThread.Name,
                        socket.Handle,
                        simultaneous);

                OnRequest?.Invoke(this, listener);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "error at processing individual client socket");
            }
            finally
            {
                simultaneous--;              
            }
        }

        /// <summary>
        ///     Individual process
        /// </summary>               
        void PerformListenAsync(IAsyncResult ar)
        {
            try
            {
                // Always call End async method or there will be a memory leak. (HRM)
                // It creates a new socket to continue 
                var socket = _listener.EndAcceptSocket(ar);
                RequestReceived(socket);

                _listener.Server.EndConnect(ar);
            } 
            catch(Exception ex)
            {
                _logger.LogError(ex, "error performing async listener");
            }
        }

        public void Stop()
		{
            _listener.Stop();
		}
	}
}
