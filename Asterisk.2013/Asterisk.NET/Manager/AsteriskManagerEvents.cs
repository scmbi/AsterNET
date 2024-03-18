using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using AsterNET;
using AsterNET.Helpers;
using AsterNET.Manager;
using AsterNET.Manager.Event;
using Microsoft.Extensions.Logging;
using Sufficit.Asterisk.Manager.Events;

namespace Sufficit.Asterisk.Manager
{
    /// <summary>
    /// Default implementation of the ManagerConnection interface.
    /// </summary>
    public class AsteriskManagerEvents : IDisposable
    {
        /// <summary>
        /// If this property set to <b>true</b> then ManagerConnection send all unassigned events to UnhandledEvent handler,<br/>
        /// if set to <b>false</b> then all unassgned events lost and send only UnhandledEvent.<br/>
        /// Default: <b>false</b>
        /// </summary>
        public bool FireAllEvents { get; set; } = false;

        /// <summary>
        /// Allows you to specifiy how events are fired. If false (default) then
        /// events will be fired in order. Otherwise events will be fired as they arrive and 
        /// control logic in your application will need to handle synchronization.
        /// </summary>
        public bool Async { get; set; } = false;

        #region STATIC PUBLIC

        public static string GetEventKey<T>() where T : IManagerEvent
            => GetEventKey(typeof(T));

        public static string GetEventKey(IManagerEvent e)
            => GetEventKey(e.GetType());

        public static string GetEventKey(Type e)
            => GetEventKey(e.Name);

        public static string GetEventKey(string @event)
        {
            if (string.IsNullOrWhiteSpace(@event))
                throw new ArgumentNullException(nameof(@event)); 

            var key = @event.Trim().ToLowerInvariant();
            if (key.EndsWith("event"))
                key = key.Substring(0, key.Length - 5);

            return key;
        }

        /// <summary>
        /// Update internal logger
        /// </summary>
        public static void Log (ILogger logger) => _logger = logger;

        #endregion
        #region STATIC PRIVATE

        private static string StripInternalActionId(string actionId)
        {
            if (string.IsNullOrEmpty(actionId))
                return string.Empty;
            int delimiterIndex = actionId.IndexOf(Common.INTERNAL_ACTION_ID_DELIMITER);
            if (delimiterIndex > 0)
            {
                if (actionId.Length > delimiterIndex + 1)
                    return actionId.Substring(delimiterIndex + 1).Trim();
                return actionId.Substring(0, delimiterIndex).Trim();
            }
            return string.Empty;
        }

        private static string GetInternalActionId(string actionId)
        {
            if (string.IsNullOrEmpty(actionId))
                return string.Empty;
            int delimiterIndex = actionId.IndexOf(Common.INTERNAL_ACTION_ID_DELIMITER);
            if (delimiterIndex > 0)
                return actionId.Substring(0, delimiterIndex).Trim();
            return string.Empty;
        }

        private static ILogger _logger
            = new LoggerFactory().CreateLogger<Helper>();

        private static object _lockDiscovered = new object();
        private static IEnumerable<Type>? DiscoveredTypes;

        private static bool AssemblyMatch(Assembly assembly)
        {
            return
                !assembly.IsDynamic && assembly.FullName != null && (
                assembly.FullName.StartsWith(nameof(Sufficit), true, CultureInfo.InvariantCulture) ||
                assembly.FullName.StartsWith(nameof(AsterNET), true, CultureInfo.InvariantCulture)
                );
        }

        private static IEnumerable<Type> GetDiscoveredTypes()
        {
            // Thread safe
            lock (_lockDiscovered)
            {
                if (DiscoveredTypes == null)
                {
                    var manager = typeof(IManagerEvent);
                    var discovered = new List<Type>();

                    var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                    foreach (var assembly in assemblies)
                    {
                        // searching in assemblies
                        if (!AssemblyMatch(assembly))
                            continue;

                        IEnumerable<Type> types = Array.Empty<Type>();
                        try { types = assembly.GetTypes(); }
                        catch (Exception ex)
                        {
                            if (ex is System.Reflection.ReflectionTypeLoadException typeLoadException)
                            {
                                foreach (var loaderException in typeLoadException.LoaderExceptions)
                                    _logger.LogError(loaderException, "error getting types on assembly: {assembly}", assembly);
                            }
                            else _logger.LogError(ex, "error getting types on assembly: {assembly}", assembly);
                            continue;
                        }

                        foreach (var type in types)
                        {
                            if (type.IsPublic && !type.IsAbstract && manager.IsAssignableFrom(type))
                                discovered.Add(type);
                        }
                    }

                    DiscoveredTypes = discovered;
                }

                return DiscoveredTypes;
            }
        }

