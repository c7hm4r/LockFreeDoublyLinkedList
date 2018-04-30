#region license
// Copyright 2016 Christoph Müller
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Test
{
	internal static class LinqHelper
    {
	    public static IEqualityComparer<T> ToEqualityComparer<T>
            (Func<T, T, bool> comparator, Func<T, int> getHashCode)
        {
            return new equalityComparer<T>(comparator, getHashCode);
        }

	    public static IEnumerable<T> Repeat<T>(int count, Func<T> generator)
        {
            for (int i = 0; i < count; i++)
                yield return generator();
        }

	    public static IEnumerable<T> Repeat<T>(Func<T> generator)
        {
            while (true)
                yield return generator();
        }

	    #region private
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

	        #region private
	        private readonly Func<T, T, bool> comparator;
	        private readonly Func<T, int> getHashCode;
	        #endregion
        }
	    #endregion
    }
}
