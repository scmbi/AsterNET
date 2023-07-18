using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AsterNET.FastAGI.MappingStrategies;
using AsterNET.IO;
using AsterNET.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sufficit.Asterisk;
using Sufficit.Asterisk.FastAGI;

namespace AsterNET.FastAGI
{
    public class AsteriskFastAGI
    {
        #region Variables

        private readonly FastAGIOptions _options;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;
        private readonly AGISocketHandler _socketHandler;

        /// <summary>
        ///     The strategy to use for bind AGIRequests to AGIScripts that serve them.
        /// </summary>
        public IMappingStrategy Strategy { get; }

        /// <summary>The thread pool that contains the worker threads to process incoming requests.</summary>
        private AsterNET.Util.ThreadPool pool;

        /// <summary> True while this server is shut down. </summary>
        private bool stopped;


        private Encoding socketEncoding = Encoding.ASCII;

        public Encoding SocketEncoding
        {
            get { return socketEncoding; }
            set { socketEncoding = value; }
        }

        #endregion
        #region CONSTRUCTORS

        public AsteriskFastAGI(IServiceProvider provider) : this(provider, provider.GetService<IMappingStrategy>() ?? new ResourceMappingStrategy()) { }

        public AsteriskFastAGI(IServiceProvider provider, IMappingStrategy strategy)
        {
            _serviceProvider = provider;
            Strategy = strategy; // setting mapping strategy

            _options = _serviceProvider.GetRequiredService<IOptions<FastAGIOptions>>().Value;
            _logger = _serviceProvider.GetRequiredService<ILogger<AsteriskFastAGI>>();

            var ipAddress = IPAddress.Parse(_options.Address);
            var logger = _serviceProvider.GetRequiredService<ILogger<AGISocketHandler>>();
            var options = new ListenerOptions() { Port = _options.Port, Address = ipAddress, Encoding = SocketEncoding };
            _socketHandler = new AGISocketHandler(logger, Options.Create<ListenerOptions>(options));
            _socketHandler.OnRequest += OnRequest;
        }

        private async void OnRequest(object sender, SocketConnection e)
        {
            _logger.LogDebug("Received connection.");
            var loggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();
            var connectionHandler = new AGIConnectionHandler(loggerFactory, e, Strategy, _options.SC511_CAUSES_EXCEPTION, _options.SCHANGUP_CAUSES_EXCEPTION);
            await connectionHandler.Run(CancellationToken.None);
        }

        #endregion
        #region Start() 

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            stopped = false;
            Strategy.Load();
            //pool = new AsterNET.Util.ThreadPool("AGIServer", (int)_options.Workers);
            //_logger.LogDebug("Thread pool started.");

            try
            {                
                await _socketHandler.ExecuteAsync(cancellationToken); 

                // await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            catch (Exception ex)
            {
                if (ex is IOException)
                {
                    _logger.LogError(ex, "Unable start AGI Server: cannot to bind to " + _options.Address + ":" + _options.Port + ".");
                }
                               
                _socketHandler.Stop();
                

               //pool.Shutdown();
                _logger.LogInformation("AGI Server shut down.");

                throw ex;
            }


            _logger.LogInformation("Listening on " + _options.Address + ":" + _options.Port + ".");

            try
            {
                var loggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();

                //ISocketConnection socket;
                //while (!cancellationToken.IsCancellationRequested)
                {
                    //var socket = await serverSocket.AcceptAsync(cancellationToken);
                    //var connectionHandler = new AGIConnectionHandler(loggerFactory, socket, mappingStrategy, SC511_CAUSES_EXCEPTION, SCHANGUP_CAUSES_EXCEPTION);
                    //pool.AddJob(connectionHandler);
                    //break;
                }
            }
            catch (IOException ex)
            {
                if (!stopped)
                {
                    _logger.LogError(ex, "IOException while waiting for connections (1).");
                    throw ex;
                }
            }
            finally
            {
                try
                {
                    _socketHandler.Stop();
                }
                catch (IOException ex)
                {
                    _logger.LogError("IOException while waiting for connections (2).", ex);
                }
				catch { }                
                //pool.Shutdown();

                _logger.LogInformation("AGI Server shut down.");
            }
        }

        #endregion

        #region Stop() 

        public void Stop()
        {
            stopped = true;
            _socketHandler.Stop();
        }

        #endregion
    }
}