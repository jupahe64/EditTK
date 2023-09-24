using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace EditTK.Utils
{
    internal static class StructUtil
    {
        private static MethodInfo s_unsafe_sizeof = typeof(Unsafe).GetMethod(nameof(Unsafe.SizeOf))!;

        private static Dictionary<Type, int> s_structSizeCache = new();

        /// <summary>
        /// A non generic version of <see cref="Unsafe.SizeOf{T}"/> that caches results
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static int SizeOf(Type type)
        {
            return s_structSizeCache.GetOrCreate(type, () =>
            {
                return (int)s_unsafe_sizeof.MakeGenericMethod(new Type[] {type}).Invoke(null, null)!;
            });
        }
    }
}
