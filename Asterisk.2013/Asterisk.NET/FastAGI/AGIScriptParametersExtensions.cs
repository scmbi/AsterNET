using System;
using System.Collections.Generic;
using System.Text;

namespace AsterNET.FastAGI
{
    public static class AGIScriptParametersExtensions
    {
        /// <summary>
        ///     Gets an asterisk channel header parameter value
        /// </summary>
        /// <param name="key">case insensivite for header key</param>
        public static string? Header (this AGIScriptParameters source, string key)
        {
            string? varname = null;

            var normalizedkey = key.ToUpperInvariant();
            var normalized = source.Request.Type.ToUpperInvariant();
            switch (normalized)
            {
                case "PJSIP": varname = $"PJSIP_HEADER(read,{normalizedkey})"; break;
                case "SIP": varname = $"SIP_HEADER({normalizedkey})"; break;
                case "LOCAL": varname = $"HEADER({normalizedkey})"; break;
            }

            if (!string.IsNullOrWhiteSpace(varname))
                return source.Channel.GetVariable(varname);
            else
                return null;
        }

        /// <summary>
        ///     Updates or Remove an asterisk channel header parameter
        /// </summary>
        public static void Header(this AGIScriptParameters source, string key, string? value)
        {
            throw new NotImplementedException();
        }
    }
}