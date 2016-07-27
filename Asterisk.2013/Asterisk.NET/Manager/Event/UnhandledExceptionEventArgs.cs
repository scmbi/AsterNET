using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AsterNET.Manager.Event
{
    /// <summary>
    /// An event thrown when an exception is thrown by an event handler.
    /// </summary>
    public class UnhandledExceptionEventArgs : EventArgs
    {
        public ManagerEvent ManagerEvent;
        public Exception ThrownException;
    }
}
