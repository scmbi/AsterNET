using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using AsterNET.Manager;
using AsterNET.Manager.Event;
using AsterNET.Manager.Response;
using Microsoft.Extensions.Logging;
using Sufficit.Asterisk;
using Sufficit.Asterisk.Manager;
using Sufficit.Asterisk.Manager.Events;

namespace AsterNET.Helpers
{
    internal class Helper
    {
        /// <summary>
        /// Update internal logger
        /// </summary>
        public static void Log(ILogger logger) => _logger = logger;

        private static CultureInfo defaultCulture;

        private static ILogger _logger 
            = new LoggerFactory().CreateLogger<Helper>();

        #region CultureInfo 

        internal static CultureInfo CultureInfo
        {
            get
            {
                if (defaultCulture == null)
                    defaultCulture = CultureInfo.GetCultureInfo("en");
                return defaultCulture;
            }
        }

        #endregion

        #region ToHexString(sbyte[]) 

        /// <summary> The hex digits used to build a hex string representation of a byte array.</summary>
        internal static readonly char[] hexChar =
        {
            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd',
            'e', 'f'
        };

        /// <summary>
        ///     Converts a byte array to a hex string representing it. The hex digits are lower case.
        /// </summary>
        /// <param name="b">the byte array to convert</param>
        /// <returns> the hex representation of b</returns>
        internal static string ToHexString(sbyte[] b)
        {
            var sb = new StringBuilder(b.Length*2);
            for (int i = 0; i < b.Length; i++)
            {
                sb.Append(hexChar[URShift((b[i] & 0xf0), 4)]);
                sb.Append(hexChar[b[i] & 0x0f]);
            }
            return sb.ToString();
        }

        #endregion

        #region GetInternalActionId(actionId) 

        internal static string GetInternalActionId(string actionId)
        {
            if (string.IsNullOrEmpty(actionId))
                return string.Empty;
            int delimiterIndex = actionId.IndexOf(Common.INTERNAL_ACTION_ID_DELIMITER);
            if (delimiterIndex > 0)
                return actionId.Substring(0, delimiterIndex).Trim();
            return string.Empty;
        }

        #endregion

        #region StripInternalActionId(actionId) 

        internal static string StripInternalActionId(string actionId)
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

        #endregion

        #region IsTrue(string) 

        /// <summary>
        ///     Checks if a String represents true or false according to Asterisk's logic.<br />
        ///     The original implementation is util.c is as follows:
        /// </summary>
        /// <param name="s">the String to check for true.</param>
        /// <returns>
        ///     true if s represents true,
        ///     false otherwise.
        /// </returns>
        internal static bool IsTrue(string s)
        {
            if (s == null || s.Length == 0)
                return false;
            string sx = s.ToLower(CultureInfo);
            if (sx == "yes" || sx == "true" || sx == "y" || sx == "t" || sx == "1" || sx == "on")
                return true;
            return false;
        }

        #endregion

        #region URShift(...) 

        /// <summary>
        ///     Performs an unsigned bitwise right shift with the specified number
        /// </summary>
        /// <param name="number">Number to operate on</param>
        /// <param name="bits">Ammount of bits to shift</param>
        /// <returns>The resulting number from the shift operation</returns>
        internal static int URShift(int number, int bits)
        {
            if (number >= 0)
                return number >> bits;
            return (number >> bits) + (2 << ~bits);
        }

        /// <summary>
        ///     Performs an unsigned bitwise right shift with the specified number
        /// </summary>
        /// <param name="number">Number to operate on</param>
        /// <param name="bits">Ammount of bits to shift</param>
        /// <returns>The resulting number from the shift operation</returns>
        internal static int URShift(int number, long bits)
        {
            return URShift(number, (int) bits);
        }

        /// <summary>
        ///     Performs an unsigned bitwise right shift with the specified number
        /// </summary>
        /// <param name="number">Number to operate on</param>
        /// <param name="bits">Ammount of bits to shift</param>
        /// <returns>The resulting number from the shift operation</returns>
        internal static long URShift(long number, int bits)
        {
            if (number >= 0)
                return number >> bits;
            return (number >> bits) + (2L << ~bits);
        }

        /// <summary>
        ///     Performs an unsigned bitwise right shift with the specified number
        /// </summary>
        /// <param name="number">Number to operate on</param>
        /// <param name="bits">Ammount of bits to shift</param>
        /// <returns>The resulting number from the shift operation</returns>
        internal static long URShift(long number, long bits)
        {
            return URShift(number, (int) bits);
        }

