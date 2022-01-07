using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace EditTK.Util
{
    public static class DictionaryExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TValue GetOrCreate<TKey, TValue>(this Dictionary<TKey, TValue> self, TKey key) 
            where TValue : class, new()
            where TKey : notnull
        {
            if (self.TryGetValue(key, out TValue? value))
            {
                return value;
            }
            else
            {
                TValue newVal = new();

                self[key] = newVal;

                return newVal;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TValue GetOrCreate<TKey, TValue>(this Dictionary<TKey, TValue> self, TKey key, Func<TValue> creator)
            where TValue : class
            where TKey : notnull
        {
            if (self.TryGetValue(key, out TValue? value))
            {
                return value;
            }
            else
            {
                TValue newVal = creator();

                self[key] = newVal;

                return newVal;
            }
        }
    }
}
