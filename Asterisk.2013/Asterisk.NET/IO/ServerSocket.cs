using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace AsterNET.IO
{
	/// <summary>
	/// ServerSocket using standard socket classes.
	/// </summary>
	public class ServerSocket
	{
		private readonly TcpListener _listener;
		private readonly Encoding _encoding;

		public ServerSocket(int port, IPAddress bindAddress, Encoding encoding)
		{
            _encoding = encoding;
            _listener = new TcpListener(new IPEndPoint(bindAddress, port));
            _listener.Server.DualMode = true;
            _listener.Start();
		}
		
		public ISocketConnection Accept(ILoggerFactory? factory)
		{
			if (_listener != null)
			{
				var socket = _listener.AcceptSocket();
				if (socket != null)
					return (new SocketConnectionAsync(socket, _encoding, factory?.CreateLogger<ISocketConnection>()).Start());
			}
			return null;
		}
		
		public void Close()
		{
            _listener.Stop();
		}
	}
}
