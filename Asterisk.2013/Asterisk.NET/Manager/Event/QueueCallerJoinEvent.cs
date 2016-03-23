using System;

namespace AsterNET.Manager.Event
{
    public class QueueCallerJoinEvent : ManagerEvent
    {
        private string queue;
        private int position;

        public string Queue
        {
            get { return this.queue; }
            set { this.queue = value; }
        }
        public int Position
        {
            get { return this.position; }
            set { this.position = value; }
        }

        /// <summary>
        /// Creates a new QueueCallerJoinEvent.
        /// </summary>
        public QueueCallerJoinEvent(ManagerConnection source)
            : base(source)
        {
        }
    }
}
