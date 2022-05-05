using AsterNET.Manager.Event;
using Microsoft.Extensions.Logging;
using Sufficit.Asterisk.Manager.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using AsterNET.Helpers;

namespace AsterNET.Manager
{
    public static class ManagerConnectionExtensions
    {
        #region BuildEvent(Hashtable list, object source, IDictionary attributes) 

        /// <summary>
        ///     Builds the event based on the given map of attributes and the registered event classes.
        /// </summary>
        /// <param name="source">source attribute for the event</param>
        /// <param name="list"></param>
        /// <param name="attributes">map containing event attributes</param>
        /// <returns>a concrete instance of IManagerEvent or null if no event class was registered for the event type.</returns>
        internal static ManagerEventGeneric BuildEvent(this ManagerConnection source, IDictionary<int, ConstructorInfo> list, Dictionary<string, string> attributes)
        {
            ManagerEventGeneric e;
            string eventType;
            ConstructorInfo constructor = null;
            int hash, hashEvent;

            eventType = attributes["event"].Trim().ToLowerInvariant();
            // Remove Event tail from event name (ex. JabberEvent)
            if (eventType.EndsWith("event"))
                eventType = eventType.Trim().ToLowerInvariant().Substring(0, eventType.Length - 5);
            hashEvent = eventType.GetHashCode();

            if (eventType == "user")
            {
                string userevent = attributes["userevent"].Trim().ToLowerInvariant();
                hash = string.Concat(eventType, userevent).GetHashCode();
                if (list.ContainsKey(hash))
                    constructor = list[hash];
                else
                    constructor = list[hashEvent];
            }
            else if (list.ContainsKey(hashEvent))
                constructor = list[hashEvent];

            if (constructor == null)
            {
                e = new ManagerEventGeneric<UnknownEvent>();
                string s = string.Join(";", attributes.Select(x => x.Key + "=" + x.Value).ToArray());
                source.Logger.LogWarning($"unknown event: { s }");
            }
            else
            {
                try
                {
                    var generic = (IManagerEvent)constructor.Invoke(null);
                    e = new ManagerEventGeneric(generic);

                    source.Logger.LogTrace($"creating event: {generic}");
                }
                catch (Exception ex)
                {
#if LOGGER
                    source.Logger.LogError("Unable to create new instance of " + eventType, ex);
                    return null;
#else
					throw ex;
#endif
                }
            }

            Helper.SetAttributes(e, attributes, source.Logger);

            if (e.HasAttributes())
            {
                var generatedType = e.Event.GetType();
                source.Logger.LogDebug($"Generating event ({generatedType}): {e.ToJson()}");
            }

            // ResponseEvents are sent in response to a ManagerAction if the
            // response contains lots of data. They include the actionId of
            // the corresponding ManagerAction.
            if (e.Event is IResponseEvent responseEvent)
            {
                string actionId = responseEvent.ActionId;
                if (actionId != null)
                {
                    responseEvent.ActionId = Helper.StripInternalActionId(actionId);
                    responseEvent.InternalActionId = Helper.GetInternalActionId(actionId);
                }
            }

            return e;
        }

        #endregion
    }
}
