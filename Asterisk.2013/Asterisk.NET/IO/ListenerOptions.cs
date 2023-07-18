using Sufficit.Asterisk.IO;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace AsterNET.IO
{
    public class ListenerOptions : AGISocketExtendedOptions
    {
        /// <summary>
        ///     Maximum pending waiting connections
        /// </summary>
        public uint BackLog { get; set; }
            = 10;
    }
}