        private static void RegisterEventClass(Dictionary<string, ConstructorInfo> list, Type clazz)
        {
            // Ignore all abstract classes
            // Class not derived from IManagerEvent
            if (clazz.IsAbstract || !typeof(IManagerEvent).IsAssignableFrom(clazz))
                return;

            string eventKey = GetEventKey(clazz);
                       
            // If assignable from UserEvent and no "userevent" at the start - add "userevent" to beginning
            if (typeof(UserEvent).IsAssignableFrom(clazz) && !eventKey.StartsWith("user"))
                eventKey = "user" + eventKey;

            if (list.ContainsKey(eventKey))
                return;

            ConstructorInfo? constructor = null;
            try
            {
                constructor = clazz.GetConstructor(Array.Empty<Type>());
                if (constructor == null)
                {
                    constructor = clazz.GetConstructor(new[] { typeof(ManagerConnection) });
                }
            }
            catch (MethodAccessException ex)
            {
                throw new ArgumentException("RegisterEventClass : " + clazz + " has no usable constructor", ex);
            }

            if (constructor != null && constructor.IsPublic)
                list.Add(eventKey, constructor);
            else
                throw new ArgumentException("RegisterEventClass : " + clazz + " has no public default constructor");
        }

        /// <summary>
        ///     Register buildin Event classes or any executing assembly
        /// </summary>
        /// <param name="list"></param>
        private static void RegisterBuiltinEventClasses(Dictionary<string, ConstructorInfo> list)
        {
            foreach (var type in GetDiscoveredTypes())
                RegisterEventClass(list, type);
        }

        private static void RegisterEventHandler(Dictionary<string, Func<IManagerEvent, bool>> list, Type eventType, Func<IManagerEvent, bool> action)
        {
            string eventKey = GetEventKey(eventType);
            if (list.ContainsKey(eventKey))
                throw new ArgumentException("Event class already registered : " + eventKey);

            list.Add(eventKey, action);
        }

        #endregion
        #region INTERNAL - FROM MANAGER

        /// <summary>
        /// Dispatching from manager
        /// </summary>
        internal void Dispatch(object? sender, IManagerEvent e)
        {
            if (Async)
                _ = Task.Run(() => DispatchInternal(sender, e)).ConfigureAwait(false);
            else
                DispatchInternal(sender, e);
        }

        /// <summary>
        ///     Builds the event based on the given map of attributes and the registered event classes.
        /// </summary>
        /// <param name="attributes">map containing event attributes</param>
        /// <returns>a concrete instance of IManagerEvent or null if no event class was registered for the event type.</returns>
        internal ManagerEventGeneric Build(Dictionary<string, string> attributes)
        {
            ManagerEventGeneric e;
            ConstructorInfo? constructor = null;

            string eventKey = GetEventKey(attributes["event"]);
            if (eventKey == "user")
            {
                string userevent = attributes["userevent"].Trim().ToLowerInvariant();
                if (!string.IsNullOrWhiteSpace(userevent))
                    eventKey = "user" + userevent;
            }

            if (registeredEventClasses.ContainsKey(eventKey))
                constructor = registeredEventClasses[eventKey];

            if (constructor == null)
            {
                e = new ManagerEventGeneric<UnknownEvent>();
                string s = string.Join(";", attributes.Select(x => x.Key + "=" + x.Value).ToArray());
                _logger.LogWarning("unknown event: {s}", s);
            }
            else
            {
                try
                {
                    var generic = (IManagerEvent)constructor.Invoke(null);
                    e = new ManagerEventGeneric(generic);

                    _logger.LogTrace("creating event: {generic}", generic);
                }

#if LOGGER
                catch (Exception ex)
                {
                    _logger.LogError("Unable to create new instance of " + eventKey, ex);
                    return null;
                }
#else
                catch { throw; }
#endif
            }            

            Helper.SetAttributes(e, attributes, _logger);

            if (e.HasAttributes())
            {
                var generatedType = e.Event.GetType();
                _logger.LogDebug("Generating event ({type}): {json}", generatedType, e.ToJson());
            }

            /* // testing
            if (e.Event is HangupEvent)
            {
                var json = JsonSerializer.Serialize(e);
                _logger.LogWarning(json);
            }
            */

            // ResponseEvents are sent in response to a ManagerAction if the
            // response contains lots of data. They include the actionId of
            // the corresponding ManagerAction.
            if (e.Event is IResponseEvent responseEvent)
            {
                string actionId = responseEvent.ActionId;
                if (actionId != null)
                {
                    responseEvent.ActionId = StripInternalActionId(actionId);
                    responseEvent.InternalActionId = GetInternalActionId(actionId);
                }
            }

            return e;
        }