        #endregion

        #region ToArray(ICollection c, object[] objects) 

        /// <summary>
        ///     Obtains an array containing all the elements of the collection.
        /// </summary>
        /// <param name="objects">The array into which the elements of the collection will be stored.</param>
        /// <param name="c"></param>
        /// <returns>The array containing all the elements of the collection.</returns>
        internal static object[] ToArray(ICollection c, object[] objects)
        {
            int index = 0;

            Type type = objects.GetType().GetElementType();
            var objs = (object[]) Array.CreateInstance(type, c.Count);

            IEnumerator e = c.GetEnumerator();

            while (e.MoveNext())
                objs[index++] = e.Current;

            //If objects is smaller than c then do not return the new array in the parameter
            if (objects.Length >= c.Count)
                objs.CopyTo(objects, 0);

            return objs;
        }

        #endregion

        #region ParseVariables(Dictionary<string, string> dictionary, string variables, char[] delim)

        /// <summary>
        ///     Parse variable(s) string to dictionary.
        /// </summary>
        /// <param name="dictionary"></param>
        /// <param name="variables">variable(a) string</param>
        /// <param name="delim">variable pairs delimiter</param>
        /// <returns></returns>
        internal static Dictionary<string, string> ParseVariables(Dictionary<string, string> dictionary,
            string variables, char[] delim)
        {
            if (dictionary == null)
                dictionary = new Dictionary<string, string>();
            else
                dictionary.Clear();

            if (string.IsNullOrEmpty(variables))
                return dictionary;
            string[] vars = variables.Split(delim);
            int idx;
            string vname, vval;
            foreach (var var in vars)
            {
                idx = var.IndexOf('=');
                if (idx > 0)
                {
                    vname = var.Substring(0, idx);
                    vval = var.Substring(idx + 1);
                }
                else
                {
                    vname = var;
                    vval = string.Empty;
                }
                dictionary.Add(vname, vval);
            }
            return dictionary;
        }

        #endregion

        #region JoinVariables(IDictionary dictionary, string delim) 

        /// <summary>
        ///     Join variables dictionary to string.
        /// </summary>
        /// <param name="dictionary"></param>
        /// <param name="delim"></param>
        /// <param name="delimKeyValue"></param>
        /// <returns></returns>
        internal static string JoinVariables(NameValueCollection? collection, char[] delim, string delimKeyValue)
            => JoinVariables(collection, new string(delim), delimKeyValue);
        
        internal static string JoinVariables(NameValueCollection? collection, string delim, string delimKeyValue)
        {
            if (collection == null)
                return string.Empty;

            var sb = new StringBuilder();
            var items = collection.AllKeys.SelectMany(collection.GetValues, (k, v) => new { Key = k, Value = v });
            foreach (var pair in items)
            {
                if (sb.Length > 0)
                    sb.Append(delim);
                sb.Append(string.Concat(pair.Key, delimKeyValue, pair.Value));
            }
            return sb.ToString();
        }

        /// <summary>
        ///     Join variables dictionary to string.
        /// </summary>
        /// <param name="dictionary"></param>
        /// <param name="delim"></param>
        /// <param name="delimKeyValue"></param>
        /// <returns></returns>
        internal static string JoinVariables(IDictionary collection, char[] delim, string delimKeyValue)
        {
            return JoinVariables(collection, new string(delim), delimKeyValue);
        }
        internal static string JoinVariables(IDictionary? dictionary, string delim, string delimKeyValue)
        {
            if (dictionary == null)
                return string.Empty;

            var sb = new StringBuilder();
            foreach (DictionaryEntry pair in dictionary)
            {
                if (sb.Length > 0)
                    sb.Append(delim);
                sb.Append(string.Concat(pair.Key, delimKeyValue, pair.Value));
            }
            return sb.ToString();
        }

        #endregion

        #region GetMillisecondsFrom(DateTime start) 

        internal static long GetMillisecondsFrom(DateTime start)
        {
            TimeSpan ts = DateTime.Now - start;
            return (long) ts.TotalMilliseconds;
        }

        #endregion

        #region ParseString(string val) 

        internal static object ParseString(string val)
        {
            if (val == "none")
                return string.Empty;
            return val;
        }

        #endregion

        #region GetGetters(class) 

