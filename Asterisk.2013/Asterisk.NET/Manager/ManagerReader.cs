using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Xml;
using AsterNET.Helpers;
using AsterNET.IO;
using AsterNET.Manager.Action;
using AsterNET.Manager.Event;
using AsterNET.Manager.Response;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sufficit.Asterisk.Manager.Events;

namespace AsterNET.Manager
{
	/// <summary>
	///     Default implementation of the ManagerReader interface.
	/// </summary>
	public class ManagerReader
	{
		private readonly ILogger _logger;
		private readonly ManagerConnection mrConnector;
		private ISocketConnection? mrSocket;

		private bool die;
		private bool is_logoff;
		private bool disconnect;
		private byte[]? lineBytes;
		private string? lineBuffer;
		private readonly Queue<string> lineQueue;
		private ResponseHandler? pingHandler;
		private bool processingCommandResult;
		private bool wait4identiier;
		private DateTime lastPacketTime;
		private readonly Dictionary<string, string> packet;
		private readonly List<string> commandList;

        public bool IsConnected()
            => mrSocket?.IsConnected() ?? false;

		/// <summary>
		/// Disconnect cause
		/// </summary>
		public Exception? Exception { get; private set; }

		protected void Close(Exception ex)
		{
			Exception = ex;
            _logger.LogError(ex, "Read data error: {message}", ex.Message);

			// Any catch - disconnect !
			if (mrSocket != null) 
			{ 
				if (mrSocket.IsConnected())
					mrSocket.Close("manager reader, close");

                mrSocket = null;
            }

            disconnect = true;
        }

        #region ManagerReader(dispatcher, asteriskServer) 

        /// <summary>
        ///     Creates a new ManagerReader.
        /// </summary>
        /// <param name="connection">the dispatcher to use for dispatching events and responses.</param>
        public ManagerReader(ManagerConnection connection)
		{
            _logger = connection.Logger;
            mrConnector = connection;
			die = false;
			lineQueue = new Queue<string>();
			packet = new Dictionary<string, string>();
			commandList = new List<string>();
        }

		#endregion

		#region Socket 

		/// <summary>
		///     Sets the socket to use for reading from the asterisk server.
		/// </summary>
		internal ISocketConnection? Socket
		{
			set { mrSocket = value; }
		}

		#endregion

		#region Die 

		internal bool Die
		{
			get { return die; }
			set
			{
				die = value;
				if (die)
					mrSocket = null;
			}
		}

		#endregion

		#region IsLogoff 

		internal bool IsLogoff
		{
			set { is_logoff = value; }
		}

		#endregion

		#region mrReaderCallback(IAsyncResult ar) 

		/// <summary>
		/// Async Read callback
		/// </summary>
		/// <param name="ar">IAsyncResult</param>
		private static void mrReaderCallback(IAsyncResult ar)
		{
			if (ar.AsyncState is ManagerReader mrReader)
			{
				if (mrReader.die)
					return;

				if (!mrReader.IsConnected())
				{
					// No socket - it's DISCONNECT !!!
					mrReader.disconnect = true;
					return;
				}

				var mrSocket = mrReader.mrSocket;
                if (mrSocket == null)
                {
                    // No socket available - it's DISCONNECT !!!
                    mrReader.disconnect = true;
                    return;
                }

                var nstream = mrSocket.GetStream();
				if (nstream == null)
				{
					// No network stream - it's DISCONNECT !!!
					mrReader.disconnect = true;
					return;
				}

				try
				{
					int count = nstream.EndRead(ar);
					if (count == 0)
					{
						// No received data - it's may be DISCONNECT !!!
						if (!mrReader.is_logoff)
							mrReader.disconnect = true;
						return;
					}

					if (mrReader.lineBytes == null)
					{
                        // Invalid bytes buffer !!! it's may be DISCONNECT Or Not Configured !!!
                        mrReader.disconnect = true;
                        return;
                    }

					string line = mrSocket.Options.Encoding.GetString(mrReader.lineBytes, 0, count);
					mrReader.lineBuffer += line;
					int idx;
					// \n - because not all dev in Digium use \r\n
					// .Trim() kill \r
					lock (((ICollection)mrReader.lineQueue).SyncRoot)
						while (!string.IsNullOrEmpty(mrReader.lineBuffer) && (idx = mrReader.lineBuffer.IndexOf('\n')) >= 0)
						{
							line = idx > 0 ? mrReader.lineBuffer.Substring(0, idx).Trim() : string.Empty;
							mrReader.lineBuffer = (idx + 1 < mrReader.lineBuffer.Length
								? mrReader.lineBuffer.Substring(idx + 1)
								: string.Empty);
							mrReader.lineQueue.Enqueue(line);
						}
					// Give a next portion !!!
					nstream.BeginRead(mrReader.lineBytes, 0, mrReader.lineBytes.Length, mrReaderCallback, mrReader);
				}
				catch (Exception ex)
				{
					mrReader.Close(ex);
				}
			}
		}

		#endregion

		#region Reinitialize 

