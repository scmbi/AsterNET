using System;
using AsterNET.Manager.Event;

namespace AsterNET.Manager.Action
{
    /// <summary>
    ///     Retrieves a list of all defined SIP peers.<br />
    ///     For each peer that is found a PeerEntryEvent is sent by Asterisk containing
    ///     the details. When all peers have been reported a PeerlistCompleteEvent is sent.<br />
    ///     Available since Asterisk 1.2
    /// </summary>
    /// <seealso cref="Manager.event.EndpointListEvent" />
    /// <seealso cref="Manager.event.EndpointListCompleteEvent" />
    public class PJSIPShowEndpointsAction : ManagerActionEvent
    {
        public override string Action
        {
            get { return "PJSIPShowEndpoints"; }
        }

        public override Type ActionCompleteEventClass()
        {
            return typeof (EndpointListCompleteEvent);
        }
    }
}