        /// <summary>
        ///     Returns a Map of getter methods of the given class.<br />
        ///     The key of the map contains the name of the attribute that can be accessed by the getter, the
        ///     value the getter itself . A method is considered a getter if its name starts with "get",
        ///     it is declared internal and takes no arguments.
        /// </summary>
        /// <param name="clazz">the class to return the getters for</param>
        /// <returns> a Map of attributes and their accessor methods (getters)</returns>
        internal static Dictionary<string, MethodInfo> GetGetters(Type clazz)
        {
            string name;
            string methodName;
            MethodInfo method;

            var accessors = new Dictionary<string, MethodInfo>();
            MethodInfo[] methods = clazz.GetMethods();

            for (int i = 0; i < methods.Length; i++)
            {
                method = methods[i];
                methodName = method.Name;

                // skip not "get..." methods and  skip methods with != 0 parameters
                if (!methodName.StartsWith("get_") || method.GetParameters().Length != 0)
                    continue;

                name = methodName.Substring(4);
                if (name.Length == 0)
                    continue;
                accessors[name] = method;
            }
            return accessors;
        }

        #endregion

        #region GetSetters(Type clazz) 

        /// <summary>
        ///     Returns a Map of setter methods of the given class.<br />
        ///     The key of the map contains the name of the attribute that can be accessed by the setter, the
        ///     value the setter itself. A method is considered a setter if its name starts with "set",
        ///     it is declared internal and takes no arguments.
        /// </summary>
        /// <param name="clazz">the class to return the setters for</param>
        /// <returns> a Map of attributes and their accessor methods (setters)</returns>
        internal static IDictionary GetSetters(Type clazz)
        {
            IDictionary accessors = new Hashtable();
            MethodInfo[] methods = clazz.GetMethods();
            string name;
            string methodName;
            MethodInfo method;

            for (int i = 0; i < methods.Length; i++)
            {
                method = methods[i];
                methodName = method.Name;
                // skip not "set..." methods and  skip methods with != 1 parameters
                if (!methodName.StartsWith("set_") || method.GetParameters().Length != 1)
                    continue;
                name = methodName.Substring("set_".Length).ToLower(CultureInfo);
                if (name.Length == 0) continue;
                accessors[name] = method;
            }
            return accessors;
        }

        #endregion

        #region ToString(object obj) 

        /// <summary>
        ///     Convert object with all properties to string
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        internal static string ToString(object obj)
        {
            object value;
            var sb = new StringBuilder(obj.GetType().Name, 1024);
            sb.Append(" {");
            string strValue;
            IDictionary getters = GetGetters(obj.GetType());
            bool notFirst = false;
            var arrays = new List<MethodInfo>();
            // First step - all values properties (not a list)
            foreach (string name in getters.Keys)
            {
                var getter = (MethodInfo) getters[name];
                Type propType = getter.ReturnType;
                if (propType == typeof (object))
                    continue;
                if (
                    !(propType == typeof (string) || propType == typeof (bool) || propType == typeof (double) ||
                      propType == typeof (DateTime) || propType == typeof (int) || propType == typeof (long)))
                {
                    string propTypeName = propType.Name;
                    if (propTypeName.StartsWith("Dictionary") || propTypeName.StartsWith("List"))
                    {
                        arrays.Add(getter);
                    }
                    continue;
                }

                try
                {
                    value = getter.Invoke(obj, new object[] {});
                }
                catch
                {
                    continue;
                }

                if (value == null)
                    continue;
                if (value is string)
                {
                    strValue = (string) value;
                    if (strValue.Length == 0)
                        continue;
                }
                else if (value is bool)
                {
                    strValue = ((bool) value ? "true" : "false");
                }
                else if (value is double)
                {
                    var d = (double) value;
                    if (d == 0.0)
                        continue;
                    strValue = d.ToString();
                }
                else if (value is DateTime)
                {
                    var dt = (DateTime) value;
                    if (dt == DateTime.MinValue)
                        continue;
                    strValue = dt.ToLongTimeString();
                }
                else if (value is int)
                {
                    var i = (int) value;
                    if (i == 0)
                        continue;
                    strValue = i.ToString();
                }
                else if (value is long)
                {
                    var l = (long) value;
                    if (l == 0)
                        continue;
                    strValue = l.ToString();
                }
                else
                    strValue = value.ToString();

                if (notFirst)
                    sb.Append("; ");
                notFirst = true;
                sb.Append(string.Concat(getter.Name.Substring(4), ":", strValue));
            }

            // Second step - all lists
            foreach (var getter in arrays)
            {
                value = null;
                try
                {
                    value = getter.Invoke(obj, new object[] {});
                }
                catch
                {
                    continue;
                }
                if (value == null)
                    continue;

                #region List 

                IList list;
                if (value is IList && (list = (IList) value).Count > 0)
                {
                    if (notFirst)
                        sb.Append("; ");
                    notFirst = true;
                    sb.Append(getter.Name.Substring(4));
                    sb.Append(":[");
                    bool notFirst2 = false;
                    foreach (var o in list)
                    {
                        if (notFirst2)
                            sb.Append("; ");
                        notFirst2 = true;
                        sb.Append(o);
                    }
                    sb.Append("]");
                }

                #endregion
                #region IDictionary 

                if (value is IDictionary && ((IDictionary) value).Count > 0)
                {
                    if (notFirst)
                        sb.Append("; ");
                    notFirst = true;
                    sb.Append(getter.Name.Substring(4));
                    sb.Append(":[");
                    bool notFirst2 = false;
                    foreach (var key in ((IDictionary) value).Keys)
                    {
                        object o = ((IDictionary) value)[key];
                        if (notFirst2)
                            sb.Append("; ");
                        notFirst2 = true;
                        sb.Append(string.Concat(key, ":", o));
                    }
                    sb.Append("]");
                }

                #endregion
            }

            sb.Append("}");
            return sb.ToString();
        }

