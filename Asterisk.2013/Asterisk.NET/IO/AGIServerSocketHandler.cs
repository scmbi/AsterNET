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
        private readonly ILoggerFactory _loggerFactory;
        private readonly TcpListener _listener;
		private readonly ListenerOptions _options;
        private readonly Simultaneous _simultaneous;

        public AGIServerSocketHandler(ILoggerFactory factory, IOptions<ListenerOptions> options)
		{
            _loggerFactory = factory;
            _logger = factory.CreateLogger<AGIServerSocketHandler>(); 

            _options = options.Value;

            _simultaneous = new Simultaneous();

            var address = _options.Address ?? IPAddress.IPv6Any;
            _listener = new TcpListener(address, (int)_options.Port);
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
            }
            catch (Exception ex) // never throw, marked for remove
            {
                _logger.LogError(ex, "error listening");
                throw;
            }
            
            //return Task.CompletedTask;
        }

        /// <summary>
        ///     Handler for each client request received
        /// </summary>
        //public event EventHandler<AMISingleSocketHandler>? OnRequest;
        public Func<AMISingleSocketHandler, CancellationToken, ValueTask>? OnRequest; 

        /// <summary>
        ///     Starts reader and invoke attached events 
        /// </summary>
        async void RequestAccepted (Socket? socket, CancellationToken cancellationToken)
        {
            // probably a test connection, a health check connection !
            {
                if (socket == null)
                {
                    _logger.LogDebug("ignoring accepted request, cause socket disposed");
                    return;
                }

                if (!socket.Connected)
                {
                    _logger.LogDebug("ignoring accepted request, cause it's not connected");
                    return;
                }
            }

            // simultaneous count here because socket maybe cancelled, so can throw a exception
            using var runner = _simultaneous.Run();

            try
            {               
                // forcing start from this task context, testing
                _options.Start = false;

                // creating a handler for the accepted client socket
                var logger = _loggerFactory.CreateLogger<AMISingleSocketHandler>();
                using var sc = new AMISingleSocketHandler(logger, _options, socket);

                if (!_options.Start)
                    sc.Background(cancellationToken);

                _logger.LogInformation("dispatching accepted request, simultaneous: {simultaneous}", _simultaneous);

                // starting request handler delegate
                if (OnRequest != null)
                    await OnRequest(sc, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "error at processing individual client socket");
            }
        }

        public void Stop()
		{
            _listener.Stop();
		}
	}
}
