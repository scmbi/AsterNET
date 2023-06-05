using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public AGIRequest Read(int? timeoutms = null)
        {
            // reading id
            using (_logger.BeginScope<string>($"[RD:{_random.Next()}]"))
            {
                var lines = new List<string>();

                int count = 0;
                var result = _socket.ReadLines(timeoutms);
                foreach (var line in result)
                {
                    count++;
                    _logger.LogTrace($"line from reply ({count}): {line}");
                    lines.Add(line);
                }

                return new AGIRequest(lines)
                {
                    LocalAddress = _socket.LocalAddress,
                    LocalPort = _socket.LocalPort,
                    RemoteAddress = _socket.RemoteAddress,
                    RemotePort = _socket.RemotePort
                };
            }
        }
    }
}