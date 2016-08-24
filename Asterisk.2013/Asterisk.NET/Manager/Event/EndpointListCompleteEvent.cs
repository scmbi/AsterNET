namespace AsterNET.Manager.Event
{
    /// <summary>
    /// A EndpointListCompleteEvent is triggered after the details of all peers has been reported 
    /// in response to an PJSIPShowEndpointsAction.<br/>
    /// </summary>
    /// <seealso cref="Manager.event.EndpointListEvent"/>
    /// <seealso cref="Manager.Action.PJSIPShowEndpointsAction"/>
    public class EndpointListCompleteEvent : ResponseEvent
	{
        public string EventList { get; set; }
        public string ListItems { get; set; }

        public EndpointListCompleteEvent(ManagerConnection source)
			: base(source)
		{
		}
	}
}