using Sufficit.Asterisk.Manager.Events;
using Sufficit.Asterisk.Manager.Events.Abstracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AsterNET.Manager
{
    public interface ManagerInvokable
    {
        void Invoke(object? sender, IManagerEvent e);

        int Count { get; }

        string Key { get; }

        bool IsAbstract { get; }

        Type Type { get; }
    }

    public class DisposableHandler<T> : IDisposable where T : IManagerEvent
    {
        private ManagerEventHandler<T> control;
        private EventHandler<T> action;
        public DisposableHandler(ManagerEventHandler<T> control, EventHandler<T> action)
        {
            this.control = control;
            this.action = action;

            this.control.Attach(action);
        }   

        public void Dispose()
        {
            this.control.Dettach(action);
        }
    }

    public class ManagerEventHandler<T> : ManagerInvokable where T : IManagerEvent
    {
        private event EventHandler<T>? Handler;

        public event EventHandler OnChanged;

        public string Key { get; }

        /// <summary>
        ///     Matches an base type class, abstract
        /// </summary>
        public Type Type { get; }

        public int Count => Handler?.GetInvocationList().Count() ?? 0;

        public bool IsAbstract => Type.IsAbstract;

        public ManagerEventHandler(string key)
        {
            Key = key;
            Type = typeof(T);
        }

        public void Invoke(object? sender, IManagerEvent e)
            => Handler?.Invoke(sender, (T)e);

        public void Attach(EventHandler<T> action)
        {
            Handler += action;
            OnChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Dettach(EventHandler<T> action)
        {
            Handler -= action;
            OnChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
