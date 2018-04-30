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
using System.Threading;
using System.Threading.Tasks;

namespace LockFreeDoublyLinkedLists
{
	internal static class ThreadingAdditions
    {
		/// <summary>
        /// Replaces the value under location
        /// if a distinct condition is fulfilled for the prevalent value.
        /// </summary>
        /// <typeparam name="T">The type of the value to replace.</typeparam>
        /// <param name="location">
        /// The place, where the value shall be replaced.
        /// </param>
        /// <param name="value">
        /// The value, which is assigned, when the condition is fulfilled.
        /// </param>
        /// <param name="condition">
        /// The condition,
        /// which shall be fulfilled when the condition is being performed.
        /// </param>
        /// <param name="current">
        /// The value, which has been prevalent before the replacement.
        /// </param>
        /// <returns>
        /// Whether the replacement has occurred.
        /// </returns>
        public static bool ConditionalCompareExchange<T>(
            ref T location, T value, Func<T, bool> condition, out T current)
            where T : class
        {
            Thread.MemoryBarrier();
            current = location;
            while (true)
            {
                if (!condition(current))
                    return false;
                T prevalent = Interlocked.CompareExchange(
                    ref location, value, current);
                if (ReferenceEquals(prevalent, current))
                    return true;
                current = prevalent;
            }
        }

		/// <summary>
        /// Replaces the value under location
        /// if a distinct condition is fulfilled for the prevalent value.
        /// </summary>
        /// <typeparam name="T">The type of the value to replace.</typeparam>
        /// <param name="location">
        /// The place, where the value shall be replaced.
        /// </param>
        /// <param name="value">
        /// The value, which is assigned, when the condition is fulfilled.
        /// </param>
        /// <param name="condition">
        /// The condition,
        /// which shall be fulfilled when the condition is being performed.
        /// </param>
        /// <returns>
        /// Whether the replacement has occurred.
        /// </returns>
        public static bool ConditionalCompareExchange<T>(
            ref T location, T value, Func<T, bool> condition)
            where T : class
        {
            T current;
            return ConditionalCompareExchange(
                ref location, value, condition, out current);
        }
    }
}
