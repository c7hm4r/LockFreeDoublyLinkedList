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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LockFreeDoublyLinkedLists
{
	/// <summary>
	/// Provides an implementation of a LockFree DoublyLinkedList.
	/// </summary>
	public static class LockFreeDoublyLinkedList
	{
		/// <summary>
		/// Creates a new <see cref="ILockFreeDoublyLinkedList{T}"/>.
		/// </summary>
		/// <typeparam name="T">
		/// The type of the elements in the created
		/// <see cref="ILockFreeDoublyLinkedList{T}"/>.
		/// </typeparam>
		/// <returns>The newly created instance.</returns>
		public static ILockFreeDoublyLinkedList<T> Create<T>()
			where T : class
		{
			return new lockFreeDoublyLinkedList<T>();
		}

		/// <summary>
		/// Creates a new <see cref="ILockFreeDoublyLinkedList{T}"/>.
		/// </summary>
		/// <typeparam name="T">
		/// The type of the elements in the created
		/// <see cref="ILockFreeDoublyLinkedList{T}"/>.
		/// </typeparam>
		/// <param name="initial">
		/// The initial elements in the created list.
		/// </param>
		/// <returns>The newly created instance.</returns>
		public static ILockFreeDoublyLinkedList<T> Create<T>(
				IEnumerable<T> initial)
			where T : class
		{
			return new lockFreeDoublyLinkedList<T>(initial);
		}

		#region private
		private static bool compareExchangeNodeLink<T>(ref nodeLink<T> location,
			nodeLink<T> value, nodeLink<T> comparandByValue) where T : class
		{
			return ThreadingAdditions
				.ConditionalCompareExchange(ref location,
					value, original => original.Equals(comparandByValue));
		}

		private static bool compareExchangeNodeLinkInPair<T>(
			ref valueNodeLinkPair<T> location, nodeLink<T> newLink,
			nodeLink<T> comparadByValue) where T : class
		{
			Thread.MemoryBarrier();
			T currentValue = location.Value;
			return ThreadingAdditions
				.ConditionalCompareExchange(ref location,
					new valueNodeLinkPair<T>(currentValue, newLink),
					original =>
						original.Link.Equals(comparadByValue)
							&& ReferenceEquals(original.Value, currentValue));
		}

		private class lockFreeDoublyLinkedList<T> :
			ILockFreeDoublyLinkedList<T> where T : class
		{
			public readonly node<T> HeadNode;
			public readonly node<T> TailNode;

			public IEnumerator<T> GetEnumerator()
			{
				ILockFreeDoublyLinkedListNode<T> current = Head;
				while (true)
				{
					current = current.Next;
					if (current == Tail)
						yield break;
					yield return current.Value;
				}
			}

			public ILockFreeDoublyLinkedListNode<T> Head => HeadNode;

			public ILockFreeDoublyLinkedListNode<T> Tail => TailNode;

			public ILockFreeDoublyLinkedListNode<T> PushLeft(T value)
			{
				SpinWait spin = new SpinWait();
				node<T> node = new node<T>(this);
				node<T> prev = HeadNode;

				Thread.MemoryBarrier();
				EnterTestSynchronizedBlock();
				node<T> next = prev.Next_.Link.P;
#if SynchronizedLfdll_Verbose
				Console.WriteLine("PushLeft {0} Step 0", value);
				Console.WriteLine("next =");
				LogNode(next);
#endif
				LeaveTestSynchronizedBlock();

				while (true)
				{
					/* node has not been made public yet,
					 * so no synchronization constructs are necessary. */
					node.Prev_ = new nodeLink<T>(prev, false);
					node.Next_ = new valueNodeLinkPair<T>(value, new nodeLink<T>(next, false));

					EnterTestSynchronizedBlock();
					bool b = compareExchangeNodeLinkInPair(ref prev.Next_,
						new nodeLink<T>(node, false),
						new nodeLink<T>(next, false));
#if SynchronizedLfdll_Verbose
					Console.WriteLine("PushLeft {0} Step 1", value);
					Console.WriteLine(
						"compareExchangeNlIp(ref prev.Next_, {0}, {1}) = {2}",
						NodeLinkDescription(new nodeLink<T>(node, false)),
						NodeLinkDescription(new nodeLink<T>(next, false)),
						b);
					Console.WriteLine("prev = ");
					LogNode(prev);
					LogState();
#endif
					LeaveTestSynchronizedBlock();

					if (b)
						break;

					/* Not necessary because of preceding compareExchange. */
					// Thread.MemoryBarrier();

					EnterTestSynchronizedBlock();
					next = prev.Next_.Link.P;
#if SynchronizedLfdll_Verbose
					Console.WriteLine("PushLeft {0} Step 2", value);
					Console.WriteLine("next =");
					LogNode(next);
#endif
					LeaveTestSynchronizedBlock();

					spin.SpinOnce();
				}
				pushEnd(node, next, spin);
				Thread.MemoryBarrier();
				return node;
			}

			public ILockFreeDoublyLinkedListNode<T> PushRight(T value)
			{
				SpinWait spin = new SpinWait();
				node<T> node = new node<T>(this);
				node<T> next = TailNode;

				Thread.MemoryBarrier();
				EnterTestSynchronizedBlock();
				node<T> prev = next.Prev_.P;
#if SynchronizedLfdll_Verbose
				Console.WriteLine("PushRight {0} Step 0", value);
				Console.WriteLine("prev = ");
				LogNode(prev);
#endif
				LeaveTestSynchronizedBlock();

				while (true)
				{
					/* node has not been made public yet,
					 * so no threading constructs are necessary. */
					node.Prev_ = new nodeLink<T>(prev, false);
					node.Next_ = new valueNodeLinkPair<T>(value, new nodeLink<T>(next, false));

					EnterTestSynchronizedBlock();
					bool b = compareExchangeNodeLinkInPair(ref prev.Next_,
						new nodeLink<T>(node, false),
						new nodeLink<T>(next, false));
#if SynchronizedLfdll_Verbose
					Console.WriteLine("PushRight {0} Step 1", value);
					Console.WriteLine(
						"compareExchangeNlIp(ref prev.Next_, {0}, {1}) = {2}",
						NodeLinkDescription(new nodeLink<T>(node, false)),
						NodeLinkDescription(new nodeLink<T>(next, false)),
						b);
					Console.WriteLine("prev = ");
					LogNode(prev);
					LogState();
#endif
					LeaveTestSynchronizedBlock();

					if (b)
						break;

					prev = CorrectPrev(prev, next);
					spin.SpinOnce();
				}
				pushEnd(node, next, spin);
				Thread.MemoryBarrier();
				return node;
			}

#if PopLeft
			[Obsolete("This method is not supported.", false)]
			public Tuple<T> PopLeft()
			{
				SpinWait spin = new SpinWait();
				node<T> prev = HeadNode;
				while (true)
				{
					Thread.MemoryBarrier();
					EnterTestSynchronizedBlock();
					node<T> node = prev.Next_.Link.P;
#if SynchronizedLfdll_Verbose
					Console.WriteLine("PopLeft Step 0");
					Console.WriteLine("node = ");
					LogNode(node);
#endif
					LeaveTestSynchronizedBlock();

					if (node == TailNode)
					{
						Thread.MemoryBarrier();
						return null;
					}

					Thread.MemoryBarrier();
					EnterTestSynchronizedBlock();
					nodeLink<T> next = node.Next_.Link;
#if SynchronizedLfdll_Verbose
					Console.WriteLine("PopLeft Step 1");
					Console.WriteLine("next = {0}", NodeLinkDescription(next));
#endif
					LeaveTestSynchronizedBlock();

					if (next.D)
					{
						SetMark(ref node.Prev_);

						EnterTestSynchronizedBlock();
						// ReSharper disable once UnusedVariable
						bool b1 = compareExchangeNodeLinkInPair(ref prev.Next_,
							new nodeLink<T>(next.P, false),
							(nodeLink<T>)node);
#if SynchronizedLfdll_Verbose
						Console.WriteLine("PopLeft Step 2");
						Console.WriteLine("compareExchangeNlIp(ref prev.Next_, {0}, {1}) = {2}",
							NodeLinkDescription(new nodeLink<T>(next.P, false)),
							NodeLinkDescription((nodeLink<T>)node),
							b1);
						Console.WriteLine("prev = ");
						LogNode(prev);
						LogState();
#endif
						LeaveTestSynchronizedBlock();

						continue;
					}

					EnterTestSynchronizedBlock();
					bool b = compareExchangeNodeLinkInPair(ref node.Next_,
						new nodeLink<T>(next.P, true), next);
#if SynchronizedLfdll_Verbose
					Console.WriteLine("PopLeft Step 3");
					Console.WriteLine("compareExchangeNlIp(ref node.Next_, {0}, {1}) = {2}",
						NodeLinkDescription(new nodeLink<T>(next.P, true)),
						NodeLinkDescription(next),
						b);
					Console.WriteLine("node = ");
					LogNode(node);
					LogState();
#endif
					LeaveTestSynchronizedBlock();

					if (b)
					{
						CorrectPrev(prev, next.P);

						Thread.MemoryBarrier();
						EnterTestSynchronizedBlock();
						T value = node.Next_.Value;
#if SynchronizedLfdll_Verbose
						Console.WriteLine("PopLeft Step 4");
						Console.WriteLine("value = {0}", value);
#endif
						LeaveTestSynchronizedBlock();

						Thread.MemoryBarrier();
						return new Tuple<T>(value);
					}

					spin.SpinOnce();
				}
			}
#endif

#if PopRight
			[Obsolete("This method is not atomar. For clarity use PopRightNode instead.", false)]
			public Tuple<T> PopRight()
			{
				ILockFreeDoublyLinkedListNode<T> node = PopRightNode();
				if (node == null)
					return null;
				return Tuple.Create(node.Value);
			}
#endif

			public ILockFreeDoublyLinkedListNode<T> PopRightNode()
			{
				SpinWait spin = new SpinWait();
				node<T> next = TailNode;

				Thread.MemoryBarrier();
				EnterTestSynchronizedBlock();
				node<T> node = next.Prev_.P;
#if SynchronizedLfdll_Verbose
				Console.WriteLine("PopRightNode Step 0");
				Console.WriteLine("node  =");
				LogNode(node);
#endif
				LeaveTestSynchronizedBlock();

				while (true)
				{

					Thread.MemoryBarrier();
					EnterTestSynchronizedBlock();
					bool b = !node.Next_.Link.Equals(new nodeLink<T>(next, false));
#if SynchronizedLfdll_Verbose
					Console.WriteLine("PopRightNode Step 1");
					Console.WriteLine("b = {0}", b);
#endif
					LeaveTestSynchronizedBlock();

					if (b)
					{
						node = CorrectPrev(node, next);
						continue;
					}

					if (node == HeadNode)
					{
						Thread.MemoryBarrier();
						return null;
					}

					EnterTestSynchronizedBlock();
					bool b1 = compareExchangeNodeLinkInPair(ref node.Next_,
						new nodeLink<T>(next, true),
						new nodeLink<T>(next, false));
#if SynchronizedLfdll_Verbose
					Console.WriteLine("PopRightNode Step 2");
					Console.WriteLine(
						"compareExchangeNlIp(ref node.Next_, {0}, {1}) = {2}",
						NodeLinkDescription(new nodeLink<T>(next, true)),
						NodeLinkDescription(new nodeLink<T>(next, false)),
						b1);
					Console.WriteLine("node = ");
					LogNode(node);
					LogState();
#endif
					LeaveTestSynchronizedBlock();

					if (b1)
					{
						/* Not necessary because of preceding compareExchange. */
						// Thread.MemoryBarrier();

						EnterTestSynchronizedBlock();
						node<T> prev = node.Prev_.P;
#if SynchronizedLfdll_Verbose
						Console.WriteLine("PopRightNode Step 3");
						Console.WriteLine("prev =");
						LogNode(prev);
#endif
						LeaveTestSynchronizedBlock();

						CorrectPrev(prev, next);

						Thread.MemoryBarrier();
						return node;
					}

					spin.SpinOnce();
				}
			}

			public void SetMark(ref nodeLink<T> link)
			{
				Thread.MemoryBarrier();
				EnterTestSynchronizedBlock();
				nodeLink<T> node = link;
#if SynchronizedLfdll_Verbose
				Console.WriteLine("SetMark Step 0");
				Console.WriteLine("node = {0}", NodeLinkDescription(node));
#endif
				LeaveTestSynchronizedBlock();

				while (true)
				{
					if (node.D)
						break;

					EnterTestSynchronizedBlock();
					nodeLink<T> prevalent = Interlocked.CompareExchange(
						ref link, new nodeLink<T>(node.P, true), node);
#if SynchronizedLfdll_Verbose
					Console.WriteLine("SetMark Step 1");
					Console.WriteLine("compareExchange(link, {0}, {1}) = {2}",
						NodeLinkDescription(new nodeLink<T>(node.P, true)),
						NodeLinkDescription(node),
						NodeLinkDescription(prevalent));
					Console.WriteLine("link = {0}", NodeLinkDescription(link));
#endif
					LeaveTestSynchronizedBlock();

					// ReSharper disable once PossibleUnintendedReferenceComparison
					if (prevalent == node)
						break;
					node = prevalent;
				}
			}

			public node<T> CorrectPrev(node<T> prev, node<T> node)
			{
				// ReSharper disable once IntroduceOptionalParameters.Local
				return CorrectPrev(prev, node, new SpinWait());
			}

			public node<T> CorrectPrev(node<T> prev, node<T> node, SpinWait spin)
			{
				node<T> lastLink = null;
				while (true)
				{

					Thread.MemoryBarrier();
					EnterTestSynchronizedBlock();
					nodeLink<T> link1 = node.Prev_;
#if SynchronizedLfdll_Verbose
					Console.WriteLine("CorrectPrev Step 0");
					Console.WriteLine("link1 = {0}",
						NodeLinkDescription(link1));
#endif
					LeaveTestSynchronizedBlock();

					if (link1.D)
						break;

					Thread.MemoryBarrier();
					EnterTestSynchronizedBlock();
					nodeLink<T> prev2 = prev.Next_.Link;
#if SynchronizedLfdll_Verbose
					Console.WriteLine("CorrectPrev Step 1");
					Console.WriteLine("prev2 = {0}",
						NodeLinkDescription(prev2));
#endif
					LeaveTestSynchronizedBlock();

					if (prev2.D)
					{
						if (lastLink != null)
						{
							SetMark(ref prev.Prev_);

							EnterTestSynchronizedBlock();

							// ReSharper disable once UnusedVariable
							bool b1 = compareExchangeNodeLinkInPair(ref lastLink.Next_,
								(nodeLink<T>)prev2.P, (nodeLink<T>)prev);
#if SynchronizedLfdll_Verbose
							Console.WriteLine("CorrectPrev Step 2");
							Console.WriteLine(
								"compareExchangeNlIp(lastLink.Next_, {0}, {1}) = {2}",
								NodeLinkDescription((nodeLink<T>)prev2.P),
								NodeLinkDescription((nodeLink<T>)prev),
								b1);
							Console.WriteLine("lastLink = ");
							LogNode(lastLink);
							LogState();
#endif
							LeaveTestSynchronizedBlock();

							prev = lastLink;
							lastLink = null;
							continue;
						}

						Thread.MemoryBarrier();
						EnterTestSynchronizedBlock();
						prev2 = prev.Prev_;
#if SynchronizedLfdll_Verbose
						Console.WriteLine("CorrectPrev Step 3");
						Console.WriteLine("prev2 = {0}",
							NodeLinkDescription(prev2));
#endif
						LeaveTestSynchronizedBlock();

						/* A conversion would probably sometimes
                     * lead to errors. */
						prev = prev2.P;
						continue;
					}
					/* The paper simply states „Prev_ != node“,
                 * but the types are different.
                 * It is probably assumed
                 * that the comaparison is performed as follows:
                 * !(prev2.P == node && !prev2.D).
                 * Since prev2.D is always false here,
                 * simplification is possible. **/
					if (prev2.P != node)
					{
						lastLink = prev;
						prev = prev2.P;
						continue;
					}

					EnterTestSynchronizedBlock();
					bool b = compareExchangeNodeLink(ref node.Prev_,
						new nodeLink<T>(prev, false), link1);
#if SynchronizedLfdll_Verbose
					Console.WriteLine("CorrectPrev Step 4");
					Console.WriteLine("compareExchangeNl(node.Prev_, {0}, {1}) = {2}",
						NodeLinkDescription(new nodeLink<T>(prev, false)),
						NodeLinkDescription(link1),
						b);
					Console.WriteLine("node = ");
					LogNode(node);
					LogState();
#endif
					LeaveTestSynchronizedBlock();

					if (b)
					{
						/* Not necessary because of preceding compareExchange. */
						// Thread.MemoryBarrier();

						EnterTestSynchronizedBlock();
						bool b1 = prev.Prev_.D;
#if SynchronizedLfdll_Verbose
						Console.WriteLine("CorrectPrev Step 5");
						Console.WriteLine("b1 = {0}", b1);
#endif
						LeaveTestSynchronizedBlock();

						if (b1)
							continue;
						break;
					}

					spin.SpinOnce();
				}
				return prev;
			}

			public void EnterTestSynchronizedBlock()
			{
#if SynchronizedLfdll
				if (NextStepWaitHandle.IsValueCreated)
				{
					NextStepWaitHandle.Value.WaitOne();

#if SynchronizedLfdll_Verbose
					Console.WriteLine("({0}) Step {1}",
						Thread.CurrentThread.Name, StepCounter.Count());
#endif
				}
#endif
			}

			public void LeaveTestSynchronizedBlock()
			{
#if SynchronizedLfdll
				if (NextStepWaitHandle.IsValueCreated)
					StepCompletedWaitHandle.Set();
#endif
			}

			/// <summary>
			/// Creates a new empty lockFreeDoublyLinkedList.
			/// </summary>
			public lockFreeDoublyLinkedList()
			{
				HeadNode = new node<T>(this);
				TailNode = new node<T>(this);

				HeadNode.Prev_ = new nodeLink<T>(null, false);
				HeadNode.Next_ = new valueNodeLinkPair<T>(null, new nodeLink<T>(TailNode, false));
				TailNode.Prev_ = new nodeLink<T>(HeadNode, false);
				TailNode.Next_ = new valueNodeLinkPair<T>(null, new nodeLink<T>(null, false));
				Thread.MemoryBarrier();
			}

			/// <summary>
			/// Creates a new lockFreeDoublyLinkedList
			/// which contains the contents of the enumeration initial.
			/// </summary>
			/// <param name="initial">The enumeration to copy.</param>
			public lockFreeDoublyLinkedList(IEnumerable<T> initial)
				: this()
			{
				if (initial == null)
					throw new ArgumentNullException(nameof(initial));
				foreach (T value in initial)
					PushRight(value);
			}

			#region private
			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}

			private void pushEnd(node<T> node, node<T> next, SpinWait spin)
			{
				while (true)
				{
					Thread.MemoryBarrier();
					EnterTestSynchronizedBlock();
					nodeLink<T> link1 = next.Prev_;
#if SynchronizedLfdll_Verbose
					Console.WriteLine("pushEnd Step 0");
					Console.WriteLine("link1 = {0}",
						NodeLinkDescription(link1));
#endif
					LeaveTestSynchronizedBlock();

					bool b = link1.D;
					if (!b)
					{
						Thread.MemoryBarrier();
						EnterTestSynchronizedBlock();
						b |= !node.Next_.Link.Equals(new nodeLink<T>(next, false));
#if SynchronizedLfdll_Verbose
						Console.WriteLine("pushEnd Step 1");
						Console.WriteLine(
							"!node.Next_.Link.Equals(new nodeLink<T>(next, false) = {0}",
							!node.Next_.Link.Equals(new nodeLink<T>(next, false)));
#endif
						LeaveTestSynchronizedBlock();
					}
					if (b)
						break;

					EnterTestSynchronizedBlock();
					bool b1 = compareExchangeNodeLink(ref next.Prev_,
						new nodeLink<T>(node, false), link1);
#if SynchronizedLfdll_Verbose
					Console.WriteLine("pushEnd Step 2");
					Console.WriteLine("compareExchangeNl(next.Prev_, {0}, {1}) = {2}",
						NodeLinkDescription(new nodeLink<T>(node, false)),
						NodeLinkDescription(link1),
						b1);
					Console.WriteLine("next = ");
					LogNode(next);
					LogState();
#endif
					LeaveTestSynchronizedBlock();

					if (b1)
					{

						/* Not necessary because of preceding compareExchange. */
						// Thread.MemoryBarrier();

						EnterTestSynchronizedBlock();
						bool b2 = node.Prev_.D;
#if SynchronizedLfdll_Verbose
						Console.WriteLine("pushEnd Step 3");
						Console.WriteLine("b1 = {0}", b2);
#endif
						LeaveTestSynchronizedBlock();

						if (b2)
							CorrectPrev(node, next);
						break;
					}

					spin.SpinOnce();
				}
			}
			#endregion

#if SynchronizedLfdll
			public ThreadLocal<AutoResetEvent> NextStepWaitHandle { get; }
				= new ThreadLocal<AutoResetEvent>();

			public AutoResetEvent StepCompletedWaitHandle { get; }
				= new AutoResetEvent(false);

			public Counter StepCounter { get; } = new Counter();

#if SynchronizedLfdll_Verbose
			public void LogState()
			{
				node<T> current = HeadNode;
				while (current != null)
				{
					LogNode(current);
					current = current.Next_.Link.P;
				}
			}

			public void LogNode(ILockFreeDoublyLinkedListNode<T> inode)
			{
				node<T> node = (node<T>)inode;
				Console.WriteLine(nodeName(node));
				if (node != null)
				{
					Console.WriteLine("    .Prev_ = "
						+ NodeLinkDescription(node.Prev_));
					Console.WriteLine("    .Next_ = "
						+ ValueNodeLinkPairDescription(node.Next_));
				}
			}
#endif

#if SynchronizedLfdll_Verbose
			private string nodeName(node<T> node)
			{
				if (node == null)
					return "null";
				if (node == HeadNode)
					return "HeadNode";
				if (node == TailNode)
					return "TailNode";
				return "Node " + node.Value;
			}

			public string NodeLinkDescription(nodeLink<T> link)
			{
				return "(" + nodeName(link.P)
					+ ", " + link.D + ")";
			}

			public string ValueNodeLinkPairDescription
				(valueNodeLinkPair<T> pair)
			{
				return "(" + pair.Value + ", "
					+ NodeLinkDescription(pair.Link) + ")";
			}

			// ReSharper disable once UnusedMember.Local
			private void logNodeLink(nodeLink<T> link)
			{
				Console.WriteLine(NodeLinkDescription(link));
			}

			public void LogValueNodeLinkPair(valueNodeLinkPair<T> pair)
			{
				Console.WriteLine(ValueNodeLinkPairDescription(pair));
			}
#endif
#endif // SynchronizedLfdll
		}

		private class node<T> : ILockFreeDoublyLinkedListNode<T> where T : class
		{
			// ReSharper disable once InconsistentNaming
			public valueNodeLinkPair<T> Next_;

			// ReSharper disable once InconsistentNaming
			public nodeLink<T> Prev_;

			// ReSharper disable once InconsistentNaming
			public readonly lockFreeDoublyLinkedList<T> List_;

#if DEBUG
			public long Id { get; }

#endif

			public ILockFreeDoublyLinkedList<T> List => List_;

			public bool IsDummyNode =>
				this == List_.HeadNode || this == List_.TailNode;

			public T Value
			{
				get
				{
					if (IsDummyNode)
						throwIsDummyNodeException();
					/* At the commented out code it is assumed
                     * that Value_ is not allowed to be readout
                     * once the node was deleted.
                     * However, this behaviour does not seem useful. */
					//T val = this.newValue;
					//if (this.Next_.D)
					//    return default(T);

					Thread.MemoryBarrier();
					T value = Next_.Value;
					Thread.MemoryBarrier();
					return value;
				}
				set
				{
					if (IsDummyNode)
						throwIsDummyNodeException();
					Thread.MemoryBarrier();
					while (true)
					{
						valueNodeLinkPair<T> currentPair = Next_;
						if (Interlocked.CompareExchange(
							ref Next_,
							new valueNodeLinkPair<T>(value, currentPair.Link),
							currentPair) == currentPair)
						{
							break;
						}
					}
				}
			}

			public ILockFreeDoublyLinkedListNode<T> Next
			{
				get
				{
					node<T> cursor = this;
					bool b = toNext(ref cursor);
					Thread.MemoryBarrier();
					if (b)
						return cursor;
					return null;
				}
			}

			public ILockFreeDoublyLinkedListNode<T> Prev
			{
				get
				{
					node<T> cursor = this;
					bool b = toPrev(ref cursor);
					Thread.MemoryBarrier();
					if (b)
						return cursor;
					return null;
				}
			}

			public bool Removed
			{
				get
				{
					Thread.MemoryBarrier();
					bool result = Next_.Link.D;
					Thread.MemoryBarrier();
					return result;
				}
			}

			public ILockFreeDoublyLinkedListNode<T> InsertBefore(T newValue)
			{
				ILockFreeDoublyLinkedListNode<T> result = insertBefore(newValue, this);
				Thread.MemoryBarrier();
				return result;
			}

			public ILockFreeDoublyLinkedListNode<T> InsertAfter(T newValue)
			{
				ILockFreeDoublyLinkedListNode<T> result = insertAfter(newValue, this);
				Thread.MemoryBarrier();
				return result;
			}

			public ILockFreeDoublyLinkedListNode<T> InsertAfterIf(T newValue, Func<T, bool> condition)
			{
				if (IsDummyNode)
					return null;

				SpinWait spin = new SpinWait();
				node<T> cursor = this;
				node<T> node = new node<T>(List_);
				node<T> prev = cursor;
				node<T> next;
				while (true)
				{

					Thread.MemoryBarrier();
					List_.EnterTestSynchronizedBlock();
					valueNodeLinkPair<T> nextLink = prev.Next_;
#if SynchronizedLfdll_Verbose
					Console.WriteLine("insertAfterIf Step 0");
					Console.WriteLine("nextLink =");
					List_.LogValueNodeLinkPair(nextLink);
#endif
					List_.LeaveTestSynchronizedBlock();

					next = nextLink.Link.P;
					node.Prev_ = new nodeLink<T>(prev, false);
					node.Next_ = new valueNodeLinkPair<T>(newValue,
						new nodeLink<T>(next, false));

					bool cexSuccess;
					valueNodeLinkPair<T> currentPair = nextLink;
					while (true)
					{
						if (!condition(currentPair.Value))
						{
							Thread.MemoryBarrier();
							return null;
						}
						if (!currentPair.Link.Equals(
							new nodeLink<T>(next, false)))
						{
							cexSuccess = false;
							break;
						}

						List_.EnterTestSynchronizedBlock();
						valueNodeLinkPair<T> prevalent
							= Interlocked.CompareExchange
							(ref cursor.Next_,
							new valueNodeLinkPair<T>(currentPair.Value,
									new nodeLink<T>(node, false)),
							currentPair);
#if SynchronizedLfdll_Verbose
						Console.WriteLine("InsertAfterIf Step 1");
						Console.WriteLine(
							"CompareExchange(ref cursor.Next_, {0}, {1}) = {2}",
							new valueNodeLinkPair<T>(currentPair.Value,
									new nodeLink<T>(node, false)),
							currentPair,
							prevalent);
						Console.WriteLine("cursor =");
						List_.LogNode(cursor);
#endif
						List_.LeaveTestSynchronizedBlock();

						if (ReferenceEquals(prevalent, currentPair))
						{
							cexSuccess = true;
							break;
						}
						currentPair = prevalent;
					}

					if (cexSuccess)
						break;

					if (currentPair.Link.D)
					{
						Thread.MemoryBarrier();
						return null;
					}
					spin.SpinOnce();
				}
				List_.CorrectPrev(prev, next);
				Thread.MemoryBarrier();
				return node;
			}


			public bool Remove() // out T lastValue
			{
				if (IsDummyNode)
				{
					// lastValue = default(T);
					return false;
				}
				while (true)
				{

					Thread.MemoryBarrier();
					List_.EnterTestSynchronizedBlock();
					nodeLink<T> next = Next_.Link;
#if SynchronizedLfdll_Verbose
					Console.WriteLine("Remove Step 0");
					Console.WriteLine("next = {0}", List_.NodeLinkDescription(next));
#endif
					List_.LeaveTestSynchronizedBlock();

					if (next.D)
					{
						// lastValue = default(T);
						Thread.MemoryBarrier();
						return false;
					}

					List_.EnterTestSynchronizedBlock();
					bool b = compareExchangeNodeLinkInPair(ref Next_,
						new nodeLink<T>(next.P, true), next);
#if SynchronizedLfdll_Verbose
					Console.WriteLine("Remove Step 1");
					Console.WriteLine("compareExchangeNl(ref this.Next_, {0}, {1}) = {2}",
						List_.NodeLinkDescription(new nodeLink<T>(next.P, true)),
						List_.NodeLinkDescription(next),
						b);
					Console.WriteLine("this =");
					List_.LogNode(this);
					List_.LogState();
#endif
					List_.LeaveTestSynchronizedBlock();

					if (b)
					{
						nodeLink<T> prev;
						while (true)
						{

							/* Not necessary because of preceding compareExchange. */
							// Thread.MemoryBarrier();

							List_.EnterTestSynchronizedBlock();
							prev = Prev_;
#if SynchronizedLfdll_Verbose
							Console.WriteLine("Remove Step 2");
							Console.WriteLine("prev = {0}", List_.NodeLinkDescription(Prev_));
#endif
							List_.LeaveTestSynchronizedBlock();

							if (prev.D)
								break;

							List_.EnterTestSynchronizedBlock();
							bool b1 = compareExchangeNodeLink(ref Prev_,
								new nodeLink<T>(prev.P, true), prev);
#if SynchronizedLfdll_Verbose
							Console.WriteLine("Remove Step 3");
							Console.WriteLine("compareExchangeNl(ref this.Prev_, {0}, {1}) = {2}",
								List_.NodeLinkDescription(new nodeLink<T>(prev.P, true)),
								List_.NodeLinkDescription(prev),
								b1);
							Console.WriteLine("this =");
							List_.LogNode(this);
							List_.LogState();
#endif
							List_.LeaveTestSynchronizedBlock();

							if (b1)
								break;
						}
						List_.CorrectPrev(prev.P, next.P);
						//lastValue = this.newValue;
						Thread.MemoryBarrier();
						return true;
					}
				}
			}

			public T CompareExchangeValue(T newValue, T comparand)
			{
				valueNodeLinkPair<T> currentPair;
				Thread.MemoryBarrier();
				while (true)
				{
					currentPair = Next_;
					if (!ReferenceEquals(currentPair.Value, comparand))
						return currentPair.Value;
					if (ReferenceEquals(
							Interlocked.CompareExchange(
								ref Next_,
								new valueNodeLinkPair<T>(
									newValue, currentPair.Link),
								currentPair),
							currentPair))
					{
						break;
					}
				}
				return currentPair.Value;
			}

			public node(lockFreeDoublyLinkedList<T> list)
			{
				List_ = list;
#if DEBUG

				Id = Interlocked.Increment(ref nextId) - 1;
#endif
				/* Value_ is flushed
                 * at the moment the current node instance is published
                 * (by CompareExchange). */
			}

			#region private
#if DEBUG

			// ReSharper disable once StaticFieldInGenericType
			private static long nextId = 0;

#endif

			private void throwIsDummyNodeException()
			{
				throw new InvalidOperationException(
					"The current node is the dummy head or dummy tail node " +
					"of the current List, so it may not store any value.");
			}

			private bool toNext(ref node<T> cursor)
			{
				while (true)
				{
					if (cursor == List_.TailNode)
						return false;

					Thread.MemoryBarrier();
					List_.EnterTestSynchronizedBlock();
					node<T> next = cursor.Next_.Link.P;
#if SynchronizedLfdll_Verbose
					Console.WriteLine("toNext Step 0");
					Console.WriteLine("next =");
					List_.LogNode(next);
#endif
					List_.LeaveTestSynchronizedBlock();

					Thread.MemoryBarrier();
					List_.EnterTestSynchronizedBlock();
					bool d = next.Next_.Link.D;
#if SynchronizedLfdll_Verbose
					Console.WriteLine("toNext Step 1");
					Console.WriteLine("d = {0}", d);
#endif
					List_.LeaveTestSynchronizedBlock();

					bool b = d;
					if (b)
					{

						Thread.MemoryBarrier();
						List_.EnterTestSynchronizedBlock();
						b &= !cursor.Next_.Link.Equals(new nodeLink<T>(next, true));
#if SynchronizedLfdll_Verbose
						Console.WriteLine("toNext Step 2");
						Console.WriteLine("!cursor.Next_.Link.Equals(new nodeLink<T>(next, true)) = {0}",
							!cursor.Next_.Link.Equals(new nodeLink<T>(next, true)));
#endif
						List_.LeaveTestSynchronizedBlock();

					}
					if (b)
					{
						List_.SetMark(ref next.Prev_);

						Thread.MemoryBarrier();
						List_.EnterTestSynchronizedBlock();
						node<T> p = next.Next_.Link.P;
#if SynchronizedLfdll_Verbose
						Console.WriteLine("toNext Step 3");
						Console.WriteLine("p =");
						List_.LogNode(p);
#endif
						List_.LeaveTestSynchronizedBlock();

						List_.EnterTestSynchronizedBlock();
						// ReSharper disable once UnusedVariable
						bool b1 = compareExchangeNodeLinkInPair(ref cursor.Next_,
							(nodeLink<T>)p, (nodeLink<T>)next);
#if SynchronizedLfdll_Verbose
						Console.WriteLine("Remove Step 4");
						Console.WriteLine("compareExchangeNlIp(ref cursor.Next_, {0}, {1}) = {2}",
							List_.NodeLinkDescription((nodeLink<T>)p),
							List_.NodeLinkDescription((nodeLink<T>)next),
							b1);
						Console.WriteLine("cursor =");
						List_.LogNode(cursor);
						List_.LogState();
#endif
						List_.LeaveTestSynchronizedBlock();

						continue;
					}
					cursor = next;
					if (!d)
						return true;
				}
			}

			private bool toPrev(ref node<T> cursor)
			{
				while (true)
				{
					if (cursor == List_.HeadNode)
						return false;

					Thread.MemoryBarrier();
					List_.EnterTestSynchronizedBlock();
					node<T> prev = cursor.Prev_.P;
#if SynchronizedLfdll_Verbose
					Console.WriteLine("toPrev Step 0");
					Console.WriteLine("prev =");
					List_.LogNode(prev);
#endif
					List_.LeaveTestSynchronizedBlock();

					Thread.MemoryBarrier();
					List_.EnterTestSynchronizedBlock();
					bool b = prev.Next_.Link.Equals(new nodeLink<T>(cursor, false));
#if SynchronizedLfdll_Verbose
					Console.WriteLine("toPrev Step 1");
					Console.WriteLine("b = {0}", b);
#endif
					List_.LeaveTestSynchronizedBlock();

					if (b)
					{

						Thread.MemoryBarrier();
						List_.EnterTestSynchronizedBlock();
						b &= !cursor.Next_.Link.D;
#if SynchronizedLfdll_Verbose
						Console.WriteLine("toPrev Step 2");
						Console.WriteLine("!cursor.Next_.Link.D = {0}", !cursor.Next_.Link.D);
#endif
						List_.LeaveTestSynchronizedBlock();

					}
					if (b)
					{
						cursor = prev;
						return true;
					}
					else
					{
						Thread.MemoryBarrier();
						List_.EnterTestSynchronizedBlock();
						bool b1 = cursor.Next_.Link.D;
#if SynchronizedLfdll_Verbose
						Console.WriteLine("toPrev Step 3");
						Console.WriteLine("b1 = {0}", b1);
#endif
						List_.LeaveTestSynchronizedBlock();

						if (b1)
							toNext(ref cursor);
						else
							List_.CorrectPrev(prev, cursor);
					}
				}
			}

			private ILockFreeDoublyLinkedListNode<T> insertBefore(
				T value, node<T> cursor, SpinWait spin = new SpinWait())
			{
				if (cursor == List_.HeadNode)
					return InsertAfter(value);
				node<T> node = new node<T>(List_);

				Thread.MemoryBarrier();
				List_.EnterTestSynchronizedBlock();
				node<T> prev = cursor.Prev_.P;
#if SynchronizedLfdll_Verbose
				Console.WriteLine("insertBefore Step 0");
				Console.WriteLine("prev =");
				List_.LogNode(prev);
#endif
				List_.LeaveTestSynchronizedBlock();

				node<T> next;
				while (true)
				{
					while (true)
					{

						Thread.MemoryBarrier();
						List_.EnterTestSynchronizedBlock();
						bool b = !cursor.Next_.Link.D;
#if SynchronizedLfdll_Verbose
						Console.WriteLine("insertBefore Step 1");
						Console.WriteLine("b = {0}", b);
#endif
						List_.LeaveTestSynchronizedBlock();

						if (b)
							break;

						/* Since cursor was deleted
                         * the method correctPrev has not returned a node 
                         * which is logically before cursor;
                         * the return value shall not have semantic meaning.
                         * As correctPrev apparently exprects
                         * a logical predecessor of node / cursor,
                         * prev cannot be passed to the method.
                         * This is dire for program execution
                         * especially when prev == List_.tailNode. */

						toNext(ref cursor);

						#region Bugfix 1
						/* Ascertain a new predecessor of cursor. */
						Thread.MemoryBarrier();
						List_.EnterTestSynchronizedBlock();
						prev = cursor.Prev_.P;
#if SynchronizedLfdll_Verbose
						Console.WriteLine("insertBefore Step 1.1");
						Console.WriteLine("prev =");
						List_.LogNode(prev);
#endif
						List_.LeaveTestSynchronizedBlock();
						#endregion

						prev = List_.CorrectPrev(prev, cursor);
					}
					next = cursor;
					node.Prev_ = new nodeLink<T>(prev, false);
					node.Next_ = new valueNodeLinkPair<T>(value,
						new nodeLink<T>(next, false));

					List_.EnterTestSynchronizedBlock();
					bool b1 = compareExchangeNodeLinkInPair(ref prev.Next_,
						new nodeLink<T>(node, false),
						new nodeLink<T>(cursor, false));
#if SynchronizedLfdll_Verbose
					Console.WriteLine("insertBefore Step 2");
					Console.WriteLine("compareExchangeNlIp(ref prev.Next_, {0}, {1}) = {2}",
						List_.NodeLinkDescription(new nodeLink<T>(node, false)),
						List_.NodeLinkDescription(new nodeLink<T>(cursor, false)),
						b1);
					Console.WriteLine("prev =");
					List_.LogNode(prev);
					List_.LogState();
#endif
					List_.LeaveTestSynchronizedBlock();

					if (b1)
						break;

					prev = List_.CorrectPrev(prev, cursor);
					spin.SpinOnce();
				}

				List_.CorrectPrev(prev, next);
				return node;
			}

			private ILockFreeDoublyLinkedListNode<T> insertAfter(T value, node<T> cursor)
			{
				SpinWait spin = new SpinWait();

				if (cursor == List_.TailNode)
					return insertBefore(value, cursor, spin);
				node<T> node = new node<T>(List_);
				node<T> prev = cursor;
				node<T> next;
				while (true)
				{

					Thread.MemoryBarrier();
					List_.EnterTestSynchronizedBlock();
					next = prev.Next_.Link.P;
#if SynchronizedLfdll_Verbose
					Console.WriteLine("insertAfter Step 0");
					Console.WriteLine("next =");
					List_.LogNode(next);
#endif
					List_.LeaveTestSynchronizedBlock();

					node.Prev_ = new nodeLink<T>(prev, false);
					node.Next_ = new valueNodeLinkPair<T>(value,
						new nodeLink<T>(next, false));

					List_.EnterTestSynchronizedBlock();
					bool b1 = compareExchangeNodeLinkInPair(ref cursor.Next_,
						new nodeLink<T>(node, false),
						new nodeLink<T>(next, false));
#if SynchronizedLfdll_Verbose
					Console.WriteLine("insertAfter Step 1");
					Console.WriteLine("compareExchangeNlIp(ref cursor.Next_, {0}, {1}) = {2}",
						List_.NodeLinkDescription(new nodeLink<T>(node, false)),
						List_.NodeLinkDescription(new nodeLink<T>(next, false)),
						b1);
					Console.WriteLine("cursor =");
					List_.LogNode(cursor);
					List_.LogState();
#endif
					List_.LeaveTestSynchronizedBlock();

					if (b1)
						break;

					/* Not necessary because of preceding compareExchange. */
					// Thread.MemoryBarrier();

					List_.EnterTestSynchronizedBlock();
					bool b = prev.Next_.Link.D;
#if SynchronizedLfdll_Verbose
					Console.WriteLine("insertAfter Step 2");
					Console.WriteLine("b = {0}", b);
					List_.LogNode(next);
#endif
					List_.LeaveTestSynchronizedBlock();

					if (b)
						return insertBefore(value, cursor, spin);
					spin.SpinOnce();
				}
				List_.CorrectPrev(prev, next);
				return node;
			}
			#endregion
		}

		private class nodeLink<T> where T : class
		{
			public node<T> P { get; }
			public bool D { get; }

			public override bool Equals(object obj)
			{
				if (!(obj is nodeLink<T>))
					return false;

				var other = (nodeLink<T>)obj;
				return D == other.D && P == other.P;
			}

			public bool Equals(nodeLink<T> other)
			{
				if (other == null)
					return false;

				return D == other.D && P == other.P;
			}

			public override int GetHashCode()
			{
				int hash = 17;
				hash = hash * 486187739 + P.GetHashCode();
				hash = hash * 486187739 + D.GetHashCode();
				return hash;
			}

			public static explicit operator nodeLink<T>(node<T> node)
			{
				return new nodeLink<T>(node, false);
			}

			public static explicit operator node<T>(nodeLink<T> link)
			{
#if DEBUG
				/* I am not sure,
                 * whether it is simply assumed in the document
                 * that the conversion is always possible. */
				if (link.D)
					throw new ArgumentException();
#endif
				return link.P;
			}

			public nodeLink(node<T> p, bool d)
			{
				P = p;
				D = d;
			}
		}

		private class valueNodeLinkPair<T> where T : class
		{
			public T Value { get; }
			public nodeLink<T> Link { get; }

			public valueNodeLinkPair(T value, nodeLink<T> link)
			{
				Value = value;
				Link = link;
			}
		}
		#endregion
	}
}
