namespace AsterNET.Manager.Event
{
    /// <summary>
    /// An IdentifyDetailEvent is triggered in response to a PJSIPShowEndpointAction and contains information about the identification of the endpoint.<br/>
    /// It is implemented in channels/chan_pjsip.c
    /// </summary>
	public class IdentifyDetailEvent : ResponseEvent
	{
        public string ObjectType { get; set; }
        public string ObjectName { get; set; }
        public string Endpoint { get; set; }
        public string Match { get; set; }
        public string EndpointName { get; set; }

		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public IdentifyDetailEvent(ManagerConnection source)
			: base(source)
		{
		}
	}
}