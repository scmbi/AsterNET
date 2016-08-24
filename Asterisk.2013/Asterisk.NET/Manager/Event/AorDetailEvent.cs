namespace AsterNET.Manager.Event
{
    /// <summary>
    /// An AorDetailEvent is triggered in response to a PJSIPShowEndpointAction and contains information about the aors of the endpoint.<br/>
    /// It is implemented in channels/chan_pjsip.c
    /// </summary>
	public class AorDetailEvent : ResponseEvent
	{
        public string ObjectType { get; set; }
        public string ObjectName { get; set; }
        public string MinimumExpiration { get; set; }
        public string MaximumExpiration { get; set; }
        public string DefaultExpiration { get; set; }
        public string QualifyFrequency { get; set; }
        public string AuthenticateQualify { get; set; }
        public string MaxContacts { get; set; }
        public string RemoveExisting { get; set; }
        public string Mailboxes { get; set; }
        public string OutboundProxy { get; set; }
        public string SupportPath { get; set; }
        public string TotalContacts { get; set; }
        public string ContactsRegistered { get; set; }
        public string EndpointName { get; set; }
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public AorDetailEvent(ManagerConnection source)
			: base(source)
		{
		}
	}
}