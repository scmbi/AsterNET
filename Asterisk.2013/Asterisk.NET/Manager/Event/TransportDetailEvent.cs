namespace AsterNET.Manager.Event
{
    /// <summary>
    /// A TransportDetailEvent is triggered in response to a PJSIPShowEndpointAction and contains information about the endpoint transport.<br/>
    /// It is implemented in channels/chan_pjsip.c
    /// </summary>
    public class TransportDetailEvent : ResponseEvent
	        {
        public string ObjectType { get; set; }
        public string ObjectName { get; set; }
        public string Protocol { get; set; }
        public string Bind { get; set; }
        public string AsycOperations { get; set; }
        public string CaListFile { get; set; }
        public string CaListPath { get; set; }
        public string CertFile { get; set; }
        public string PrivKeyFile { get; set; }
        public string Password { get; set; }
        public string ExternalSignalingAddress { get; set; }
        public string ExternalSignalingPort { get; set; }
        public string ExternalMediaAddress { get; set; }
        public string Domain { get; set; }
        public string VerifyServer { get; set; }
        public string VerifyClient { get; set; }
        public string RequireClientCert { get; set; }
        public string Method { get; set; }
        public string Cipher { get; set; }
        public string LocalNet { get; set; }
        public string Tos { get; set; }
        public string Cos { get; set; }
        public string WebsocketWriteTimeout { get; set; }
        public string EndpointName { get; set; }
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public TransportDetailEvent(ManagerConnection source)
			: base(source)
		{
		}
	}
}