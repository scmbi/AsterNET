namespace AsterNET.Manager.Event
{
    /// <summary>
    /// A EndpointListEvent is triggered in response to a PJSIPShowEndpointsAction
    /// and contains information about an endpoint.<br/>
    /// </summary>
    public class EndpointListEvent : ResponseEvent
	{
        public string ObjectType { get; set; }
        public string ObjectName { get; set; }
        public string Transport { get; set; }
        public string Aor { get; set; }
        public string Auths { get; set; }
        public string OutboundAuths { get; set; }
        public string DeviceState { get; set; }
        public string ActiveChannels { get; set; }

        /// <summary>
        /// Get/Set the status of this peer.<br/>
        /// For SIP peers this is one of:
        /// <dl>
        /// <dt>"UNREACHABLE"</dt>
        /// <dd></dd>
        /// <dt>"LAGGED (%d ms)"</dt>
        /// <dd></dd>
        /// <dt>"OK (%d ms)"</dt>
        /// <dd></dd>
        /// <dt>"UNKNOWN"</dt>
        /// <dd></dd>
        /// <dt>"Unmonitored"</dt>
        /// <dd></dd>
        /// </dl>
        /// </summary>
        /*
        public string Status
		{
			get { return this.status; }
			set { this.status = value; }
		}*/

		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public EndpointListEvent(ManagerConnection source)
			: base(source)
		{
		}
	}
}