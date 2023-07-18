using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace AsterNET.IO
{
    /// <summary>
    /// Try to get disposed event, tcp connections closed by servers
    /// </summary>
    public class TcpClientMonitor : TcpClient
    {
        public TcpClientMonitor(string hostname, int port) : base(hostname, port) { }

        /// <summary>
        /// Monitor dispose event
        /// </summary>
        public event EventHandler? OnDisposing;

        protected override void Dispose(bool disposing)
        {
            OnDisposing?.Invoke(this, EventArgs.Empty);
            base.Dispose(disposing);
        }
    }
}
