using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    static class LinqHelper
    {
        public static IEqualityComparer<T> ToEqualityComparer<T>
            (Func<T, T, bool> comparator, Func<T, int> getHashCode)
        {
            return new equalityComparer<T>(comparator, getHashCode);
        }

        private class equalityComparer<T> : IEqualityComparer<T>
        {
            public bool Equals(T x, T y)
            {
                return comparator(x, y);
            }

            public int GetHashCode(T obj)
            {
                return getHashCode(obj);
            }

            public equalityComparer(Func<T, T, bool> comparator, Func<T, int> getHashCode)
            {
                this.comparator = comparator;
                this.getHashCode = getHashCode;
            }

            private Func<T, T, bool> comparator;
            private Func<T, int> getHashCode;
        }
    }
}
