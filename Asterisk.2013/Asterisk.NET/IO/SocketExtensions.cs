using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Text;

namespace AsterNET.IO
{
    public static class SocketExtensions
    {
        public static bool IsRemoteRequest(this Socket source)
        {
            var remote = source.RemoteEndPoint;
            var local = source.LocalEndPoint;

            return IsRemoteRequest(remote, local);
        }

        public static bool IsRemoteRequest(this TcpClient source)
            => IsRemoteRequest(source.Client.RemoteEndPoint, source.Client.LocalEndPoint);

        public static bool IsRemoteRequest(EndPoint remote, EndPoint local)
        {
            if (remote is IPEndPoint remoteIp && local is IPEndPoint localIp)
            {
                if (remoteIp.Address.MapToIPv4().Equals(localIp.Address.MapToIPv4()))
                    return false;
            }

            if (remote.AddressFamily == AddressFamily.InterNetworkV6)
            {
                if (remote.AddressFamily == AddressFamily.InterNetwork)
                    return false;
                else
                    return true;
            }
            else if (remote.AddressFamily == AddressFamily.InterNetwork)
            {
                return false;
            }
            else return true;
        }
    }
}
