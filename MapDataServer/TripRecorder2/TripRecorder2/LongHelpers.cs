using System;
using System.Collections.Generic;
using System.Text;

namespace TripRecorder2
{
    public static class LongHelpers
    {
        public static (int, int) ToInts(long a)
        {
            int a1 = unchecked((int)(a & uint.MaxValue));
            int a2 = unchecked((int)(a >> 32));
            return (a1, a2);
        }

        public static long ToLong(int a1, int a2)
        {
            long b = a2;
            b = unchecked(b << 32);
            b = unchecked(b | (uint)a1);
            return b;
        }
    }
}