        #endregion

        #region SetAttributes(object evt, IDictionary attributes) 

        internal static void SetAttributes(IParseSupport o, Dictionary<string, string> attributes, ILogger? logger = default)
        {
            Type dataType;
            object val;

            // Preparse attributes
            attributes = o.ParseSpecial(attributes);

            var underlayingEvent = o.GetSetter();
            var underlayingType = underlayingEvent.GetType();
            IDictionary setters = GetSetters(underlayingType);
            MethodInfo setter;
            foreach (var name in attributes.Keys)
            {
                if (name == "event")
                    continue;

                if (name == "source")
                    setter = (MethodInfo) setters["src"];
                else
                    setter = (MethodInfo) setters[stripIllegalCharacters(name)];

                if (setter == null)
                {
                    o.Parse(name, attributes[name]);
                }
                else
                {                    
                    dataType = (setter.GetParameters()[0]).ParameterType;
#if LOGGER
                    if(!FromStringBuilderHelper.TryConvert(dataType, attributes[name], name, underlayingType, logger, out val))                    
                        continue;
#else
					val = FromStringBuilderHelper.Convert(dataType, attributes[name], name, underlayingType, logger);
#endif                         
                    try
                    {
                        setter.Invoke(underlayingEvent, new[] {val});
                    }
                    catch (Exception ex)
                    {
#if LOGGER
                        logger?.LogError(ex, "Unable to set property '" + name + "' on " + underlayingType);
#else
						throw new ManagerException("Unable to set property '" + name + "' on " + o.GetType(), ex);
#endif
                    }
                }
            }
        }

        #endregion

        #region AddKeyValue(IDictionary list, string line) 

        internal static void AddKeyValue(IDictionary list, string line)
        {
            int delimiterIndex = line.IndexOf(":");
            if (delimiterIndex > 0 && line.Length > delimiterIndex + 1)
            {
                string name = line.Substring(0, delimiterIndex).ToLower(CultureInfo).Trim();
                string val = line.Substring(delimiterIndex + 1).Trim();
                if (list.Contains(name))
                    list[name] += Environment.NewLine + val;
                else if (val == "<null>")
                    list[name] = null;
                else
                    list[name] = val;
            }
        }

        #endregion

        #region stripIllegalCharacters(string s) 

        /// <summary>
        ///     Strips all illegal charaters from the given lower case string.
        /// </summary>
        /// <param name="s">the original string</param>
        /// <returns>the string with all illegal characters stripped</returns>
        private static string stripIllegalCharacters(string s)
        {
            char c;
            bool needsStrip = false;

            if (string.IsNullOrEmpty(s))
                return null;

            for (int i = 0; i < s.Length; i++)
            {
                c = s[i];
                if (c >= '0' && c <= '9')
                    continue;
                if (c >= 'a' && c <= 'z')
                    continue;
                if (c >= 'A' && c <= 'Z')
                    continue;
                needsStrip = true;
                break;
            }

            if (!needsStrip)
                return s;

            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                c = s[i];
                if (c >= '0' && c <= '9')
                    sb.Append(c);
                else if (c >= 'a' && c <= 'z')
                    sb.Append(c);
                else if (c >= 'A' && c <= 'Z')
                    sb.Append(c);
            }

