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
    public static class EventQueueDispatcher
    {
        #if LOGGER
        private static Logger logger = Logger.Instance();
        #endif

        private static Thread InternalThread = new Thread(Run);
        private static ManualResetEvent QueueReset = new ManualResetEvent(false);
        private static ConcurrentQueue<ManagerEvent> EventQueue = new ConcurrentQueue<ManagerEvent>();
        public static Action<ManagerEvent> EventReady;

        static EventQueueDispatcher()
        {
            InternalThread.Start();
        }

        public static void QueueEvent(ManagerEvent e)
        {
            EventQueue.Enqueue(e);
            QueueReset.Set();
        }

        private static void Run()
        {
            while (true)
            {
                ManagerEvent readyEvent;
                if (!EventQueue.TryDequeue(out readyEvent))
                {
                    QueueReset.Reset();
#if LOGGER
                    logger.Debug("Dispatcher queue: Waiting for events");
#endif
                    QueueReset.WaitOne();
                }
                else
                {
#if LOGGER
                    logger.Debug("Dispatcher queue: Dispatching event " + readyEvent.GetType().Name);
#endif
                    EventReady(readyEvent);
                }
            }
        }
    }
}
