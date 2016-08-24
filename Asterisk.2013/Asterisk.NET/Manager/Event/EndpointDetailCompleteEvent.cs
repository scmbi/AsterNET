namespace AsterNET.Manager.Event
{
    /// <summary>
    /// A EndpointDetailCompleteEvent is triggered after the details of all peers has been reported in response to an SIPPeersAction or SIPShowPeerAction.<br/>
    /// </summary>
    /// <seealso cref="Manager.event.AuthDetailEvent"/>
    /// <seealso cref="Manager.event.AorDetailEvent"/>
    /// <seealso cref="Manager.event.EndpointDetailEvent"/>
    /// <seealso cref="Manager.event.IdentifyDetailEvent"/>
    /// <seealso cref="Manager.event.TransportDetailEvent"/>
    /// <seealso cref="Manager.Action.PJSIPShowEndpointAction"/>
    public class EndpointDetailCompleteEvent : ResponseEvent
	{
        public string EventList { get; set; }
        public string ListItems { get; set; }

        public EndpointDetailCompleteEvent(ManagerConnection source)
			: base(source)
		{
		}
	}
}