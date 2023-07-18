using System.Collections;
using System.Collections.Generic;
using AsterNET.Manager.Event;
using AsterNET.Manager.Response;
using Sufficit.Asterisk.Manager.Events;

namespace AsterNET.Manager
{
    /// <summary>
    ///     Collection of ResponseEvent. Use in events generation actions.
    /// </summary>
    public class ResponseEvents
    {
        private readonly List<IResponseEvent> events;

        /// <summary>
        ///     Creates a new <see cref="ResponseEvents"/>.
        /// </summary>
        public ResponseEvents()
        {
            events = new List<IResponseEvent>();
            Complete = false;
        }

        /// <summary>
        ///     Gets or sets the response.
        /// </summary>
        public ManagerResponse Response { get; set; }

        /// <summary>
        ///     Gets the list of events.
        /// </summary>
        public List<IResponseEvent> Events
        {
            get { return events; }
        }

        /// <summary>
        ///     Indicates if all events have been received.
        /// </summary>
        public bool Complete { get; set; }

        /// <summary>
        ///     Adds a ResponseEvent that has been received.
        /// </summary>
        /// <param name="e"><see cref="IResponseEvent"/></param>
        public void AddEvent(IResponseEvent e)
        {
            lock (((IList) events).SyncRoot)
            {
                events.Add(e);
            }
        }
    }
}