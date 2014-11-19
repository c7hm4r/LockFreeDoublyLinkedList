//#define SynchronizedLfdll


#if SynchronizedLfdll
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace LockFreeDoublyLinkedList
{
    /// <summary>
    /// A threadsafe Counter.
    /// </summary>
    public class Counter
    {
        /// <summary>
        /// The current counter value.
        /// </summary>
        public long Current
        {
            get { return Interlocked.Read(ref value); }
        }

        /// <summary>
        /// Inkrement the counter value.
        /// </summary>
        /// <returns>The new counter value.</returns>
        public long Count()
        {
            return Interlocked.Increment(ref value);
        }

        private long value = 0;
    }
}
#endif