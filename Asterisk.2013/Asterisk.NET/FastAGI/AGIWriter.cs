using System.IO;
using AsterNET.FastAGI.Command;
using AsterNET.IO;

namespace AsterNET.FastAGI
{
    /// <summary>
    ///     Default implementation of the AGIWriter interface.
    /// </summary>
    public class AGIWriter
    {
        private readonly SocketConnection socket;

        public AGIWriter(SocketConnection socket)
        {
            lock (this)
                this.socket = socket;
        }

        public void SendCommand(AGICommand command) 
            => SendCommand(command.BuildCommand() + "\n");        

        /// <summary>
        /// I Hope you know what u are doing
        /// </summary>
        /// <param name="buffer"></param>
        /// <exception cref="AGINetworkException"></exception>
        public void SendCommand(string buffer)
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