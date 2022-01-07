using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace EditTK.Graphics
{
    //TODO

    static class GlslValueTypes
    {
        public enum Kind
        {
            SCALAR,
            VECTOR,
            MATRIX
        }

        public struct GlslType
        {
            public GlslType(Type type, Kind kind, int amount)
            {
                Type = type;
                Kind = kind;
                Amount = amount;
            }

            public Type Type  { get; private set; }
            public Kind Kind  { get; private set; }
            public int Amount { get; private set; }
        }

        private static readonly Dictionary<Type, Func<IntPtr, object, int>> s_writerFunctions = new();

        public static IReadOnlyDictionary<string, Type> ScalarTypesByName => s_typesByName;
        public static IReadOnlyDictionary<string, Type> ScalarTypesByVecPrefix => s_typesByVecPrefix;

        private static readonly Dictionary<string, Type> s_typesByName = new Dictionary<string, Type>();
        private static readonly Dictionary<string, Type> s_typesByVecPrefix = new Dictionary<string, Type>();

        private unsafe static void RegisterType<T>(string name, string vecPrefix) where T : unmanaged
        {
            Type type = typeof(T);

            s_typesByName[name]           = type;
            s_typesByVecPrefix[vecPrefix] = type;

            s_writerFunctions[type] = (p, o) =>
            {
                Unsafe.Copy(p.ToPointer(), ref Unsafe.Unbox<T>(o));
                return 0;
            };
        }

        static GlslValueTypes()
        {
            RegisterType<float> ("float", string.Empty);
            RegisterType<double>("double", "d");
            RegisterType<int>   ("int",    "i");
            RegisterType<uint>  ("uint",   "u");
            RegisterType<bool>  ("bool",   "b");
        }
    }
}
