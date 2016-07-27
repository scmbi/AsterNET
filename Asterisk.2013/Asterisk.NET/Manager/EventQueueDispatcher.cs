using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading;
using AsterNET.Manager.Event;

namespace AsterNET.Manager
{
    public class EventQueueDispatcher
    {
        #if LOGGER
        private Logger logger = Logger.Instance();
        #endif

        private readonly Thread _internalThread;
        private readonly ManualResetEvent _queueReset = new ManualResetEvent(false);
        private readonly ConcurrentQueue<ManagerEvent> _eventQueue = new ConcurrentQueue<ManagerEvent>();
        private readonly Action<ManagerEvent> _eventReady;

        public EventQueueDispatcher(Action<ManagerEvent> eventReady)
        {
            _eventReady = eventReady;
            _internalThread = new Thread(() => Run(this));
            _internalThread.Start();
        }

        public void QueueEvent(ManagerEvent e)
        {
            _eventQueue.Enqueue(e);
            _queueReset.Set();
        }

        private static void Run(EventQueueDispatcher dispatcher)
        {
            while (true)
            {
                ManagerEvent readyEvent;
                if (!dispatcher._eventQueue.TryDequeue(out readyEvent))
                {
                    dispatcher._queueReset.Reset();
#if LOGGER
                    dispatcher.logger.Debug("Dispatcher queue: Waiting for events");
#endif
                    dispatcher._queueReset.WaitOne();
                }
                else
                {
#if LOGGER
                    dispatcher.logger.Debug("Dispatcher queue: Dispatching event " + readyEvent.GetType().Name);
#endif
                    dispatcher._eventReady(readyEvent);
                }
            }
        }
    }
}