        #endregion

        /// <summary>
        /// Register User Event Class
        /// </summary>
        /// <param name="userEventClass"></param>
        public void RegisterUserEventClass(Type userEventClass)
        {
            RegisterEventClass(registeredEventClasses, userEventClass);
        }

        /// <summary>
        /// Knonwing classes to build events. <br />
        /// If not listed, will be generated a generic event
        /// </summary>
        private readonly Dictionary<string, ConstructorInfo> registeredEventClasses;

        /// <summary>
        /// User handlers for events
        /// </summary>
        private readonly HashSet<AsteriskManagerEventHandler> _handlers;

        public AsteriskManagerEvents()
        {
            _handlers = new HashSet<AsteriskManagerEventHandler>();
            registeredEventClasses = new Dictionary<string, ConstructorInfo>();

            RegisterBuiltinEventClasses(registeredEventClasses);       
        }

        public delegate void AsteriskManagerEventHandler(object? sender, IManagerEvent e);

        public delegate void AsteriskManagerEventHandler<T>(object? sender, T e) where T : IManagerEvent;

        private HashSet<ManagerInvokable> Handlers = new HashSet<ManagerInvokable>();

        public IDisposable On<T>(EventHandler<T> action) where T : IManagerEvent
        {
            string eventKey = GetEventKey<T>();
            var handler = Handlers.FirstOrDefault(s => s.Key == eventKey) as ManagerEventHandler<T>;
            if (handler == null)
            {
                handler = new ManagerEventHandler<T>(eventKey);  
                handler.OnChanged += OnHandlerChanged;
                Handlers.Add(handler);
            }

            return new DisposableHandler<T>(handler, action);
        }

        private void OnHandlerChanged(object? sender, EventArgs e)
        {
            if (sender is ManagerInvokable handler && handler.Count == 0)            
                Handlers.Remove(handler);            
        }

        /// <summary>
        /// Dispatching after defined if async or sync
        /// </summary>
        private void DispatchInternal(object? sender, IManagerEvent e)
        {
            string eventKey = GetEventKey(e);
            if (Handlers.Any())
            {
                var handler = Handlers.FirstOrDefault(s => s.Key == eventKey);
                if (handler != null)
                {
                    handler.Invoke(sender, e);
                    return;
                }

                // lets try with abstract classes
                var eventtype = e.GetType();
                handler = Handlers.FirstOrDefault(s => s.Type.IsAssignableFrom(eventtype));
                if (handler != null)
                {
                    handler.Invoke(sender, e);
                    return;
                }
            }

            if (FireAllEvents)
            {
                _logger.LogDebug("dispatching unhandled event: {0}", e);                
                UnhandledEvent?.Invoke(sender, e);
            }
        }

        public void Dispose()
        {
            UnhandledEvent = null;
            Handlers.Clear();
            registeredEventClasses.Clear();
        }

        /// <summary>
        /// An UnhandledEvent is triggered on unknown event.
        /// </summary>
        public event EventHandler<IManagerEvent>? UnhandledEvent;
    }
}
