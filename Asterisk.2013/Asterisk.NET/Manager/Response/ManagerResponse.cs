using Sufficit.Asterisk;
using Sufficit.Asterisk.Manager.Events;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using AsterNET.Helpers;
using Sufficit.Asterisk.Manager.Events.Abstracts;

namespace AsterNET.Manager.Response
{
    /// <summary>
    ///     Represents a response received from the Asterisk server as the result of a
    ///     previously sent ManagerAction.<br />
    ///     The response can be linked with the action that caused it by looking the
    ///     action id attribute that will match the action id of the corresponding
    ///     action.
    /// </summary>
    public class ManagerResponse : ManagerEvent, IParseSupport
    {
        public object GetSetter() => this;

        #region Constructor - ManagerResponse() 

        public ManagerResponse() { }

        public ManagerResponse(Dictionary<string, string> attributes) : this()
        {
            Helper.SetAttributes(this, attributes);
        }

        #endregion

        #region Attributes 

        /// <summary>
        ///     Store all unknown (without setter) keys from manager event.<br />
        ///     Use in default Parse method <see cref="Parse" />.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, string>? Attributes { get; internal set; }

        #endregion

        #region Server 

        /// <summary>
        ///     Specify a server to which to send your commands (x.x.x.x or hostname).<br />
        ///     This should match the server name specified in your config file's "host" entry.
        ///     If you do not specify a server, the proxy will pick the first one it finds -- fine in single-server configurations.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Server { get; set; }

        #endregion

        #region ActionId 

        /// <summary>
        ///     Get/Set the action id received with this response referencing the action that generated this response.
        /// </summary>
        public string ActionId { get; set; }

        #endregion

        #region Message 

        /// <summary>
        ///     Get/Set the message received with this response.<br />
        ///     The content depends on the action that generated this response.
        /// </summary>
        public string Message { get; set; }

        #endregion

        #region Response 

        /// <summary>
        ///     Get/Set the value of the "Response:" line.<br />
        ///     This typically a String like "Success" or "Error" but depends on the action that generated this response.
        /// </summary>
        public string Response { get; set; }

        #endregion

        #region UniqueId 

        /// <summary>
        ///     Get/Set the unique id received with this response.<br />
        ///     The unique id is used to keep track of channels created by the action sent, for example an OriginateAction.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? UniqueId { get; set; }

        #endregion

        #region IsSuccess() 

        /// <summary>
        ///     Return true if Response is "success", or "goodbye" for logoff <br />
        ///     (Extra: yes, asterisk sends a "goodbye" msg, a despite off success, why we need standards in the world ?!) fuck developers ...
        /// </summary>
        /// <returns></returns>
        public bool IsSuccess()
        {
            var response = Response.ToLower().Trim();
            return response == "success" || response == "goodbye";
        }

        #endregion

        #region GetAttribute(string key) 

        /// <summary>
        ///     Returns the value of the attribute with the given key.<br />
        ///     This is particulary important when a response contains special
        ///     attributes that are dependent on the action that has been sent.<br />
        ///     An example of this is the response to the GetVarAction.
        ///     It contains the value of the channel variable as an attribute
        ///     stored under the key of the variable name.<br />
        ///     Example:
        ///     <pre>
        ///         GetVarAction action = new GetVarAction();
        ///         action.setChannel("SIP/1310-22c3");
        ///         action.setVariable("ALERT_INFO");
        ///         ManagerResponse response = connection.SendAction(action);
        ///         String alertInfo = response.getAttribute("ALERT_INFO");
        ///     </pre>
        ///     As all attributes are internally stored in lower case the key is
        ///     automatically converted to lower case before lookup.
        /// </summary>
        /// <param name="key">the key to lookup.</param>
        /// <returns>
        ///     the value of the attribute stored under this key or
        ///     null if there is no such attribute.
        /// </returns>
        public string GetAttribute(string key)
        {
            return (string)Attributes[key.ToLower(Helper.CultureInfo)];
        }

        #endregion

        #region Parse(string key, string value) 

        /// <summary>
        ///     Unknown properties parser
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

        #endregion

        #region ParseSpecial(Dictionary<string, string> attributes)

        /// <inheritdoc cref="IParseSupport.ParseSpecial(Dictionary{string, string}?)"/>
        public virtual Dictionary<string, string> ParseSpecial (Dictionary<string, string>? attributes)
            => attributes ?? new Dictionary<string, string>();
        

        #endregion


        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull | JsonIgnoreCondition.WhenWritingDefault)]
        public string? EventList { get; set; }

        #region ToString() 

        public override string ToString()
        {
            return Helper.ToString(this);
        }

        #endregion
    }
}