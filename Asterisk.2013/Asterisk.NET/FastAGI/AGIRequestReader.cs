using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using AsterNET.IO;
using AsterNET.Manager;
using Microsoft.Extensions.Logging;

namespace AsterNET.FastAGI
{
    /// <summary>
    /// Reads the initial agi request
    /// </summary>
    public class AGIRequestReader
    {
        private readonly ILogger _logger;
        private readonly ISocketConnection _socket;
        private readonly Random _random; 

        public AGIRequestReader(ISocketConnection socket, ILogger<AGIRequestReader> logger)
        {
            _socket = socket;
            _logger = logger;

            _random = new Random();
        }

        public AGIRequest Read()
        {
            // reading id
            using (_logger.BeginScope<string>($"[RD:{_random.Next()}]"))
            {
                var lines = new List<string>();
                try
                {
                    _logger.LogTrace("AGIReader.ReadRequest():");                   
                    foreach (var line in _socket.ReadLines())
                    {
                        lines.Add(line);
                        _logger.LogTrace($"line from request ({line})");
                    }
                }
                catch (IOException ex)
                {
                    throw new AGINetworkException("Unable to read request from Asterisk: " + ex.Message, ex);
                }

                var request = new AGIRequest(lines)
                {
                    LocalAddress = _socket.LocalAddress,
                    LocalPort = _socket.LocalPort,
                    RemoteAddress = _socket.RemoteAddress,
                    RemotePort = _socket.RemotePort
                };

                return request;
            }
        }
    }
}