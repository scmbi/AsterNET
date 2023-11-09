using AsterNET.Manager.Event;
using Microsoft.Extensions.Logging;
using Sufficit.Asterisk.Manager.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using AsterNET.Helpers;
using AsterNET.Manager.Action;
using AsterNET.Manager.Response;
using System.Threading.Tasks;
using System.Threading;

namespace AsterNET.Manager
{
    public static class ManagerConnectionExtensions
    {
        public static Task<ManagerResponse> SendActionAsync<T>(this ManagerConnection source, CancellationToken cancellationToken = default) where T : ManagerAction, new()
            => source.SendActionAsync(new T(), cancellationToken);

        public static async Task<CommandResponse> SendCommandAsync(this ManagerConnection source, CommandAction action, CancellationToken cancellationToken)
            => (CommandResponse) await source.SendActionAsync(action, cancellationToken);
    }
}
