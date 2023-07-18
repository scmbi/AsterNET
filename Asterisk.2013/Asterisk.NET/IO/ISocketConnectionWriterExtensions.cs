using AsterNET.FastAGI.Command;
using AsterNET.FastAGI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace AsterNET.IO
{
    public static class ISocketConnectionWriterExtensions
    {
        public static void SendCommand(this ISocketConnection socket, AGICommand command)
            => socket.SendCommand(command.BuildCommand() + "\n");

        /// <summary>
        /// I Hope you know what u are doing
        /// </summary>
        /// <param name="buffer"></param>
        /// <exception cref="AGINetworkException"></exception>
        public static void SendCommand(this ISocketConnection socket, string buffer)
        {
            try
            {
                socket.Write(buffer);
            }
            catch (IOException e)
            {
                throw new AGINetworkException("Unable to send command to Asterisk: " + e.Message, e);
            }
        }
    }
}
