namespace AsterNET.Manager.Event
{
    /// <summary>
    ///     A CdrEvent is triggered when a call detail record is generated, usually at the end of a call.<br />
    ///     To enable CdrEvents you have to add enabled = yes to the general section in
    ///     cdr_manager.conf.<br />
    ///     This event is implemented in cdr/cdr_manager.c
    /// </summary>
    public class CoreShowChannelEvent : ManagerEvent
    {
        public CoreShowChannelEvent(ManagerConnection source)
            : base(source)
        {
        }

        public string ActionID { get; set; }
        public string Channel { get; set; }
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
        public string Uniqueid { get; set; }
        public string Linkedid { get; set; }
        public string BridgeId { get; set; }
        public string Application { get; set; }
        public string ApplicationData { get; set; }
        public string Duration { get; set; }
    }
}