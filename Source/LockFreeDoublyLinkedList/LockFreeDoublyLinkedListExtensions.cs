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

namespace LockFreeDoublyLinkedList
{
	/// <summary>
	/// Extension methods for the class
	/// <see cref="ILockFreeDoublyLinkedList{T}"/>
	/// </summary>
	public static class LockFreeDoublyLinkedListExtensions
	{
		/// <summary>
		/// Exchanges the value of a LockFreeLinkedList node
		/// with a new Value generated from the old value
		/// if and only if <paramref name="newValue" />
		/// for the old value is not null.
		/// If not, nothing is performed and
		/// false within the result Tuple is returned.
		/// </summary>
		/// <typeparam name="T">
		/// The type of the LockFreeLinkedList contents.
		/// </typeparam>
		/// <param name="node">The node whose value shall be replaced.</param>
		/// <param name="newValue">
		/// The function which creates a new value encapsulated
		/// in a Tuple&lt;T&gt; based on the prevalent value.
		/// If the value shall not be replaced, return null;
		/// </param>
		/// <returns>
		/// The prevalent value together with
		/// a success flag encapsulated in a Tuple&lt;T, bool&gt;.
		/// </returns>
		public static Tuple<T, bool> CompareExchangeValueIf<T>(
			this ILockFreeDoublyLinkedListNode<T> node,
			Func<T, Tuple<T>> newValue)
			where T : class
		{
			T old = node.Value;
			while (true)
			{
				Tuple<T> @new = newValue(old);
				if (@new == null)
					return new Tuple<T, bool>(old, false);
				T prevalent = node.CompareExchangeValue(@new.Item1, old);
				if (prevalent == old)
					return new Tuple<T, bool>(old, true);
				old = prevalent;
			}
		}
	}
}
