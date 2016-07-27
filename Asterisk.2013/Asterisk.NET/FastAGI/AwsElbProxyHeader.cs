using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AsterNET.FastAGI
{
    public class AwsElbProxyHeader
    {
        public string InetProtocol;
        public string ClientIp;
        public int ClientPort;

        public string ProxyIp;
        public int ProxyPort;
    }
}
