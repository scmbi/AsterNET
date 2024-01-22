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
    public class AGIServerSocketHandler
    {
        private readonly ILogger _logger;
        private readonly TcpListener _listener;
		private readonly ListenerOptions _options;
        private readonly Simultaneous _simultaneous;

        public AGIServerSocketHandler(ILogger<AGIServerSocketHandler> logger, IOptions<ListenerOptions> options)
		{
            _logger = logger;
            _options = options.Value;

            _simultaneous = new Simultaneous();

            _listener = new TcpListener(_options.Address, (int)_options.Port);
            _listener.Server.DualMode = _options.DualMode;
		}

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            if (_listener.Server.Connected)
                throw new Exception("already started");

            try
            {
                _listener.Start((int)_options.BackLog);
                _logger.LogInformation("started agi socket handler executing async");

                Int64 count = 0;

                // running until cancellation is requested
                while (!cancellationToken.IsCancellationRequested)
                {
                    // await for a request, invite
                    var clientsocket = await Task.Run(_listener.AcceptSocketAsync, cancellationToken);

                    _logger.LogInformation("accepted requests counter: {count}", ++count);
                    _ = Task.Run(() => RequestAccepted(clientsocket, cancellationToken)).ConfigureAwait(false);
                }

                /*
                // running until cancellation is requested
                while (!cancellationToken.IsCancellationRequested)
                {
                    // Accept Request, invite
                    var ar = _listener.BeginAcceptSocket(PerformListenAsync, cancellationToken);

                    // waits for the accept, just the accept, not entire proccess
                    // a diferent socket will be created for the proccess
                    if (ar.AsyncWaitHandle.WaitOne())
                        _logger.LogInformation("accepted requests counter: {count}", ++count);
                    else
                        _listener.Server.EndConnect(ar);

                }
                */
            }
            catch (Exception ex) // never throw, marked for remove
            {
                _logger.LogError(ex, "error listening");
                throw;
            }
            
            //return Task.CompletedTask;
        }

        public event EventHandler<AMISingleSocketHandler>? OnRequest;

        /// <summary>
        ///     Starts reader and invoke attached events 
        /// </summary>
        void RequestAccepted (Socket socket, CancellationToken cancellationToken)
        {
            try
            {
                // simultaneous count here because socket maybe cancelled, so can throw a exception
                using var runner = _simultaneous.Run();

                // forcing start from this task context, testing
                _options.Start = false;

                // creating a handler for the accepted client socket
                var sc = new AMISingleSocketHandler(_logger, _options, socket);
                if (!_options.Start)
                    sc.Background(cancellationToken);

                _logger.LogInformation("dispatching accepted request, simultaneous: {simultaneous}", _simultaneous);

                // dispatching events
                OnRequest?.Invoke(this, sc);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "error at processing individual client socket");
            }
        }

        /// <summary>
        ///     Individual process, async
        /// </summary>        
        [Obsolete]
        async void PerformListenAsync (IAsyncResult ar)
        {
            CancellationToken cancellationToken = default;
            if (ar.AsyncState is CancellationToken ct) cancellationToken = ct;

            try
            {
                // Always call End async method or there will be a memory leak. (HRM)
                // It creates a new socket to continue 
                // And sends a signal to async request context to continue
                var socket = _listener.EndAcceptSocket(ar);
                
                // main execution control
                RequestAccepted(socket, cancellationToken);

                _listener.Server.EndConnect(ar);
            } 
            catch (Exception ex)
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
