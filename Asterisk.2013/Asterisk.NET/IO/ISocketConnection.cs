using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace AsterNET.IO
{
    public interface ISocketConnection
    {
        bool Connected { get; }

        bool Initial { get; set; }

        bool IsHangUp { get; }

        int ReceiveBufferSize { get; }

        IPAddress LocalAddress { get; }
        int LocalPort { get; }
        IPAddress RemoteAddress { get; }
        int RemotePort { get; }

        bool IsRemoteRequest { get; }

        void Close();

        void Write(string s);

        NetworkStream GetStream();

        Encoding Encoding { get; }

        IEnumerable<string> ReadLines(int? timeoutms = null);

        string? ReadLine();

        event EventHandler? OnHangUp;
    }
}
