using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EditTK.Utils
{
    public class Union<T1, T2>
    {
        private T1? _value1;
        private T2? _value2;

        public static implicit operator Union<T1, T2>(T1 value)
        {
            return new() { _value1 = value };
        }

        public static implicit operator Union<T1, T2>(T2 value)
        {
            return new() { _value2 = value };
        }

        public bool TryGetT1(out T1? value)
        {
            value = _value1;
            return _value1 is not null;
        }

        public bool TryGetT2(out T2? value)
        {
            value = _value2;
            return _value2 is not null;
        }
    }
}
