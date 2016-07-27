using System.Collections.Generic;
using System.IO;
using System.Linq;
using AsterNET.IO;

namespace AsterNET.FastAGI
{
    public class AGIReader
    {
#if LOGGER
        private readonly Logger logger = Logger.Instance();
#endif
        private readonly SocketConnection socket;

        public AGIReader(SocketConnection socket)
        {
            this.socket = socket;
        }

        public AGIRequest ReadRequest()
        {
            var lines = new List<string>();
            try
            {
#if LOGGER
                logger.Info("AGIReader.ReadRequest():");
#endif
                string line;
                while ((line = socket.ReadLine()) != null)
                {
                    if (line.Length == 0)
                        break;
                    lines.Add(line);
#if LOGGER
                    logger.Info(line);
#endif
                }
            }
            catch (IOException ex)
            {
                throw new AGINetworkException("Unable to read request from Asterisk: " + ex.Message, ex);
            }

            var firstLine = lines.FirstOrDefault();
            AwsElbProxyHeader elbProxyHeader = null;
            if (!string.IsNullOrEmpty(firstLine))
            {
                //PROXY TCP4 172.31.33.187 10.0.0.106 4675 2000
                if (firstLine.StartsWith("PROXY "))
                {
                    var parts = firstLine.Split(" ".ToCharArray());
                    if (parts.Length == 6)
                    {
                        lines.RemoveAt(0);

                        //Parse the line:

                        var queue = new Queue<string>(parts);
                        var PROXY_STRING = queue.Dequeue();
                        var INET_PROTOCOL = queue.Dequeue();
                        var CLIENT_IP = queue.Dequeue();
                        var PROXY_IP = queue.Dequeue();
                        var CLIENT_PORT = queue.Dequeue();
                        var PROXY_PORT = queue.Dequeue();

                        elbProxyHeader = new AwsElbProxyHeader()
                        {
                            InetProtocol = INET_PROTOCOL,
                            ClientIp = CLIENT_IP,
                            ClientPort = int.Parse(CLIENT_PORT),

                            ProxyIp = PROXY_IP,
                            ProxyPort = int.Parse(PROXY_PORT)
                        };
                    }
                }
            }

            var request = new AGIRequest(lines)
            {
                LocalAddress = socket.LocalAddress,
                LocalPort = socket.LocalPort,
                RemoteAddress = socket.RemoteAddress,
                RemotePort = socket.RemotePort,
                AwsElbProxyHeader = elbProxyHeader
            };

            return request;
        }

        public AGIReply ReadReply()
        {
            string line;
            var badSyntax = ((int) AGIReplyStatuses.SC_INVALID_COMMAND_SYNTAX).ToString();

            var lines = new List<string>();
            try
            {
                line = socket.ReadLine();
            }
            catch (IOException ex)
            {
                throw new AGINetworkException("Unable to read reply from Asterisk: " + ex.Message, ex);
            }
            if (line == null)
                throw new AGIHangupException();

            lines.Add(line);
            // read synopsis and usage if statuscode is 520
            if (line.StartsWith(badSyntax))
                try
                {
                    while ((line = socket.ReadLine()) != null)
                    {
                        lines.Add(line);
                        if (line.StartsWith(badSyntax))
                            break;
                    }
                }
                catch (IOException ex)
                {
                    throw new AGINetworkException("Unable to read reply from Asterisk: " + ex.Message, ex);
                }
            return new AGIReply(lines);
        }
    }
}