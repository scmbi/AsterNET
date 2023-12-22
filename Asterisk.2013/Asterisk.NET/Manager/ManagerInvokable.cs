using Sufficit.Asterisk.Manager.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace AsterNET.Manager
{
    public interface ManagerInvokable
    {
        void Invoke(object? sender, IManagerEvent e);

        int Count { get; }

        string Key { get; }

        bool IsAbstract { get; }

        Type Type { get; }
    }
}