            return sb.ToString();
        }

        #endregion

        #region BuildResponse(IDictionary attributes) 

        /// <summary>
        ///     Constructs an instance of ManagerResponse based on a map of attributes.
        /// </summary>
        /// <param name="attributes">the attributes and their values. The keys of this map must be all lower case.</param>
        /// <returns>the response with the given attributes.</returns>
        internal static ManagerResponse BuildResponse(Dictionary<string, string> attributes)
        {
            ManagerResponse response;

            string responseType = attributes["response"].ToLower(CultureInfo);

            // Determine type
            if (responseType == "error")
                response = new ManagerError();
            else if (attributes.ContainsKey("challenge"))
                response = new ChallengeResponse();
            else if (attributes.ContainsKey("mailbox") && attributes.ContainsKey("waiting"))
                response = new MailboxStatusResponse();
            else if (attributes.ContainsKey("mailbox") && attributes.ContainsKey("newmessages") &&
                     attributes.ContainsKey("oldmessages"))
                response = new MailboxCountResponse();
            else if (attributes.ContainsKey("exten") && attributes.ContainsKey("context") &&
                     attributes.ContainsKey("hint") && attributes.ContainsKey("status"))
                response = new ExtensionStateResponse();
            else
                response = new ManagerResponse();

            SetAttributes(response, attributes);
            return response;
        }

        #endregion        

        #region RegisterBuiltinEventClasses(Hashtable list) 

        /// <summary>
        ///     Register buildin Event classes or any executing assembly
        /// </summary>
        /// <param name="list"></param>
        internal static void RegisterBuiltinEventClasses(Dictionary<int, ConstructorInfo> list)
        {
            foreach (var type in GetDiscoveredTypes())
                RegisterEventClass(list, type);
        }

        internal static object _lockDiscovered = new object();
        internal static IEnumerable<Type> DiscoveredTypes;

        internal static bool AssemblyMatch(Assembly assembly)
        {
            return 
                !assembly.IsDynamic && ( 
                assembly.FullName.StartsWith(nameof(Sufficit), true, CultureInfo.InvariantCulture) ||
                assembly.FullName.StartsWith(nameof(AsterNET), true, CultureInfo.InvariantCulture)
                );
        }

        internal static IEnumerable<Type> GetDiscoveredTypes()
        {
            // Thread safe
            lock (_lockDiscovered)
            {
                if (DiscoveredTypes == null)
                {
                    Type manager = typeof(IManagerEvent);
                    var discovered = new List<Type>();

                    var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                    foreach (Assembly assembly in assemblies)
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
                            } else _logger.LogError(ex, "error getting types on assembly: {assembly}", assembly);
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

        #endregion

        #region RegisterEventClass(Dictionary<string, ConstructorInfo> list, Type clazz)

        internal static void RegisterEventClass(Dictionary<int, ConstructorInfo> list, Type clazz)
        {
            // Ignore all abstract classes
            // Class not derived from IManagerEvent
            if (clazz.IsAbstract || !typeof (IManagerEvent).IsAssignableFrom(clazz))
                return;

            string eventType = clazz.Name.ToLower(CultureInfo);

            // Remove "event" at the end (if presents)
            if (eventType.EndsWith("event"))
                eventType = eventType.Substring(0, eventType.Length - 5);

            // If assignable from UserEvent and no "userevent" at the start - add "userevent" to beginning
            if (typeof (UserEvent).IsAssignableFrom(clazz) && !eventType.StartsWith("user"))
                eventType = "user" + eventType;

            int hash = eventType.GetHashCode();
            if (list.ContainsKey(hash))
                return;

            ConstructorInfo constructor = null;
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
                throw new ArgumentException("RegisterEventClass : " + clazz + " has no usable constructor.", ex);
            }

            if (constructor != null && constructor.IsPublic)
                list.Add(hash, constructor);
            else
                throw new ArgumentException("RegisterEventClass : " + clazz + " has no public default constructor");
        }

        #endregion

        #region RegisterEventHandler(Dictionary<int, int> list, int index, Type eventType) 

        internal static void RegisterEventHandler(Dictionary<int, Func<IManagerEvent, bool>> list, Type eventType, Func<IManagerEvent, bool> action)
        {
            var eventTypeName = eventType.Name;
            int eventHash = eventTypeName.GetHashCode();
            if (list.ContainsKey(eventHash))
                throw new ArgumentException("Event class already registered : " + eventTypeName);
            list.Add(eventHash, action);
        }

        #endregion
    }
}
