using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using AsterNET.FastAGI.Command;
using AsterNET.IO;
using AsterNET.Manager;
using Microsoft.Extensions.Logging;
using Sufficit.Asterisk;

namespace AsterNET.FastAGI
{
    /// <summary>
    ///     An AGIConnectionHandler is created and run by the AGIServer whenever a new
    ///     socket connection from an Asterisk Server is received.<br />
    ///     It reads the request using an AGIReader and runs the AGIScript configured to
    ///     handle this type of request. Finally it closes the socket connection.
    /// </summary>
    public class AGIConnectionHandler
    {
        private readonly ILoggerFactory loggerFactory;
        private readonly ILogger _logger;
        private readonly ISocketConnection _socket;
        private readonly IMappingStrategy mappingStrategy;
        private readonly bool _SC511_CAUSES_EXCEPTION;
        private readonly bool _SCHANGUP_CAUSES_EXCEPTION;

        #region AGIConnectionHandler(socket, mappingStrategy)

        /// <summary>
        ///     Creates a new AGIConnectionHandler to handle the given socket connection.
        /// </summary>
        /// <param name="socket">the socket connection to handle.</param>
        /// <param name="mappingStrategy">the strategy to use to determine which script to run.</param>
        public AGIConnectionHandler(ILoggerFactory loggerFactory, ISocketConnection socket, IMappingStrategy mappingStrategy, bool SC511_CAUSES_EXCEPTION, bool SCHANGUP_CAUSES_EXCEPTION)
        {
            this.loggerFactory = loggerFactory;
            _socket = socket;
            this.mappingStrategy = mappingStrategy;
            _SC511_CAUSES_EXCEPTION = SC511_CAUSES_EXCEPTION;
            _SCHANGUP_CAUSES_EXCEPTION = SCHANGUP_CAUSES_EXCEPTION;

            _logger = loggerFactory.CreateLogger<AGIConnectionHandler>();            
        }

        #endregion

        public async Task Run(CancellationToken cancellationToken)
        {
            using (_logger.BeginScope<string>($"[HND:{new Random().Next()}]"))
            {
                string? statusMessage = null;
                try
                {
                    var request = _socket.GetRequest(cancellationToken);

                    // Added check for when the request is empty
                    // eg. telnet to the service 
                    if (request.Request.Count > 0)
                    {
                        using (var script = mappingStrategy.DetermineScript(request))
                        {
                            if (script != null)
                            {
                                var loggerChannel = loggerFactory.CreateLogger<AGIChannel>();
                                var channel = new AGIChannel(loggerChannel, _socket, _SC511_CAUSES_EXCEPTION, _SCHANGUP_CAUSES_EXCEPTION);

                                _logger.LogTrace("Begin AGIScript " + script.GetType().FullName + " on " + Thread.CurrentThread.Name);
                                await script.ExecuteAsync(request, channel, cancellationToken);
                                statusMessage = "SUCCESS";

                                _logger.LogTrace("End AGIScript " + script.GetType().FullName + " on " + Thread.CurrentThread.Name);
                            }
                            else
                            {
                                statusMessage = "No script configured for URL '" + request.RequestURL + "' (script '" + request.Script + "')";
                                throw new FileNotFoundException(statusMessage);
                            }
                        }
                    }
                    else
                    {
                        statusMessage = "A connection was made with no requests";
                        _logger.LogInformation(statusMessage);
                    }
                }
                catch (AGIHangupException ex)
                {
                    statusMessage = ex.Message;
                    _logger.LogError(ex, $"IDX00004(AGIHangup): {statusMessage}");
                }
                catch (IOException ex)
                {
                    statusMessage = ex.Message;
                    _logger.LogError(ex, $"IDX00003(IO): {statusMessage}");
                }
                catch (AGIException ex)
                {
                    statusMessage = ex.Message;
                    _logger.LogError(ex, $"IDX00002(AGI): {statusMessage}");
                }
                catch (Exception ex) // exception at script level
                {
                    statusMessage = ex.Message;
                    _logger.LogError(ex, $"IDX00001(Unexpected): {statusMessage}");
                }

                try
                {
                    if (!string.IsNullOrWhiteSpace(statusMessage))
                    {
                        var command = new SetVariableCommand(Common.AGI_DEFAULT_RETURN_STATUS, statusMessage);
                        _socket.SendCommand(command);
                    }

                    _socket.Close();
                }
                catch (IOException ex)
                {
                    _logger.LogError(ex, $"IDX00000(IOClosing): {ex.Message}");
                }
                catch (Exception ex) { _logger.LogError(ex, $"IDX00005(Unknown): {ex.Message}"); }
            }
        }
    }
}