namespace AsterNET.Manager.Event
{
    /// <summary>
    ///     A CdrEvent is triggered when a call detail record is generated, usually at the end of a call.<br />
    ///     To enable CdrEvents you have to add enabled = yes to the general section in
    ///     cdr_manager.conf.<br />
    ///     This event is implemented in cdr/cdr_manager.c
    /// </summary>
    public class CoreShowChannelsCompleteEvent : ManagerEvent
    {
        public CoreShowChannelsCompleteEvent(ManagerConnection source)
            : base(source)
        {
        }

        public string ActionID { get; set; }
        public string EventList { get; set; }
        public int ListItems { get; set; }
    }
}