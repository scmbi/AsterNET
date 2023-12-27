using Sufficit.Asterisk.Manager;
using System;
namespace AsterNET.Manager
{
    /// <summary>
    /// An NotConnectedException is thrown when a send action fails due to a socket not connected.
    /// </summary>
    public class NotConnectedException : ManagerException
    {
        const string MESSAGE = "Underlaying connection is not ready";
        public NotConnectedException(string? msg = null) : base(msg ?? MESSAGE)
        {
            
        }
    }
}