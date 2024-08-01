using Sufficit;
using Sufficit.Asterisk.Manager;
using System;
using System.Collections.Generic;
using System.Text;

namespace AsterNET.Helpers
{
    public static class HelperExtensions
    {
        public static object? EnumParse(this string? source, Type type)
        {
            if (string.IsNullOrWhiteSpace(source))
                return null;

            var dataType = type;
            if (!dataType.IsEnum)                
                dataType = Nullable.GetUnderlyingType(dataType);                

            if (dataType == typeof(Privilege))
            {
                var result = Privilege.none;
                foreach (var s in source!.Split(','))
                {
                    var normalized = s.Trim().ToLowerInvariant();
                    if (Enum.TryParse(normalized, out Privilege flag))
                    {
                        result |= flag;
                    }
                }
                return result;
            } 
            else
            {
                var result = EnumExtensions.GetValueFromDescription(dataType, source!, false);
                if (result != null) return result;

                result = Enum.Parse(dataType, source, true);
                return System.Convert.ChangeType(result, dataType);
            }                
            
        }

        [Obsolete("2024/05/27 not used anymore")]
        private static void SetFlag<T>(ref T value, T flag) where T : Enum
        {
            // 'long' can hold all possible values, except those which 'ulong' can hold.
            if (Enum.GetUnderlyingType(typeof(T)) == typeof(ulong))
            {
                ulong numericValue = System.Convert.ToUInt64(value);
                numericValue |= System.Convert.ToUInt64(flag);
                value = (T)Enum.ToObject(typeof(T), numericValue);
            }
            else
            {
                long numericValue = System.Convert.ToInt64(value);
                numericValue |= System.Convert.ToInt64(flag);
                value = (T)Enum.ToObject(typeof(T), numericValue);
            }
        }
    }
}
