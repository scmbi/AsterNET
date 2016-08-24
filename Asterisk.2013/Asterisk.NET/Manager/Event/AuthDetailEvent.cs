namespace AsterNET.Manager.Event
{
    /// <summary>
    /// An AuthDetailEvent is triggered in response to a PJSIPShowEndpointAction and contains information about the auth of the endpoint.<br/>
    /// It is implemented in channels/chan_pjsip.c
    /// </summary>
	public class AuthDetailEvent : ResponseEvent
	{
        public string ObjectType { get; set; }
        public string ObjectName { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Md5Cred { get; set; }
        public string Realm { get; set; }
        public string NonceLifetime { get; set; }
        public string AuthType { get; set; }
        public string EndpointName { get; set; }

		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public AuthDetailEvent(ManagerConnection source)
			: base(source)
		{
		}
	}
}