		internal void Reinitialize()
		{			
			disconnect = false;
			lineQueue.Clear();
			packet.Clear();
			commandList.Clear();
			lineBuffer = string.Empty;            
			lastPacketTime = DateTime.Now;
			wait4identiier = true;
			processingCommandResult = false;
			lastPacketTime = DateTime.Now; 
			
			if (mrSocket != null)
            {
                mrSocket.Initial = false;
                lineBytes = new byte[mrSocket.Options.BufferSize];
                mrSocket.GetStream()?.BeginRead(lineBytes, 0, lineBytes.Length, mrReaderCallback, this);
            }
        }

		#endregion

		#region Run()

		/// <summary>
		/// Reads line by line from the asterisk server, sets the protocol identifier as soon as it is
		/// received and dispatches the received events and responses via the associated dispatcher.
		/// </summary>
		/// <seealso cref="ManagerConnection.DispatchResponse(Response.ManagerResponse)" />
		internal void Run(object parameter)
		{
			if (mrSocket == null)
				throw new SystemException("Unable to run: socket is null.");

			CancellationTokenSource cts;
			if (parameter is CancellationToken cancellationToken)
				cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			else 
				cts = new CancellationTokenSource();

			while (!cts.IsCancellationRequested)
			{
				try
                {
                    string line;
                    while (!die)
					{
						#region check line from *

						if (!is_logoff)
						{
							if (mrSocket != null && mrSocket.Initial)
							{
								Reinitialize();
							}
							else if (disconnect)
							{
								disconnect = false;
								mrConnector.DispatchEvent(new DisconnectEvent());
							}
						}
						if (lineQueue.Count == 0)
						{
							if (lastPacketTime.AddMilliseconds(mrConnector.PingInterval) < DateTime.Now
								&& mrConnector.PingInterval > 0
								&& mrSocket != null
								&& !wait4identiier
								&& !is_logoff
								)
							{
								if (pingHandler != null)
								{
									if (pingHandler.Response == null)
									{
										// If one PingInterval from Ping without Pong then send Disconnect event
										mrConnector.RemoveResponseHandler(pingHandler);
										mrConnector.DispatchEvent(new DisconnectEvent());
									}
									pingHandler.Free();
									pingHandler = null;
								}
								else
								{
									// Send PING to *
									try
									{
										pingHandler = new ResponseHandler(new PingAction(), null);
										mrConnector.SendAction(pingHandler.Action, pingHandler);
									}
									catch
									{
										disconnect = true;
										mrSocket = null;
									}
								}
								lastPacketTime = DateTime.Now;
							}
							Thread.Sleep(50);

							continue;
						}

						#endregion

						lastPacketTime = DateTime.Now;
						lock (((ICollection) lineQueue).SyncRoot)
							line = lineQueue.Dequeue().Trim();
						
						_logger.LogTrace(line);

						#region processing Response: Follows

						if (processingCommandResult)
						{
							string lineLower = line.ToLower(Helper.CultureInfo);
							if (lineLower == "--end command--" || lineLower == "")
							{
								var commandResponse = new CommandResponse();
								Helper.SetAttributes(commandResponse, packet);
								commandResponse.Result = commandList;
								processingCommandResult = false;
								packet.Clear();
								mrConnector.DispatchResponse(commandResponse);
							}
							else if (lineLower.StartsWith("privilege: ")
								|| lineLower.StartsWith("actionid: ")
								|| lineLower.StartsWith("timestamp: ")
								|| lineLower.StartsWith("server: ")
								)
								Helper.AddKeyValue(packet, line);
							else
								commandList.Add(line);
							continue;
						}

						#endregion

						#region collect key: value and ProtocolIdentifier

						if (!string.IsNullOrEmpty(line))
						{
							if (wait4identiier && line.StartsWith("Asterisk Call Manager"))
							{
								wait4identiier = false;
								var connectEvent = new ConnectEvent();
								connectEvent.ProtocolIdentifier = line;
								mrConnector.DispatchEvent(connectEvent);
								continue;
							}
							if (line.Trim().ToLower(Helper.CultureInfo) == "response: follows"
								|| line.Trim().ToLower(Helper.CultureInfo).EndsWith("command output follows"))
							{
								// Switch to wait "--END COMMAND--"/"" mode
								processingCommandResult = true;
								commandList.Clear();
								Helper.AddKeyValue(packet, line);
								continue;
							}
							Helper.AddKeyValue(packet, line);
							continue;
						}

						#endregion

						#region process events and responses

						if (packet.ContainsKey("event"))
							mrConnector.DispatchEvent(packet);

						else if (packet.ContainsKey("response"))
							mrConnector.DispatchResponse(packet);

						#endregion

						packet.Clear();
					}

					if (mrSocket != null)
						mrSocket.Close("manager reader, run");

					break;
				}
				catch (Exception ex)
				{
                    _logger.LogError(ex, "Exception : {0}", ex.Message);				
				}

				if (die)
					break;

                _logger.LogTrace("No die, any error - send disconnect.");
				mrConnector.DispatchEvent(new DisconnectEvent());
			}
		}

		#endregion
	}
}
