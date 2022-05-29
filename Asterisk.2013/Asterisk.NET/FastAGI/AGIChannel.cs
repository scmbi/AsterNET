using AsterNET.FastAGI.Command;
using AsterNET.IO;
using Microsoft.Extensions.Logging;
using System;

namespace AsterNET.FastAGI
{
    /// <summary>
    ///     Default implementation of the AGIChannel interface.
    /// </summary>
    public class AGIChannel
    {
        private readonly ILogger? _logger;
        private readonly bool _SC511_CAUSES_EXCEPTION;
        private readonly bool _SCHANGUP_CAUSES_EXCEPTION;
        private readonly AGIReader agiReader;
        private readonly AGIWriter agiWriter;

        public AGIChannel(SocketConnection socket, bool SC511_CAUSES_EXCEPTION, bool SCHANGUP_CAUSES_EXCEPTION)
        {
            agiWriter = new AGIWriter(socket);
            agiReader = new AGIReader(socket);

            _SC511_CAUSES_EXCEPTION = SC511_CAUSES_EXCEPTION;
            _SCHANGUP_CAUSES_EXCEPTION = SCHANGUP_CAUSES_EXCEPTION;
        }

        public AGIChannel(ILogger<AGIChannel> logger, AGIWriter agiWriter, AGIReader agiReader, bool SC511_CAUSES_EXCEPTION, bool SCHANGUP_CAUSES_EXCEPTION)
        {
            _logger = logger;
            this.agiWriter = agiWriter;
            this.agiReader = agiReader;

            _SC511_CAUSES_EXCEPTION = SC511_CAUSES_EXCEPTION;
            _SCHANGUP_CAUSES_EXCEPTION = SCHANGUP_CAUSES_EXCEPTION;
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
            AGIReply agiReply = agiReader.ReadReply();
            int status = agiReply.GetStatus();
            if (status == (int) AGIReplyStatuses.SC_INVALID_OR_UNKNOWN_COMMAND)
                throw new InvalidOrUnknownCommandException(command.BuildCommand());
            if (status == (int) AGIReplyStatuses.SC_INVALID_COMMAND_SYNTAX)
                throw new InvalidCommandSyntaxException(agiReply.GetSynopsis(), agiReply.GetUsage());
            if (status == (int) AGIReplyStatuses.SC_DEAD_CHANNEL && _SC511_CAUSES_EXCEPTION)
                throw new AGIHangupException();
            if ((status == 0) && agiReply.FirstLine == "HANGUP" && _SCHANGUP_CAUSES_EXCEPTION)
                throw new AGIHangupException();
            return agiReply;
        }

        /// <summary>
        /// Recover the underlaying log system to use on extensions
        /// </summary>
        /// <returns></returns>
        public ILogger? GetLogger() => _logger;
    }
}