﻿using Sufficit.Asterisk.IO;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AsterNET.IO
{
    public interface ISocketConnection
    {
        bool IsConnected();

        bool Initial { get; set; }

        bool IsHangUp { get; }

        IPAddress LocalAddress { get; }
        int LocalPort { get; }
        IPAddress RemoteAddress { get; }
        int RemotePort { get; }

        bool IsRemoteRequest { get; }

        void Close();

        void Write(string s);

        NetworkStream GetStream();
                
        //IEnumerable<string> ReadRequest(uint? timeoutms = null);

        IEnumerable<string> ReadRequest(CancellationToken cancellationToken);
        
        IEnumerable<string> ReadReply(uint? timeoutms = null);

        //IAsyncEnumerable<string> ReadReplyAsync(uint? timeoutms = null);

        /// <summary>
        ///     Monitor channel hangup event
        /// </summary>
        event EventHandler? OnHangUp;

        /// <summary>
        ///     Monitor dispose event
        /// </summary>
        event EventHandler? OnDisposing;

        /// <summary>
        ///		Triggered at socket disconnect event for any reason. <br />
        ///		Source parameter may be null because disposing <br />
		///		Nulls cause means expected behaviors <br />
        /// </summary>
        event EventHandler<string?>? OnDisconnected;

        IntPtr Handle { get; }

        AGISocketOptions Options { get; }
    }
}
