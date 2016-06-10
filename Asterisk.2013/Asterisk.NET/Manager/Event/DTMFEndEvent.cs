namespace AsterNET.Manager.Event
{
    public class DTMFEndEvent : ManagerEvent
    {
        /// <summary>
        ///     Creates a new DialEvent.
        /// </summary>
        public DTMFEndEvent(ManagerConnection source) : base(source)
        {
        }

        public string ChannelState { get; set; }
        public string ChannelStateDesc { get; set; }
        public string CallerIDNum { get; set; }
        public string CallerIDName { get; set; }
        public string ConnectedLineNum { get; set; }
        public string ConnectedLineName { get; set; }
        public string AccountCode { get; set; }
        public string Context { get; set; }
        public string Exten { get; set; }
        public string Priority { get; set; }
        public string Linkedid { get; set; }
        public string Digit { get; set; }
        public string DurationMs { get; set; }
        public string Direction { get; set; }
    }
}