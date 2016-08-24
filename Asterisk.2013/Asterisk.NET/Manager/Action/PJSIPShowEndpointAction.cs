using System;
using AsterNET.Manager.Event;

namespace AsterNET.Manager.Action
{
    /// <summary>
    ///     Retrieves a the details about a given SIP peer.<br />
    ///     For a EndpointListEvent is sent by Asterisk containing the details of the peer
    ///     followed by a EndpointListCompleteEvent.<br />
    /// </summary>
    /// <seealso cref="Manager.event.AorDetailEvent" />
    /// <seealso cref="Manager.event.EndpointDetailEvent" />
    /// <seealso cref="Manager.event.AuthDetailEvent" />
    /// <seealso cref="Manager.event.TransportDetailEvent" />
    /// <seealso cref="Manager.event.IdentifyDetailEvent" />
    /// <seealso cref="Manager.event.EndpointDetailCompleteEvent" />

    public class PJSIPShowEndpointAction : ManagerActionEvent
    {
        /// <summary> Creates a new empty PJSIPShowEndpointAction.</summary>
        public PJSIPShowEndpointAction()
        {
        }

        /// <summary>
        ///     Creates a new PJSIPShowEndpointAction that requests the details about the given SIP endpoint.
        /// </summary>
        public PJSIPShowEndpointAction(string endpoint)
        {
            this.Endpoint = endpoint;
        }

        public override string Action
        {
            get { return "PJSIPShowEndpoint"; }
        }

        /// <summary>
        ///     Get/Set the name of the peer to retrieve.<br />
        ///     This parameter is mandatory.
        /// </summary>
        public string Endpoint { get; set; }

        public override Type ActionCompleteEventClass()
        {
            return typeof (EndpointDetailCompleteEvent);
        }
    }
}