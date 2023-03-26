using AsterNET.FastAGI.Command;
using AsterNET.IO;
using AsterNET.Manager;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Sockets;

namespace AsterNET.FastAGI
{
    /// <summary>
    ///     Default implementation of the AGIChannel interface.
    /// </summary>
    public class AGIChannel
    {
        #region HANGUP CONTROL

        /// <summary>
        ///  Indicates that hangup message is received
        /// </summary>
        public bool IsHangUp => Socket.IsHangUp;

        #endregion

        public ISocketConnection Socket { get; }

        private readonly ILogger _logger;
        private readonly bool _SC511_CAUSES_EXCEPTION;
        private readonly bool _SCHANGUP_CAUSES_EXCEPTION;
        private readonly AGIReader agiReader;
        private readonly AGIWriter agiWriter;

        public AGIChannel(ILogger<AGIChannel> logger, ISocketConnection socket, bool SC511_CAUSES_EXCEPTION, bool SCHANGUP_CAUSES_EXCEPTION)
        {
            _logger = logger;
            _logger.BeginScope(this);

            Socket = socket;
            agiWriter = new AGIWriter(socket);
            agiReader = new AGIReader(socket, logger);

            _SC511_CAUSES_EXCEPTION = SC511_CAUSES_EXCEPTION;
            _SCHANGUP_CAUSES_EXCEPTION = SCHANGUP_CAUSES_EXCEPTION;
        }

        public AGIChannel(ISocketConnection socket, bool SC511_CAUSES_EXCEPTION, bool SCHANGUP_CAUSES_EXCEPTION)
            : this(new LoggerFactory().CreateLogger<AGIChannel>(), socket, SC511_CAUSES_EXCEPTION, SCHANGUP_CAUSES_EXCEPTION)
        {
            
        }

        /// <summary>
		/// Sends the given command to the channel attached to the current thread.
		/// </summary>
		/// <param name="command">the command to send to Asterisk</param>
		/// <returns> the reply received from Asterisk</returns>
		/// <throws>  AGIException if the command could not be processed properly </throws>
        public AGIReply SendCommand(AGICommand command)
        {
            agiWriter.SendCommand(command);
            var agiReply = agiReader.ReadReply(command.ReadTimeOut);
            int status = agiReply.GetStatus();
            if (status == (int) AGIReplyStatuses.SC_INVALID_OR_UNKNOWN_COMMAND)
                throw new InvalidOrUnknownCommandException(command.BuildCommand());
            if (status == (int) AGIReplyStatuses.SC_INVALID_COMMAND_SYNTAX)
                throw new InvalidCommandSyntaxException(agiReply.GetSynopsis(), agiReply.GetUsage());

            if (_SC511_CAUSES_EXCEPTION)
            {
                if (IsHangUp || status == (int)AGIReplyStatuses.SC_DEAD_CHANNEL)
                    throw new AGIHangupException();
            }
            return agiReply;
        }

        /// <summary>
        /// Recover the underlaying log system to use on extensions
        /// </summary>
        /// <returns></returns>
        public ILogger GetLogger() => _logger;
    }
}