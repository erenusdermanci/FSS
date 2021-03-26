using System;
using System.Threading;

namespace Blocks
{
    public static class BlockIdGenerator
    {
        private static long _start = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;

        private static readonly ThreadLocal<long> Id =
            new ThreadLocal<long>(() => Interlocked.Increment(ref _start));

        public static long Next()
        {
            return Id.Value;
        }
    }
}