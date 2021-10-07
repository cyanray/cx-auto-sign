using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace cx_auto_sign
{
    public class CountCache
    {
        public int Count = 0;
        public readonly SemaphoreSlim Lock = new(1, 1);
        public override string ToString()
        {
            return Count.ToString();
        }
    }
}