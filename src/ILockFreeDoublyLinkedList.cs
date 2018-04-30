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
#if SynchronizedLfdll
#if Verbose
#define SynchronizedLfdll_Verbose
#endif
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LockFreeDoublyLinkedLists
{
    /// <summary>
    /// A lock free doubly linked list for high concurrency.
    /// </summary>
    /// <typeparam name="T">The type of the values.</typeparam>
    public interface ILockFreeDoublyLinkedList<T> : IEnumerable<T>
        where T : class
    {
#if SynchronizedLfdll
        ThreadLocal<AutoResetEvent> NextStepWaitHandle { get; }
        AutoResetEvent StepCompletedWaitHandle { get; }
#if SynchronizedLfdll_Verbose
		void LogNode(ILockFreeDoublyLinkedListNode<T> inode);
#endif
#endif

        /// <summary>
        /// The dummy head node (leftmost).
        /// </summary>
        ILockFreeDoublyLinkedListNode<T> Head { get; }
        /// <summary>
        /// The dummy tail node (rightmost).
        /// </summary>
        ILockFreeDoublyLinkedListNode<T> Tail { get; }

        /// <summary>
        /// Removes the rightmost non-dummy node, if it exists.
        /// </summary>
        /// <returns>
        /// null, if the list is empty; else the removed node.
        /// </returns>
        ILockFreeDoublyLinkedListNode<T> PopRightNode();

        /// <summary>
        /// Inserts a new node at the head position.
        /// </summary>
        /// <param name="value">The initial value of the new node.</param>
        /// <returns>The new inserted node.</returns>
        ILockFreeDoublyLinkedListNode<T> PushLeft(T value);

        /// <summary>
        /// Inserts a new node at the tail position.
        /// </summary>
        /// <param name="value">The initial value of the new node.</param>
        /// <returns>The new inserted node.</returns>
        ILockFreeDoublyLinkedListNode<T> PushRight(T value);

#if PopRight
		/// Removes the rightmost non-dummy node if it exists
		/// and returns its value after the removal.
		/// </summary>
		/// <returns>
		/// null, if the list is empty; else a Tuple&lt;T&gt;,
		/// which contains the value of the removed node.
		/// </returns>
		[Obsolete("This method is not atomar. For clarity use PopRightNode instead.",
			false)]
		Tuple<T> PopRight();
#endif
#if PopLeft
		/// <summary>
		/// Removes the leftmost non-dummy node and returns its value.
		/// </summary>
		/// <returns>The value of the removed node.</returns>
		[Obsolete("This method is not supported.", false)]
		Tuple<T> PopLeft();
#endif
    }
}