using System.IO;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System;

namespace AsterNET.IO
{
	/// <summary>
	/// Socket connection to asterisk.
	/// </summary>
	public class SocketConnection
	{
		public int ReceiveBufferSize { get; internal set; }

		private NetworkStream? networkStream;
		private StreamReader reader;
		private BinaryWriter writer;
		private Encoding encoding;
		private bool initial;

		#region Constructor - SocketConnection(string host, int port, int receiveTimeout) 

		/// <summary>
		/// Consructor
		/// </summary>
		/// <param name="host">client host</param>
		/// <param name="port">client port</param>
		/// <param name="encoding">encoding</param>
		public SocketConnection(string host, int port, Encoding encoding)
			:this(new TcpClientMonitor(host, port), encoding)
		{
		}

		/// <summary>
		/// Consructor
		/// </summary>
		/// <param name="host">client host</param>
		/// <param name="port">client port</param>
		/// <param name="encoding">encoding</param>
		/// <param name="receiveBufferSize">size of the receive buffer.</param>
		public SocketConnection(string host, int port, int receiveBufferSize, Encoding encoding)
			: this (new TcpClientMonitor(host, port) { ReceiveBufferSize = receiveBufferSize }, encoding)
		{
        }

        #endregion

        #region Constructor - SocketConnection(socket) 

        public SocketConnection(TcpClientMonitor tcpClient, Encoding encoding)
            : this((TcpClient)tcpClient, encoding)
        {
            tcpClient.OnDisposing += TcpClient_OnDisposing;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="tcpClient">TCP client from Listener</param>
        /// <param name="encoding">encoding</param>
        internal SocketConnection(TcpClient tcpClient, Encoding encoding)
		{
            ReceiveBufferSize = tcpClient.ReceiveBufferSize;

            initial = true;
			this.encoding = encoding;
			this.TcpClient = tcpClient;

			this.networkStream = this.TcpClient.GetStream();
			this.reader = new StreamReader(this.networkStream, encoding);
			this.writer = new BinaryWriter(this.networkStream, encoding);
		}

        private void TcpClient_OnDisposing(object _, EventArgs __)
        {
			TcpClient = null;
        }

		#endregion

        public NetworkStream GetStream()
			=> TcpClient.GetStream();

		/// <summary>
		/// Indicates that the underlaying tcp client is not null and not disposed
		/// </summary>
        public bool IsValid => TcpClient != null;

		/// <summary>
		/// Can be null, because it can be disposed from inside
		/// </summary>
		protected TcpClient? TcpClient { get; set; }

        /// <summary>
        /// Can be null, because it can be disposed from inside
        /// </summary>
        protected NetworkStream? NetworkStream { get; }

        public Encoding Encoding
		{
			get { return encoding; }
		}

		public bool Initial
		{
			get { return initial; }
			set { initial = value; }
		}

		#region IsConnected

		/// <summary>
		/// Returns the connection state of the socket.
		/// </summary>
		public bool IsConnected
		{
			get { return TcpClient?.Connected ?? false; }
		}

		#endregion

		#region LocalAddress 
		public IPAddress LocalAddress
		{
			get
			{
				return ((IPEndPoint)(TcpClient.Client.LocalEndPoint)).Address;
			}
		}
		#endregion

		#region LocalPort 
		public int LocalPort
		{
			get
			{
				return ((IPEndPoint)(TcpClient.Client.LocalEndPoint)).Port;
			}
		}
		#endregion

		#region RemoteAddress 
		public IPAddress RemoteAddress
		{
			get
			{
				return ((IPEndPoint)(TcpClient.Client.RemoteEndPoint)).Address;
			}
		}
		#endregion

		#region RemotePort 
		public int RemotePort
		{
			get
			{
				return ((IPEndPoint)(TcpClient.Client.LocalEndPoint)).Port;
			}
		}
		#endregion

		#region ReadLine()
		/// <summary>
		/// Reads a line of text from the socket connection. The current thread is
		/// blocked until either the next line is received or an IOException
		/// encounters.
		/// </summary>
		/// <returns>the line of text received excluding any newline character</returns>
		/// <throws>  IOException if the connection has been closed. </throws>
		public string ReadLine()
		{
			string line = null;
			try
			{
				line = reader.ReadLine();
			}
			catch
			{
				line = null;
			}
			return line;
		}

		public string ReadToEnd()
		{
			string line = string.Empty;
			do
			{
				try
				{					
					line += reader.ReadLine();
				}
				catch { line = null; break; }
			} while (reader.Peek() >= 0);
			return line;
		}
		#endregion

		#region Write(string s)
		/// <summary>
		/// Sends a given String to the socket connection.
		/// </summary>
		/// <param name="s">the String to send.</param>
		/// <throws> IOException if the String cannot be sent, maybe because the </throws>
		/// <summary>connection has already been closed.</summary>
		public void Write(string s)
		{
			writer.Write(encoding.GetBytes(s));
			writer.Flush();
		}
		#endregion

		#region Write(string msg) 
		/// <summary>
		/// Sends a given String to the socket connection.
		/// </summary>
		/// <param name="msg">the String to send.</param>
		/// <throws> IOException if the String cannot be sent, maybe because the </throws>
		/// <summary>connection has already been closed.</summary>
		public void WriteEx(string msg)
		{
			byte[] data = encoding.GetBytes(msg);
			networkStream.BeginWrite(data, 0, data.Length, onWriteFinished, networkStream);
			networkStream.Flush();
		}

		private void onWriteFinished(IAsyncResult ar)
		{
			var stream = (NetworkStream)ar.AsyncState;
			stream.EndWrite(ar);
		}
		#endregion

		#region Close
		/// <summary>
		/// Closes the socket connection including its input and output stream and
		/// frees all associated ressources.<br/>
		/// When calling close() any Thread currently blocked by a call to readLine()
		/// will be unblocked and receive an IOException.
		/// </summary>
		/// <throws>  IOException if the socket connection cannot be closed. </throws>
		public void Close()
		{
			try
			{
				if(TcpClient != null)
				{
                    TcpClient.Client?.Shutdown(SocketShutdown.Both);
                    TcpClient.Client?.Close();
                    TcpClient.Close();
                }
			}
			catch { }
		}
		#endregion
	}
}
