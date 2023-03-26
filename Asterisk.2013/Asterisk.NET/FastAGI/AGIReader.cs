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
    public class AGIReader
    {
        private readonly ILogger _logger;
        private readonly ISocketConnection _socket;
        private readonly Random _random; 

        public AGIReader(ISocketConnection socket, ILogger? logger = null)
        {
            _socket = socket;
            _logger = logger ?? new LoggerFactory().CreateLogger<AGIReader>();

            _random = new Random();
        }

        public AGIReply ReadReply(int? timeoutms = null)
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
                
                return new AGIReply(lines);
            }            
        }
    }
}