using System;

namespace AsterNET.Manager.Event
{
    public class QueueCallerLeaveEvent : ManagerEvent
    {
        private string queue;

        public string Queue
        {
            get { return this.queue; }
            set { this.queue = value; }
        }

        /// <summary>
        /// Creates a new DNDStateEvent.
        /// </summary>
        public QueueCallerLeaveEvent(ManagerConnection source)
            : base(source)
        {
        }
    }
}
