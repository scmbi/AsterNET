using System;
using System.Collections.Generic;
using System.Text;

namespace AsterNET.FastAGI
{
    /// <summary>
    ///     AGI Script parameters, enclosure for <see cref="AGIRequest"/> and <see cref="AGIChannel"/> <br/>
    ///     Facilitates extensions method that needs these two
    /// </summary>
    public class AGIScriptParameters
    {
        public AGIScriptParameters(AGIRequest request, AGIChannel channel)
        {
            Request = request;
            Channel = channel;
        }

        public AGIRequest Request { get; }

        public AGIChannel Channel { get; }
    }
}
