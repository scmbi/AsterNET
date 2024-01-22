using Sufficit.Asterisk;
using Sufficit.Asterisk.Manager.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace AsterNET.Manager.Event
{
    /// <summary>
    /// Abstract base class for all Events that can be received from the Asterisk server.<br/>
    /// Events contain data pertaining to an event generated from within the Asterisk
    /// core or an extension module.<br/>
    /// There is one conrete subclass of ManagerEvent per each supported Asterisk Event.
    /// 
    /// Channel / Privilege / UniqueId are not common to all events and should be moved to
    /// derived event classes.
    /// </summary>
    class ManagerEventGeneric<T> : ManagerEventGeneric where T : IManagerEvent, new()
    {
        public new T Event => (T)base.Event;

        public static implicit operator T(ManagerEventGeneric<T> @event) => @event.Event;

        public ManagerEventGeneric() : base(new T()) { }
    }

    public class ManagerEventGeneric : IParseSupport
    {
        public IManagerEvent Event { get; }

        public ManagerEventGeneric(IManagerEvent source)
        {
            Event = source;
            Attributes = new Dictionary<string, string>();
        }

        public object GetSetter() => Event;

        /// <summary>
        /// This generic instance should not have any attribute after the full preparation. 
        /// If its remaining, improve the underlayng classes.
        /// </summary>
        public bool HasAttributes() => Attributes?.Count > 0;

        /// <summary>
        /// Store all unknown (without setter) keys from manager event.<br/>
        /// Use in default Parse method <see cref="IParseSupport.Parse(string, string)"/>
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, string>? Attributes { get; internal set; }

        #region Methods

        /// <summary>
        /// Unknown properties parser
        /// </summary>
        /// <param name="key">key name</param>
        /// <param name="value">key value</param>
        /// <returns>true - value parsed, false - can't parse value</returns>
        public virtual void Parse (string key, string value)
        {
            if (Attributes == null)
                Attributes = new Dictionary<string, string>();

            if (Attributes.ContainsKey(key))
                // Key already presents, add with delimiter
                Attributes[key] += string.Concat(Common.LINE_SEPARATOR, value);
            else            
                Attributes.Add(key, value);      
        }

        /// <summary>
        /// Unknown properties parser.
        /// </summary>
        /// <param name="attributes">dictionary</param>
        /// <returns>updated dictionary</returns>
        public virtual Dictionary<string, string> ParseSpecial(Dictionary<string, string>? attributes)
        {
            if (attributes == null)
                return new Dictionary<string, string>();

            foreach (var pair in attributes)            
                Parse(pair.Key, pair.Value);
            
            return Attributes ?? new Dictionary<string, string>(); ;
        }

        #endregion
    }

}
