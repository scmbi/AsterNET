namespace AsterNET.Manager.Event
{
    /// <summary>
    /// Event that describes the AMI event DialEnd.
    /// </summary>
    public class DialEndEvent : ManagerEvent
    {
        #region Constructors

        public DialEndEvent(ManagerConnection source) : base(source)
        {
        }

        #endregion

        public string Event { get; set; }
        public string Privilege { get; set; }
        public string Channel { get; set; }
        public string ChannelState { get; set; }
        public string ChannelStateDesc { get; set; }
        public string CallerIDNum { get; set; }
        public string CallerIDName { get; set; }
        public string ConnectedLineNum { get; set; }
        public string ConnectedLineName { get; set; }
        public string Language { get; set; }
        public string AccountCode { get; set; }
        public string Context { get; set; }
        public string Exten { get; set; }
        public string Priority { get; set; }
        public string DestChannel { get; set; }
        public string DestChannelState { get; set; }
        public string DestChannelStateDesc { get; set; }
        public string DestCallerIDNum { get; set; }
        public string DestCallerIDName { get; set; }
        public string DestConnectedLineNum { get; set; }
        public string DestConnectedLineName { get; set; }
        public string DestLanguage { get; set; }
        public string DestAccountCode { get; set; }
        public string DestContext { get; set; }
        public string DestExten { get; set; }
        public string DestPriority { get; set; }
        public string DestUniqueid { get; set; }
        public string DialStatus { get; set; }
    }
}