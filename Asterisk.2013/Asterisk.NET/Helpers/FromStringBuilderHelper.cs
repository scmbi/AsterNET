using AsterNET.Manager;
using Microsoft.Extensions.Logging;
using Sufficit.Asterisk;
using Sufficit.Asterisk.Manager;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace AsterNET.Helpers
{
    public static class FromStringBuilderHelper
    {
        public static bool TryConvert(Type type, string? value, string title, Type sourceType, ILogger? logger, out object? Result)
        {
            try
            {
                Result = Convert(type, value, title, sourceType, logger);
                return true;
            }
            catch
            {
                Result = null;
                return false;
            }
        }

        public static bool IsTypeOrNullableOf<T>(Type match)
        {
            var type = typeof(T);
            if (match == type || Nullable.GetUnderlyingType(match) == type) return true;
            else return false;
        }

        public static bool IsEnumOrNullableOf(Type match)
        {
            if (match.IsEnum)
            {
                return true;
            } else {
                var type = Nullable.GetUnderlyingType(match);
                if(type != null && type.IsEnum) return true;
            }    

            return false; 
        }

        public static object? Convert(Type type, string? value, string title, Type eventType, ILogger? logger)
        {
            if (IsTypeOrNullableOf<bool>(type))
                return IsTrue(value);
            else if (IsTypeOrNullableOf<string>(type))
                return ParseString(value);
            else if (IsTypeOrNullableOf<uint>(type))
            {
                uint v = 0;
                uint.TryParse(value, out v);
                return v;
            }
            else if (IsTypeOrNullableOf<Int32>(type))
            {
                Int32 v = 0;
                Int32.TryParse(value, out v);
                return v;
            }
            else if (IsTypeOrNullableOf<Int64>(type))
            {
                Int64 v = 0;
                Int64.TryParse(value, out v);
                return v;
            }
            else if (IsTypeOrNullableOf<double>(type))
            {
                Double v = 0.0;
                Double.TryParse(value, NumberStyles.AllowDecimalPoint, Common.CultureInfoEn, out v);
                return v;
            }
            else if (IsTypeOrNullableOf<decimal>(type))
            {
                Decimal v = 0;
                Decimal.TryParse(value, NumberStyles.AllowDecimalPoint, Common.CultureInfoEn, out v);
                return v;
            } 
            else if (IsTypeOrNullableOf<DateTime>(type))
            {
                if (type is Nullable && string.IsNullOrWhiteSpace(value))
                    return null;

                DateTime v = DateTime.MinValue;
                DateTime.TryParse(value, out v);
                return v;
            }
            else if (IsEnumOrNullableOf(type))
            {
                try
                {
                    return value.EnumParse(type);
                }
                catch (Exception ex)
                {
                    var errorString = "Unable to convert value '" + value + "' of property '" + title + "' on " + eventType + " to required enum type " + type;
                    logger?.LogError(ex, errorString);
					throw new ManagerException(errorString, ex); 
                }
            }
            else
            {
                try
                {
                    ConstructorInfo constructor = type.GetConstructor(new[] { typeof(string) });
                    return constructor.Invoke(new object[] { value });
                }
                catch (Exception ex)
                {
                    var errorString = "Unable to convert value '" + value + "' of property '" + title + "' on " + eventType + " to required type " + type;
                    logger?.LogError(ex, errorString);
    				throw new ManagerException(errorString, ex);
                }
            }

            return null;
        }

        #region ParseString(string val) 

        internal static object ParseString(string val)
        {
            if (val == "none")
                return string.Empty;
            return val;
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

        private static CultureInfo defaultCulture;
    }
}
