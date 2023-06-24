using AsterNET.FastAGI.Command;
using AsterNET.FastAGI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;

namespace AsterNET.IO
{
    public static class ISocketConnectionReaderExtensions
    {
        public static AGIRequest GetRequest(this ISocketConnection socket, CancellationToken cancellationToken)
        {
            var requestLines = socket.ReadRequest(cancellationToken).ToArray();
            return new AGIRequest(requestLines)
            {
                LocalAddress = socket.LocalAddress,
                LocalPort = socket.LocalPort,
                RemoteAddress = socket.RemoteAddress,
                RemotePort = socket.RemotePort
            };
        }

        public static AGIReply GetReply(this ISocketConnection socket, uint? timeoutms = null)
            => new AGIReply(socket.ReadReply(timeoutms).ToArray());
    }
}
