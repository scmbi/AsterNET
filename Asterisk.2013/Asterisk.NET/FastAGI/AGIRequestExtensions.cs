using System;
using System.Collections.Generic;
using System.Text;

namespace AsterNET.FastAGI
{
    public static class AGIRequestExtensions
    {
        public static Guid GetContextId(this AGIRequest source)
        {
            // trying via accountcode
            if (!Guid.TryParse(source.AccountCode, out Guid ContextId))
            {
                // trying via url parameter
                var idByParameter = source.Parameter("contextid");
                if (!string.IsNullOrWhiteSpace(idByParameter))
                    _ = Guid.TryParse(idByParameter, out ContextId);
            }

            return ContextId;
        }
    }
}
