//Copyright 2014 Christoph Müller

//Licensed under the Apache License, Version 2.0 (the "License");
//you may not use this file except in compliance with the License.
//You may obtain a copy of the License at

//   http://www.apache.org/licenses/LICENSE-2.0

//Unless required by applicable law or agreed to in writing, software
//distributed under the License is distributed on an "AS IS" BASIS,
//WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//See the License for the specific language governing permissions and
//limitations under the License.


//#define SynchronizedLfdll
//#define Verbose
//#define DEBUG

/* Atomicity of the PopLeft method is not supported
 * by the current LFDLL implementation. */
//#define PopLeft


#if SynchronizedLfdll
#if Verbose
#define SynchronizedLfdll_Verbose
#endif
#endif

using System;
using System.Collections.Generic;
using System.Threading;

namespace LockFreeDoublyLinkedList
{
    /// <summary>
    /// A lock free doubly linked list for high concurrency.
    /// </summary>
    /// <typeparam name="T">The type of the values.</typeparam>
    public class LockFreeDoublyLinkedList<T> : IEnumerable<T>
        where T : class
    {
        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>A IEnumerator&lt;T&gt; that can be used to iterate through the collection.</returns>
        public IEnumerator<T> GetEnumerator()
        {
            INode current = Head;
            while (true)
            {
                current = current.Next;
                if (current == null)
                    yield break;
                yield return current.Value;
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>A IEnumerator&lt;T&gt; that can be used to iterate through the collection.</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <summary>
        /// The dummy head node (leftmost).
        /// </summary>
        public INode Head { get { return headNode; } }
        /// <summary>
        /// The dummy tail node (rightmost).
        /// </summary>
        public INode Tail { get { return tailNode; } }

        /// <summary>
        /// Inserts a new node at the head position.
        /// </summary>
        /// <param name="value">The initial value of the new node.</param>
        /// <returns>The new inserted node.</returns>
        public INode PushLeft(T value)
        {
            SpinWait spin = new SpinWait();
            node node = new node(this);
            node prev = headNode;

            Thread.MemoryBarrier();
            enterTestSynchronizedBlock();
            node next = prev.Next_.Link.P;
#if SynchronizedLfdll_Verbose
            Console.WriteLine("PushLeft {0} Step 0", value);
            Console.WriteLine("next =");
            LogNode(next);
#endif
            leaveTestSynchronizedBlock();

            while (true)
            {
                /* node has not been made public yet,
                 * so no synchronization constructs are necessary. */
                node.Prev_ = new nodeLink(prev, false);
                node.Next_ = new valueNodeLinkPair
                    (value, new nodeLink(next, false));

                enterTestSynchronizedBlock();
                bool b = compareExchangeNodeLinkInPair(ref prev.Next_,
                    new nodeLink(node, false),
                    new nodeLink(next, false));
#if SynchronizedLfdll_Verbose
                Console.WriteLine("PushLeft {0} Step 1", value);
                Console.WriteLine("compareExchangeNlIp(ref prev.Next_, {0}, {1}) = {2}",
                    nodeLinkDescription(new nodeLink(node, false)),
                    nodeLinkDescription(new nodeLink(next, false)),
                    b);
                Console.WriteLine("prev = ");
                LogNode(prev);
                LogState();
#endif
                leaveTestSynchronizedBlock();

                if (b)
                    break;

                /* Not necessary because of preceding compareExchange. */
                // Thread.MemoryBarrier();

                enterTestSynchronizedBlock();
                next = prev.Next_.Link.P;
#if SynchronizedLfdll_Verbose
                Console.WriteLine("PushLeft {0} Step 2", value);
                Console.WriteLine("next =");
                LogNode(next);
#endif
                leaveTestSynchronizedBlock();

                spin.SpinOnce();
            }
            pushEnd(node, next, spin);
            Thread.MemoryBarrier();
            return node;
        }
        /// <summary>
        /// Inserts a new node at the tail position.
        /// </summary>
        /// <param name="value">The initial value of the new node.</param>
        /// <returns>The new inserted node.</returns>
        public INode PushRight(T value)
        {
            SpinWait spin = new SpinWait();
            node node = new node(this);
            node next = tailNode;

            Thread.MemoryBarrier();
            enterTestSynchronizedBlock();
            node prev = next.Prev_.P;
#if SynchronizedLfdll_Verbose
            Console.WriteLine("PushRight {0} Step 0", value);
            Console.WriteLine("prev = ");
            LogNode(prev);
#endif
            leaveTestSynchronizedBlock();

            while (true)
            {
                /* node has not been made public yet,
                 * so no threading constructs are necessary. */
                node.Prev_ = new nodeLink(prev, false);
                node.Next_ = new valueNodeLinkPair
                    (value, new nodeLink(next, false));

                enterTestSynchronizedBlock();
                bool b = compareExchangeNodeLinkInPair(ref prev.Next_,
                    new nodeLink(node, false),
                    new nodeLink(next, false));
#if SynchronizedLfdll_Verbose
                Console.WriteLine("PushRight {0} Step 1", value);
                Console.WriteLine("compareExchangeNlIp(ref prev.Next_, {0}, {1}) = {2}",
                    nodeLinkDescription(new nodeLink(node, false)),
                    nodeLinkDescription(new nodeLink(next, false)),
                    b);
                Console.WriteLine("prev = ");
                LogNode(prev);
                LogState();
#endif
                leaveTestSynchronizedBlock();

                if (b)
                    break;

                prev = correctPrev(prev, next);
                spin.SpinOnce();
            }
            pushEnd(node, next, spin);
            Thread.MemoryBarrier();
            return node;
        }

#if PopLeft
        /// <summary>
        /// Removes the leftmost non-dummy node and returns its value.
        /// </summary>
        /// <returns>The value of the removed node.</returns>
        public Tuple<T> PopLeft()
        {
            SpinWait spin = new SpinWait();
            node prev = headNode;
            while (true)
            {
                Thread.MemoryBarrier();
                enterTestSynchronizedBlock();
                node node = prev.Next_.Link.P;
#if SynchronizedLfdll_Verbose
                Console.WriteLine("PopLeft Step 0");
                Console.WriteLine("node = ");
                LogNode(node);
#endif
                leaveTestSynchronizedBlock();

                if (node == tailNode)
                {
                    Thread.MemoryBarrier();
                    return null;
                }

                Thread.MemoryBarrier();
                enterTestSynchronizedBlock();
                nodeLink next = node.Next_.Link;
#if SynchronizedLfdll_Verbose
                Console.WriteLine("PopLeft Step 1");
                Console.WriteLine("next = {0}", nodeLinkDescription(next));
#endif
                leaveTestSynchronizedBlock();

                if (next.D)
                {
                    setMark(ref node.Prev_);

                    enterTestSynchronizedBlock();
                    // ReSharper disable once UnusedVariable
                    bool b1 = compareExchangeNodeLinkInPair(ref prev.Next_,
                        new nodeLink(next.P, false),
                        (nodeLink)node);
#if SynchronizedLfdll_Verbose
                    Console.WriteLine("PopLeft Step 2");
                    Console.WriteLine("compareExchangeNlIp(ref prev.Next_, {0}, {1}) = {2}",
                        nodeLinkDescription(new nodeLink(next.P, false)),
                        nodeLinkDescription((nodeLink)node),
                        b1);
                    Console.WriteLine("prev = ");
                    LogNode(prev);
                    LogState();
#endif
                    leaveTestSynchronizedBlock();

                    continue;
                }

                enterTestSynchronizedBlock();
                bool b = compareExchangeNodeLinkInPair(ref node.Next_,
                    new nodeLink(next.P, true), next);
#if SynchronizedLfdll_Verbose
                Console.WriteLine("PopLeft Step 3");
                Console.WriteLine("compareExchangeNlIp(ref node.Next_, {0}, {1}) = {2}",
                    nodeLinkDescription(new nodeLink(next.P, true)),
                    nodeLinkDescription(next),
                    b);
                Console.WriteLine("node = ");
                LogNode(node);
                LogState();
#endif
                leaveTestSynchronizedBlock();

                if (b)
                {
                    correctPrev(prev, next.P);

                    Thread.MemoryBarrier();
                    enterTestSynchronizedBlock();
                    T value = node.Next_.Value;
#if SynchronizedLfdll_Verbose
                    Console.WriteLine("PopLeft Step 4");
                    Console.WriteLine("value = {0}", value);
#endif
                    leaveTestSynchronizedBlock();

                    Thread.MemoryBarrier();
                    return new Tuple<T>(value);
                }

                spin.SpinOnce();
            }
        }
#endif
        /// <summary>
        /// Removes the rightmost non-dummy node if it exists
        /// and returns its value after the removal.
        /// </summary>
        /// <returns>
        /// null, if the list is empty; else a Tuple&lt;T&gt;,
        /// which contains the value of the removed node.
        /// </returns>
        [ObsoleteAttribute("This method is not atomar. For clarity use PopRightNode instead.", false)]
        public Tuple<T> PopRight()
        {
            INode node = PopRightNode();
            if (node == null)
                return null;
            return Tuple.Create(node.Value);
        }

        /// <summary>
        /// Removes the rightmost non-dummy node, if it exists.
        /// </summary>
        /// <returns>
        /// null, if the list is empty; else the removed node.
        /// </returns>
        public INode PopRightNode()
        {
            SpinWait spin = new SpinWait();
            node next = tailNode;

            Thread.MemoryBarrier();
            enterTestSynchronizedBlock();
            node node = next.Prev_.P;
#if SynchronizedLfdll_Verbose
            Console.WriteLine("PopRight Step 0");
            Console.WriteLine("node  =");
            LogNode(node);
#endif
            leaveTestSynchronizedBlock();

            while (true)
            {

                Thread.MemoryBarrier();
                enterTestSynchronizedBlock();
                bool b = !node.Next_.Link.Equals(new nodeLink(next, false));
#if SynchronizedLfdll_Verbose
                Console.WriteLine("PopRight Step 1");
                Console.WriteLine("b = {0}", b);
#endif
                leaveTestSynchronizedBlock();

                if (b)
                {
                    node = correctPrev(node, next);
                    continue;
                }

                if (node == headNode)
                {
                    Thread.MemoryBarrier();
                    return null;
                }

                enterTestSynchronizedBlock();
                bool b1 = compareExchangeNodeLinkInPair(ref node.Next_,
                    new nodeLink(next, true),
                    new nodeLink(next, false));
#if SynchronizedLfdll_Verbose
                Console.WriteLine("PopRight Step 2");
                Console.WriteLine("compareExchangeNlIp(ref node.Next_, {0}, {1}) = {2}",
                    nodeLinkDescription(new nodeLink(next, true)),
                    nodeLinkDescription(new nodeLink(next, false)),
                    b1);
                Console.WriteLine("node = ");
                LogNode(node);
                LogState();
#endif
                leaveTestSynchronizedBlock();

                if (b1)
                {
                    /* Not necessary because of preceding compareExchange. */
                    // Thread.MemoryBarrier();

                    enterTestSynchronizedBlock();
                    node prev = node.Prev_.P;
#if SynchronizedLfdll_Verbose
                    Console.WriteLine("PopRight Step 3");
                    Console.WriteLine("prev =");
                    LogNode(prev);
#endif
                    leaveTestSynchronizedBlock();

                    correctPrev(prev, next);

                    Thread.MemoryBarrier();
                    return node;
                }

                spin.SpinOnce();
            }
        }

        /// <summary>
        /// Creates a new empty LockFreeDoublyLinkedList.
        /// </summary>
        public LockFreeDoublyLinkedList()
        {
            headNode = new node(this);
            tailNode = new node(this);

            headNode.Prev_ = new nodeLink(null, false);
            headNode.Next_ = new valueNodeLinkPair
                (null, new nodeLink(tailNode, false));
            tailNode.Prev_ = new nodeLink(headNode, false);
            tailNode.Next_ = new valueNodeLinkPair
                (null, new nodeLink(null, false));
            Thread.MemoryBarrier();
        }

        /// <summary>
        /// Creates a new LockFreeDoublyLinkedList
        /// which contains the contents of the enumeration initial.
        /// </summary>
        /// <param name="initial">The enumeration to copy.</param>
        public LockFreeDoublyLinkedList(IEnumerable<T> initial)
            : this()
        {
            if (initial == null)
                throw new ArgumentNullException("initial");
            foreach (T value in initial)
                PushRight(value);
        }

        /// <summary>
        /// A node of a LockFreeDoublyLinkedList instance.
        /// </summary>
        public interface INode
        {
#if DEBUG
            long Id { get; }

#endif
            /// <summary>
            /// The value stored by the current node instance.
            /// </summary>
            T Value { get; set; }

            /// <summary>
            /// Returns the corresponding LockFreeDoublyLinkedList
            /// of the current node instance.
            /// </summary>
            LockFreeDoublyLinkedList<T> List { get; } 
                
            /// <summary>
            /// The right neighbor node or null,
            /// if the current node is the dummy tail node.
            /// </summary>
            INode Next { get; }
            /// <summary>
            /// The left neighbor node or null,
            /// if the current node is the dummy head node.
            /// </summary>
            INode Prev { get; }

            /// <summary>
            /// Returns, if the current node has been removed.
            /// </summary>
            bool Removed { get; }

            /// <summary>
            /// Inserts a new node left beside the current node instance.
            /// </summary>
            /// <param name="newValue">The initial value of the new node.</param>
            /// <returns>The new inserted node.</returns>
            INode InsertBefore(T newValue);
            /// <summary>
            /// Inserts a new node right beside the current node instance.
            /// </summary>
            /// <param name="newValue">The initial value of the new node.</param>
            /// <returns>The new inserted node.</returns>
            INode InsertAfter(T newValue);

            /// <summary>
            /// Inserts a new node after the current node instance
            /// if and only if:
            /// <list type="bullet">
            ///     <item><description>
            ///         the current value is no dummy node,
            ///     </description></item>
            ///     <item><description>
            ///         the current node has not yet been deleted
            ///     </description></item>
            ///     <item><description>
            ///         and the <paramref name="condition"/>
            ///         for the current node’s value is satisfied.
            ///     </description></item>
            /// </list>
            /// </summary>
            /// <param name="newValue">
            /// The new value for the node to insert.
            /// </param>
            /// <param name="condition">
            /// The condition which the current node’s value
            /// needs to satisfy for the insertion to take place.
            /// </param>
            /// <returns>
            /// The inserted node,
            /// if the insertion could be performed.
            /// <c>null</c> else.
            /// </returns>
            INode InsertAfterIf(T newValue, Func<T, bool> condition);

            /// <summary>
            /// Removes the current node
            /// from the corresponding LockFreeDoublyLinkedList instance.
            /// </summary>
            /// <returns>
            /// Whether the current node has been deleted by this thread.
            /// Also returns false if the current node is a dummy node.
            /// </returns>
            bool Remove();

            /// <summary>
            /// Compares the Value property to comparand
            /// and replaces the prevalent value with newValue
            /// if and only if comparand reference equals the prevalent value.
            /// This happens in a single atomic operation.
            /// </summary>
            /// <param name="newValue">The value to write.</param>
            /// <param name="comparand">The original value </param>
            /// <returns>
            /// The prevalent value,
            /// regardless of whether the replacement took place.
            /// </returns>
            T CompareExchangeValue(T newValue, T comparand);
        }

        private node headNode;
        private node tailNode;

        private void pushEnd(node node, node next, SpinWait spin)
        {
            while (true)
            {

                Thread.MemoryBarrier();
                enterTestSynchronizedBlock();
                nodeLink link1 = next.Prev_;
#if SynchronizedLfdll_Verbose
                Console.WriteLine("pushEnd Step 0");
                Console.WriteLine("link1 = {0}",
                    nodeLinkDescription(link1));
#endif
                leaveTestSynchronizedBlock();

                bool b = link1.D;
                if (!b)
                {
                    Thread.MemoryBarrier();
                    enterTestSynchronizedBlock();
                    b |= !node.Next_.Link.Equals(new nodeLink(next, false));
#if SynchronizedLfdll_Verbose
                    Console.WriteLine("pushEnd Step 1");
                    Console.WriteLine("!node.Next_.Link.Equals(new nodeLink(next, false) = {0}",
                        !node.Next_.Link.Equals(new nodeLink(next, false)));
#endif
                    leaveTestSynchronizedBlock();
                }
                if (b)
                    break;

                enterTestSynchronizedBlock();
                bool b1 = compareExchangeNodeLink(ref next.Prev_,
                    new nodeLink(node, false), link1);
#if SynchronizedLfdll_Verbose
                Console.WriteLine("pushEnd Step 2");
                Console.WriteLine("compareExchangeNl(next.Prev_, {0}, {1}) = {2}",
                    nodeLinkDescription(new nodeLink(node, false)),
                    nodeLinkDescription(link1),
                    b1);
                Console.WriteLine("next = ");
                LogNode(next);
                LogState();
#endif
                leaveTestSynchronizedBlock();

                if (b1)
                {

                    /* Not necessary because of preceding compareExchange. */
                    // Thread.MemoryBarrier();

                    enterTestSynchronizedBlock();
                    bool b2 = node.Prev_.D;
#if SynchronizedLfdll_Verbose
                    Console.WriteLine("pushEnd Step 3");
                    Console.WriteLine("b1 = {0}", b2);
#endif
                    leaveTestSynchronizedBlock();

                    if (b2)
                        correctPrev(node, next);
                    break;
                }

                spin.SpinOnce();
            }
        }

        private void setMark(ref nodeLink link)
        {
            Thread.MemoryBarrier();
            enterTestSynchronizedBlock();
            nodeLink node = link;
#if SynchronizedLfdll_Verbose
            Console.WriteLine("SetMark Step 0");
            Console.WriteLine("node = {0}", nodeLinkDescription(node));
#endif
            leaveTestSynchronizedBlock();

            while (true)
            {
                if (node.D)
                    break;

                enterTestSynchronizedBlock();
                nodeLink prevalent = Interlocked.CompareExchange<nodeLink>(
                    ref link, new nodeLink(node.P, true), node);
#if SynchronizedLfdll_Verbose
                Console.WriteLine("SetMark Step 1");
                Console.WriteLine("compareExchange(link, {0}, {1}) = {2}",
                    nodeLinkDescription(new nodeLink(node.P, true)),
                    nodeLinkDescription(node),
                    nodeLinkDescription(prevalent));
                Console.WriteLine("link = {0}", nodeLinkDescription(link));
#endif
                leaveTestSynchronizedBlock();

                // ReSharper disable once PossibleUnintendedReferenceComparison
                if (prevalent == node)
                    break;
                node = prevalent;
            }
        }

        private node correctPrev(node prev, node node)
        {
            // ReSharper disable once IntroduceOptionalParameters.Local
            return correctPrev(prev, node, new SpinWait());
        }

        private node correctPrev(node prev, node node, SpinWait spin)
        {
            node lastLink = null;
            while (true)
            {

                Thread.MemoryBarrier();
                enterTestSynchronizedBlock();
                nodeLink link1 = node.Prev_;
#if SynchronizedLfdll_Verbose
                Console.WriteLine("CorrectPrev Step 0");
                Console.WriteLine("link1 = {0}",
                    nodeLinkDescription(link1));
#endif
                leaveTestSynchronizedBlock();

                if (link1.D)
                    break;

                Thread.MemoryBarrier();
                enterTestSynchronizedBlock();
                nodeLink prev2 = prev.Next_.Link;
#if SynchronizedLfdll_Verbose
                Console.WriteLine("CorrectPrev Step 1");
                Console.WriteLine("prev2 = {0}",
                    nodeLinkDescription(prev2));
#endif
                leaveTestSynchronizedBlock();

                if (prev2.D)
                {
                    if (lastLink != null)
                    {
                        setMark(ref prev.Prev_);

                        enterTestSynchronizedBlock();
                        // ReSharper disable once UnusedVariable
                        bool b1 = compareExchangeNodeLinkInPair(ref lastLink.Next_,
                            (nodeLink)prev2.P, (nodeLink)prev);
#if SynchronizedLfdll_Verbose
                        Console.WriteLine("CorrectPrev Step 2");
                        Console.WriteLine("compareExchangeNlIp(lastLink.Next_, {0}, {1}) = {2}",
                            nodeLinkDescription((nodeLink)prev2.P),
                            nodeLinkDescription((nodeLink)prev),
                            b1);
                        Console.WriteLine("lastLink = ");
                        LogNode(lastLink);
                        LogState();
#endif
                        leaveTestSynchronizedBlock();

                        prev = lastLink;
                        lastLink = null;
                        continue;
                    }

                    Thread.MemoryBarrier();
                    enterTestSynchronizedBlock();
                    prev2 = prev.Prev_;
#if SynchronizedLfdll_Verbose
                    Console.WriteLine("CorrectPrev Step 3");
                    Console.WriteLine("prev2 = {0}",
                        nodeLinkDescription(prev2));
#endif
                    leaveTestSynchronizedBlock();

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

                enterTestSynchronizedBlock();
                bool b = compareExchangeNodeLink(ref node.Prev_,
                    new nodeLink(prev, false), link1);
#if SynchronizedLfdll_Verbose
                Console.WriteLine("CorrectPrev Step 4");
                Console.WriteLine("compareExchangeNl(node.Prev_, {0}, {1}) = {2}",
                    nodeLinkDescription(new nodeLink(prev, false)),
                    nodeLinkDescription(link1),
                    b);
                Console.WriteLine("node = ");
                LogNode(node);
                LogState();
#endif
                leaveTestSynchronizedBlock();

                if (b)
                {
                    /* Not necessary because of preceding compareExchange. */
                    // Thread.MemoryBarrier();

                    enterTestSynchronizedBlock();
                    bool b1 = prev.Prev_.D;
#if SynchronizedLfdll_Verbose
                    Console.WriteLine("CorrectPrev Step 5");
                    Console.WriteLine("b1 = {0}", b1);
#endif
                    leaveTestSynchronizedBlock();

                    if (b1)
                        continue;
                    break;
                }

                spin.SpinOnce();
            }
            return prev;
        }

        private void enterTestSynchronizedBlock()
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

        private void leaveTestSynchronizedBlock()
        {
#if SynchronizedLfdll
            if (NextStepWaitHandle.IsValueCreated)
                StepCompletedWaitHandle.Set();
#endif
        }

        private static bool compareExchangeNodeLink(ref nodeLink location,
            nodeLink value, nodeLink comparandByValue)
        {
            return ThreadingAdditions
                .ConditionalCompareExchange<nodeLink>(ref location,
                value, original => original.Equals(comparandByValue));
        }

        private static bool compareExchangeNodeLinkInPair(
            ref valueNodeLinkPair location, nodeLink newLink,
            nodeLink comparadByValue)
        {
            Thread.MemoryBarrier();
            T currentValue = location.Value;
            return ThreadingAdditions
                .ConditionalCompareExchange<valueNodeLinkPair>(ref location,
                    new valueNodeLinkPair(currentValue, newLink),
                    original =>
                        original.Link.Equals(comparadByValue)
                        && ReferenceEquals(original.Value, currentValue));
        }
#if SynchronizedLfdll
        public ThreadLocal<AutoResetEvent> NextStepWaitHandle
        {
            get { return nextStepWaitHandle; }
        }

        public AutoResetEvent StepCompletedWaitHandle
        {
            get { return stepCompletedWaitHandle; }
        }

        public Counter StepCounter { get { return counter; } }

#if SynchronizedLfdll_Verbose
        public void LogState()
        {
            node current = headNode;
            while (current != null)
            {
                LogNode(current);
                current = current.Next_.Link.P;
            }
        }

        public void LogNode(LockFreeDoublyLinkedList<T>.INode inode)
        {
            node node = (node)inode;
            Console.WriteLine(nodeName(node));
            if (node != null)
            {
                Console.WriteLine("    .Prev_ = "
                    + nodeLinkDescription(node.Prev_));
                Console.WriteLine("    .Next_ = "
                    + valueNodeLinkPairDescription(node.Next_));
            }
        }
#endif

        private readonly AutoResetEvent stepCompletedWaitHandle
            = new AutoResetEvent(false);
        private ThreadLocal<AutoResetEvent> nextStepWaitHandle =
            new ThreadLocal<AutoResetEvent>();
        private readonly Counter counter = new Counter();

#if SynchronizedLfdll_Verbose
        private string nodeName(node node)
        {
            if (node == null)
                return "null";
            if (node == headNode)
                return "HeadNode";
            if (node == tailNode)
                return "TailNode";
            return "Node " + node.Value;
        }
        private string nodeLinkDescription(nodeLink link)
        {
            return "(" + nodeName(link.P)
                + ", " + link.D + ")";
        }

        private string valueNodeLinkPairDescription
            (valueNodeLinkPair pair)
        {
            return "(" + pair.Value + ", "
                   + nodeLinkDescription(pair.Link) + ")";
        }

        private void logNodeLink(nodeLink link)
        {
            Console.WriteLine(nodeLinkDescription(link));
        }

        private void logValueNodeLinkPair(valueNodeLinkPair pair)
        {
            Console.WriteLine(valueNodeLinkPairDescription(pair));
        }
#endif
#endif // SynchronizedLfdll

        private class node : INode
        {
            // ReSharper disable once InconsistentNaming
            public valueNodeLinkPair Next_;
            // ReSharper disable once InconsistentNaming
            public nodeLink Prev_;
            // ReSharper disable once InconsistentNaming
            public readonly LockFreeDoublyLinkedList<T> List_;

#if DEBUG
            public long Id { get; private set; }

#endif

            public LockFreeDoublyLinkedList<T> List
            {
                get { return List_; }
            }

            public T Value
            {
                get
                {
                    if (this == List_.headNode || this == List_.tailNode)
                        throw new InvalidOperationException();
                    /* At the commented out code it is assumed
                     * that Value_ is not allowed to be readout
                     * once the node was deleted.
                     * However, this behaviour does not seem useful. */
                    //T val = this.newValue;
                    //if (this.Next_.D)
                    //    return default(T);

                    Thread.MemoryBarrier();
                    T value = this.Next_.Value;
                    Thread.MemoryBarrier();
                    return value;
                }
                set
                {
                    if (this == List_.headNode || this == List_.tailNode)
                        throw new InvalidOperationException();
                    Thread.MemoryBarrier();
                    while (true)
                    {
                        valueNodeLinkPair currentPair = this.Next_;
                        if (Interlocked.CompareExchange<valueNodeLinkPair>(
                            ref this.Next_,
                            new valueNodeLinkPair(value, currentPair.Link),
                            currentPair) == currentPair)
                        {
                            break;
                        }
                    }
                }
            }

            public INode Next
            {
                get
                {
                    node cursor = this;
                    bool b = toNext(ref cursor);
                    Thread.MemoryBarrier();
                    if (b)
                        return cursor;
                    return null;
                }
            }
            public INode Prev
            {
                get
                {
                    node cursor = this;
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
                    bool result = this.Next_.Link.D;
                    Thread.MemoryBarrier();
                    return result;
                }
            }

            public INode InsertBefore(T newValue)
            {
                INode result = insertBefore(newValue, this);
                Thread.MemoryBarrier();
                return result;
            }
            public INode InsertAfter(T newValue)
            {
                INode result = insertAfter(newValue, this);
                Thread.MemoryBarrier();
                return result;
            }

            public INode InsertAfterIf(T newValue, Func<T, bool> condition)
            {
                if (this == List_.tailNode || this == List_.headNode)
                    return null;

                SpinWait spin = new SpinWait();
                node cursor = this;
                node node = new node(List_);
                node prev = cursor;
                valueNodeLinkPair nextLink;
                node next;
                while (true)
                {

                    Thread.MemoryBarrier();
                    List_.enterTestSynchronizedBlock();
                    nextLink = prev.Next_;
#if SynchronizedLfdll_Verbose
                    Console.WriteLine("insertAfterIf Step 0");
                    Console.WriteLine("nextLink =");
                    List_.logValueNodeLinkPair(nextLink);
#endif
                    List_.leaveTestSynchronizedBlock();

                    next = nextLink.Link.P;
                    node.Prev_ = new nodeLink(prev, false);
                    node.Next_ = new valueNodeLinkPair(newValue,
                        new nodeLink(next, false));

                    bool cexSuccess;
                    valueNodeLinkPair currentPair = nextLink;
                    while (true)
                    {
                        if (!condition(currentPair.Value))
                        {
                            Thread.MemoryBarrier();
                            return null;
                        }
                        if (!currentPair.Link.Equals(
                            new nodeLink(next, false)))
                        {
                            cexSuccess = false;
                            break;
                        }
                        
                        List_.enterTestSynchronizedBlock();
                        valueNodeLinkPair prevalent
                            = Interlocked.CompareExchange
                            (ref cursor.Next_,
                            new valueNodeLinkPair
                                (currentPair.Value,
                                    new nodeLink(node, false)),
                            currentPair);
#if SynchronizedLfdll_Verbose
                        Console.WriteLine("InsertAfterIf Step 1");
                        Console.WriteLine(
                            "CompareExchange(ref cursor.Next_, {0}, {1}) = {2}",
                            new valueNodeLinkPair
                                (currentPair.Value,
                                    new nodeLink(node, false)),
                            currentPair,
                            prevalent);
                        Console.WriteLine("cursor =");
                        List_.LogNode(cursor);
#endif
                        List_.leaveTestSynchronizedBlock();

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
                List_.correctPrev(prev, next);
                Thread.MemoryBarrier();
                return node;
            }


            public bool Remove() // out T lastValue
            {
                if (this == List_.headNode || this == List_.tailNode)
                {
                    // lastValue = default(T);
                    return false;
                }
                while (true)
                {

                    Thread.MemoryBarrier();
                    List_.enterTestSynchronizedBlock();
                    nodeLink next = this.Next_.Link;
#if SynchronizedLfdll_Verbose
                    Console.WriteLine("Remove Step 0");
                    Console.WriteLine("next = {0}", List_.nodeLinkDescription(next));
#endif
                    List_.leaveTestSynchronizedBlock();

                    if (next.D)
                    {
                        // lastValue = default(T);
                        Thread.MemoryBarrier();
                        return false;
                    }

                    List_.enterTestSynchronizedBlock();
                    bool b = compareExchangeNodeLinkInPair(ref this.Next_,
                        new nodeLink(next.P, true), next);
#if SynchronizedLfdll_Verbose
                    Console.WriteLine("Remove Step 1");
                    Console.WriteLine("compareExchangeNl(ref this.Next_, {0}, {1}) = {2}",
                        List_.nodeLinkDescription(new nodeLink(next.P, true)),
                        List_.nodeLinkDescription(next),
                        b);
                    Console.WriteLine("this =");
                    List_.LogNode(this);
                    List_.LogState();
#endif
                    List_.leaveTestSynchronizedBlock();

                    if (b)
                    {
                        nodeLink prev;
                        while (true)
                        {

                            /* Not necessary because of preceding compareExchange. */
                            // Thread.MemoryBarrier();

                            List_.enterTestSynchronizedBlock();
                            prev = this.Prev_;
#if SynchronizedLfdll_Verbose
                            Console.WriteLine("Remove Step 2");
                            Console.WriteLine("prev = {0}", List_.nodeLinkDescription(this.Prev_));
#endif
                            List_.leaveTestSynchronizedBlock();

                            if (prev.D)
                                break;

                            List_.enterTestSynchronizedBlock();
                            bool b1 = compareExchangeNodeLink(ref this.Prev_,
                                new nodeLink(prev.P, true), prev);
#if SynchronizedLfdll_Verbose
                            Console.WriteLine("Remove Step 3");
                            Console.WriteLine("compareExchangeNl(ref this.Prev_, {0}, {1}) = {2}",
                                List_.nodeLinkDescription(new nodeLink(prev.P, true)),
                                List_.nodeLinkDescription(prev),
                                b1);
                            Console.WriteLine("this =");
                            List_.LogNode(this);
                            List_.LogState();
#endif
                            List_.leaveTestSynchronizedBlock();

                            if (b1)
                                break;
                        }
                        List_.correctPrev(prev.P, next.P);
                        //lastValue = this.newValue;
                        Thread.MemoryBarrier();
                        return true;
                    }
                }
            }

            public node(LockFreeDoublyLinkedList<T> list)
            {
                this.List_ = list;
#if DEBUG

                Id = Interlocked.Increment(ref nextId) - 1;
#endif
                /* Value_ is flushed
                 * at the moment the current node instance is published
                 * (by CompareExchange). */
            }

#if DEBUG
            // ReSharper disable once StaticFieldInGenericType
            private static long nextId = 0;

#endif
            private bool toNext(ref node cursor)
            {
                while (true)
                {
                    if (cursor == List_.tailNode)
                        return false;

                    Thread.MemoryBarrier();
                    List_.enterTestSynchronizedBlock();
                    node next = cursor.Next_.Link.P;
#if SynchronizedLfdll_Verbose
                    Console.WriteLine("toNext Step 0");
                    Console.WriteLine("next =");
                    List_.LogNode(next);
#endif
                    List_.leaveTestSynchronizedBlock();

                    Thread.MemoryBarrier();
                    List_.enterTestSynchronizedBlock();
                    bool d = next.Next_.Link.D;
#if SynchronizedLfdll_Verbose
                    Console.WriteLine("toNext Step 1");
                    Console.WriteLine("d = {0}", d);
#endif
                    List_.leaveTestSynchronizedBlock();

                    bool b = d;
                    if (b)
                    {

                        Thread.MemoryBarrier();
                        List_.enterTestSynchronizedBlock();
                        b &= !cursor.Next_.Link.Equals(new nodeLink(next, true));
#if SynchronizedLfdll_Verbose
                        Console.WriteLine("toNext Step 2");
                        Console.WriteLine("!cursor.Next_.Link.Equals(new nodeLink(next, true)) = {0}",
                            !cursor.Next_.Link.Equals(new nodeLink(next, true)));
#endif
                        List_.leaveTestSynchronizedBlock();

                    }
                    if (b)
                    {
                        List_.setMark(ref next.Prev_);

                        Thread.MemoryBarrier();
                        List_.enterTestSynchronizedBlock();
                        node p = next.Next_.Link.P;
#if SynchronizedLfdll_Verbose
                        Console.WriteLine("toNext Step 3");
                        Console.WriteLine("p =");
                        List_.LogNode(p);
#endif
                        List_.leaveTestSynchronizedBlock();

                        List_.enterTestSynchronizedBlock();
                        // ReSharper disable once UnusedVariable
                        bool b1 = compareExchangeNodeLinkInPair(ref cursor.Next_,
                            (nodeLink)p, (nodeLink)next);
#if SynchronizedLfdll_Verbose
                        Console.WriteLine("Remove Step 4");
                        Console.WriteLine("compareExchangeNlIp(ref cursor.Next_, {0}, {1}) = {2}",
                            List_.nodeLinkDescription((nodeLink)p),
                            List_.nodeLinkDescription((nodeLink)next),
                            b1);
                        Console.WriteLine("cursor =");
                        List_.LogNode(cursor);
                        List_.LogState();
#endif
                        List_.leaveTestSynchronizedBlock();

                        continue;
                    }
                    cursor = next;
                    if (!d && next != List_.tailNode)
                        return true;
                }
            }
            private bool toPrev(ref node cursor)
            {
                while (true)
                {
                    if (cursor == List_.headNode)
                        return false;

                    Thread.MemoryBarrier();
                    List_.enterTestSynchronizedBlock();
                    node prev = cursor.Prev_.P;
#if SynchronizedLfdll_Verbose
                    Console.WriteLine("toPrev Step 0");
                    Console.WriteLine("prev =");
                    List_.LogNode(prev);
#endif
                    List_.leaveTestSynchronizedBlock();

                    Thread.MemoryBarrier();
                    List_.enterTestSynchronizedBlock();
                    bool b = prev.Next_.Link.Equals(new nodeLink(cursor, false));
#if SynchronizedLfdll_Verbose
                    Console.WriteLine("toPrev Step 1");
                    Console.WriteLine("b = {0}", b);
#endif
                    List_.leaveTestSynchronizedBlock();

                    if (b)
                    {

                        Thread.MemoryBarrier();
                        List_.enterTestSynchronizedBlock();
                        b &= !cursor.Next_.Link.D;
#if SynchronizedLfdll_Verbose
                        Console.WriteLine("toPrev Step 2");
                        Console.WriteLine("!cursor.Next_.Link.D = {0}", !cursor.Next_.Link.D);
#endif
                        List_.leaveTestSynchronizedBlock();

                    }
                    if (b)
                    {
                        cursor = prev;
                        if (prev != List_.headNode)
                            return true;
                    }
                    else
                    {
                        Thread.MemoryBarrier();
                        List_.enterTestSynchronizedBlock();
                        bool b1 = cursor.Next_.Link.D;
#if SynchronizedLfdll_Verbose
                        Console.WriteLine("toPrev Step 3");
                        Console.WriteLine("b1 = {0}", b1);
#endif
                        List_.leaveTestSynchronizedBlock();

                        if (b1)
                            toNext(ref cursor);
                        else
                            List_.correctPrev(prev, cursor);
                    }
                }
            }

            private INode insertBefore(T value, node cursor)
            {
                return insertBefore(value, cursor, new SpinWait());
            }
            private INode insertBefore(T value, node cursor, SpinWait spin)
            {
                if (cursor == List_.headNode)
                    return InsertAfter(value);
                node node = new node(List_);

                Thread.MemoryBarrier();
                List_.enterTestSynchronizedBlock();
                node prev = cursor.Prev_.P;
#if SynchronizedLfdll_Verbose
                Console.WriteLine("insertBefore Step 0");
                Console.WriteLine("prev =");
                List_.LogNode(prev);
#endif
                List_.leaveTestSynchronizedBlock();

                node next;
                while (true)
                {
                    while (true)
                    {

                        Thread.MemoryBarrier();
                        List_.enterTestSynchronizedBlock();
                        bool b = !cursor.Next_.Link.D;
#if SynchronizedLfdll_Verbose
                        Console.WriteLine("insertBefore Step 1");
                        Console.WriteLine("b = {0}", b);
#endif
                        List_.leaveTestSynchronizedBlock();

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
                        List_.enterTestSynchronizedBlock();
                        prev = cursor.Prev_.P;
#if SynchronizedLfdll_Verbose
                        Console.WriteLine("insertBefore Step 1.1");
                        Console.WriteLine("prev =");
                        List_.LogNode(prev);
#endif
                        List_.leaveTestSynchronizedBlock();
                        #endregion

                        prev = List_.correctPrev(prev, cursor);
                    }
                    next = cursor;
                    node.Prev_ = new nodeLink(prev, false);
                    node.Next_ = new valueNodeLinkPair(value,
                        new nodeLink(next, false));

                    List_.enterTestSynchronizedBlock();
                    bool b1 = compareExchangeNodeLinkInPair(ref prev.Next_,
                        new nodeLink(node, false),
                        new nodeLink(cursor, false));
#if SynchronizedLfdll_Verbose
                    Console.WriteLine("insertBefore Step 2");
                    Console.WriteLine("compareExchangeNlIp(ref prev.Next_, {0}, {1}) = {2}",
                        List_.nodeLinkDescription(new nodeLink(node, false)),
                        List_.nodeLinkDescription(new nodeLink(cursor, false)),
                        b1);
                    Console.WriteLine("prev =");
                    List_.LogNode(prev);
                    List_.LogState();
#endif
                    List_.leaveTestSynchronizedBlock();

                    if (b1)
                        break;

                    prev = List_.correctPrev(prev, cursor);
                    spin.SpinOnce();
                }

                List_.correctPrev(prev, next);
                return node;
            }

            private INode insertAfter(T value, node cursor)
            {
                SpinWait spin = new SpinWait();

                if (cursor == List_.tailNode)
                    return insertBefore(value, cursor, spin);
                node node = new node(List_);
                node prev = cursor;
                node next;
                while (true)
                {

                    Thread.MemoryBarrier();
                    List_.enterTestSynchronizedBlock();
                    next = prev.Next_.Link.P;
#if SynchronizedLfdll_Verbose
                    Console.WriteLine("insertAfter Step 0");
                    Console.WriteLine("next =");
                    List_.LogNode(next);
#endif
                    List_.leaveTestSynchronizedBlock();

                    node.Prev_ = new nodeLink(prev, false);
                    node.Next_ = new valueNodeLinkPair(value,
                        new nodeLink(next, false));

                    List_.enterTestSynchronizedBlock();
                    bool b1 = compareExchangeNodeLinkInPair(ref cursor.Next_,
                        new nodeLink(node, false),
                        new nodeLink(next, false));
#if SynchronizedLfdll_Verbose
                    Console.WriteLine("insertAfter Step 1");
                    Console.WriteLine("compareExchangeNlIp(ref cursor.Next_, {0}, {1}) = {2}",
                        List_.nodeLinkDescription(new nodeLink(node, false)),
                        List_.nodeLinkDescription(new nodeLink(next, false)),
                        b1);
                    Console.WriteLine("cursor =");
                    List_.LogNode(cursor);
                    List_.LogState();
#endif
                    List_.leaveTestSynchronizedBlock();

                    if (b1)
                        break;

                    /* Not necessary because of preceding compareExchange. */
                    // Thread.MemoryBarrier();

                    List_.enterTestSynchronizedBlock();
                    bool b = prev.Next_.Link.D;
#if SynchronizedLfdll_Verbose
                    Console.WriteLine("insertAfter Step 2");
                    Console.WriteLine("b = {0}", b);
                    List_.LogNode(next);
#endif
                    List_.leaveTestSynchronizedBlock();

                    if (b)
                        return insertBefore(value, cursor, spin);
                    spin.SpinOnce();
                }
                List_.correctPrev(prev, next);
                return node;
            }

            public T CompareExchangeValue(T newValue, T comparand)
            {
                valueNodeLinkPair currentPair;
                Thread.MemoryBarrier();
                while (true)
                {
                    currentPair = Next_;
                    if (!ReferenceEquals(currentPair.Value, comparand))
                        return currentPair.Value;
                    if (ReferenceEquals(
                            Interlocked.CompareExchange(
                                ref Next_,
                                new valueNodeLinkPair(
                                    newValue, currentPair.Link),
                                currentPair),
                            currentPair))
                    {
                        break;
                    }
                }
                return currentPair.Value;
            }
        }

        private class nodeLink
        {
            public node P { get; private set; }
            public bool D { get; private set; }

            public nodeLink(node p, bool d)
            {
                P = p;
                D = d;
            }

            public override bool Equals(object obj)
            {
                if (!(obj is nodeLink))
                    return false;

                var other = (nodeLink)obj;
                return this.D == other.D && this.P == other.P;
            }

            public bool Equals(nodeLink other)
            {
                if (other == null)
                    return false;

                return this.D == other.D && this.P == other.P;
            }

            public override int GetHashCode()
            {
                int hash = 17;
                hash = hash * 486187739 + P.GetHashCode();
                hash = hash * 486187739 + D.GetHashCode();
                return hash;
            }

            public static explicit operator nodeLink(node node)
            {
                return new nodeLink(node, false);
            }

            public static explicit operator node(nodeLink link)
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
        }

        private class valueNodeLinkPair
        {
            public T Value { get; private set; }
            public nodeLink Link { get; private set; }

            public valueNodeLinkPair(T value, nodeLink link)
            {
                Value = value;
                Link = link;
            }
        }
    }
}
