using System;

namespace AsterNET.Manager.Event
{
    public class DeviceStateChangeEvent : ManagerEvent
    {
        private string device;
        private string state;

        public string Device
        {
            get { return this.device; }
            set { this.device = value; }
        }
        public string State
        {
            get { return this.state; }
            set { this.state = value; }
        }

        /// <summary>
        /// Creates a new DeviceStateChangeEvent.
        /// </summary>
        public DeviceStateChangeEvent(ManagerConnection source)
            : base(source)
        {
        }
    }
}
