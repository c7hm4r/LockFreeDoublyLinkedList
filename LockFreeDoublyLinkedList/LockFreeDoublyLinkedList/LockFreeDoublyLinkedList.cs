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
    public class LockFreeDoublyLinkedList<T> : IEnumerable<T>
        where T : class
    {
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

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public INode Head { get { return headNode; } }
        public INode Tail { get { return tailNode; } }

        public INode PushLeft(T value)
        {
            SpinWait spin = new SpinWait();
            node node = new node(value, this);
            node prev = headNode;

            Thread.MemoryBarrier();
            enterTestSynchronizedBlock();
            node next = prev.Next_.P;
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
                node.Next_ = new nodeLink(next, false);

                enterTestSynchronizedBlock();
                bool b = compareExchangeNodeLink(ref prev.Next_,
                    new nodeLink(node, false),
                    new nodeLink(next, false));
#if SynchronizedLfdll_Verbose
                Console.WriteLine("PushLeft {0} Step 1", value);
                Console.WriteLine("compareExchangeNl(ref prev.Next_, {0}, {1}) = {2}",
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
                next = prev.Next_.P;
#if SynchronizedLfdll_Verbose
                Console.WriteLine("PushLeft {0} Step 2", value);
                Console.WriteLine("next =");
                LogNode(next);
#endif
                leaveTestSynchronizedBlock();

                spin.SpinOnce();
            }
            pushEnd(node, next, spin);
            return node;
        }
        public INode PushRight(T value)
        {
            SpinWait spin = new SpinWait();
            node node = new node(value, this);
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
                node.Next_ = new nodeLink(next, false);

                enterTestSynchronizedBlock();
                bool b = compareExchangeNodeLink(ref prev.Next_,
                    new nodeLink(node, false),
                    new nodeLink(next, false));
#if SynchronizedLfdll_Verbose
                Console.WriteLine("PushRight {0} Step 1", value);
                Console.WriteLine("compareExchangeNl(ref prev.Next_, {0}, {1}) = {2}",
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
            return node;
        }

#if PopLeft
        public Tuple<T> PopLeft()
        {
            SpinWait spin = new SpinWait();
            node prev = headNode;
            while (true)
            {
                Thread.MemoryBarrier();
                enterTestSynchronizedBlock();
                node node = prev.Next_.P;
#if SynchronizedLfdll_Verbose
                Console.WriteLine("PopLeft Step 0");
                Console.WriteLine("node = ");
                LogNode(node);
#endif
                leaveTestSynchronizedBlock();

                if (node == tailNode)
                    return null;

                Thread.MemoryBarrier();
                enterTestSynchronizedBlock();
                nodeLink next = node.Next_;
#if SynchronizedLfdll_Verbose
                Console.WriteLine("PopLeft Step 1");
                Console.WriteLine("next = {0}", nodeLinkDescription(next));
#endif
                leaveTestSynchronizedBlock();

                if (next.D)
                {
                    setMark(ref node.Prev_);

                    enterTestSynchronizedBlock();
                    bool b1 = compareExchangeNodeLink(ref prev.Next_,
                        new nodeLink(next.P, false),
                        (nodeLink)node);
#if SynchronizedLfdll_Verbose
                    Console.WriteLine("PopLeft Step 2");
                    Console.WriteLine("compareExchangeNl(ref prev.Next_, {0}, {1}) = {2}",
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
                bool b = compareExchangeNodeLink(ref node.Next_,
                    new nodeLink(next.P, true), next);
#if SynchronizedLfdll_Verbose
                Console.WriteLine("PopLeft Step 3");
                Console.WriteLine("compareExchangeNl(ref node.Next_, {0}, {1}) = {2}",
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
                    return new Tuple<T>(node.Value_);
                }

                spin.SpinOnce();
            }
        }
#endif
        public Tuple<T> PopRight()
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
                bool b = !node.Next_.Equals(new nodeLink(next, false));
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
                    return null;

                enterTestSynchronizedBlock();
                bool b1 = compareExchangeNodeLink(ref node.Next_,
                    new nodeLink(next, true),
                    new nodeLink(next, false));
#if SynchronizedLfdll_Verbose
                Console.WriteLine("PopRight Step 2");
                Console.WriteLine("compareExchangeNl(ref node.Next_, {0}, {1}) = {2}",
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
                    return new Tuple<T>(node.Value_);
                }

                spin.SpinOnce();
            }
        }

        public LockFreeDoublyLinkedList()
        {
            headNode = new node(this);
            tailNode = new node(this);

            headNode.Prev_ = new nodeLink(null, false);
            headNode.Next_ = new nodeLink(tailNode, false);
            tailNode.Prev_ = new nodeLink(headNode, false);
            tailNode.Next_ = new nodeLink(null, false);
        }

        public interface INode
        {
#if DEBUG
            long Id { get; }

#endif
            T Value { get; set; }

            INode Next { get; }
            INode Prev { get; }

            bool Removed { get; }

            INode InsertBefore(T newValue);
            INode InsertAfter(T newValue);

            bool Remove();
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
                    b |= !node.Next_.Equals(new nodeLink(next, false));
#if SynchronizedLfdll_Verbose
                    Console.WriteLine("pushEnd Step 1");
                    Console.WriteLine("!node.Next_.Equals(new nodeLink(next, false) = {0}",
                        !node.Next_.Equals(new nodeLink(next, false)));
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
                nodeLink prev2 = prev.Next_;
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
                        bool b1 = compareExchangeNodeLink(ref lastLink.Next_,
                            (nodeLink)prev2.P, (nodeLink)prev);
#if SynchronizedLfdll_Verbose
                        Console.WriteLine("CorrectPrev Step 2");
                        Console.WriteLine("compareExchangeNl(lastLink.Next_, {0}, {1}) = {2}",
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
                current = current.Next_.P;
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
                    + nodeLinkDescription(node.Next_));
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

        private void logNodeLink(nodeLink link)
        {
            Console.WriteLine(nodeLinkDescription(link));
        }
#endif
#endif // SynchronizedLfdll

        private class node : INode
        {
            // ReSharper disable once InconsistentNaming
            public nodeLink Next_;
            // ReSharper disable once InconsistentNaming
            public nodeLink Prev_;
            // ReSharper disable once InconsistentNaming
            public T Value_;
            public readonly LockFreeDoublyLinkedList<T> List;

#if DEBUG
            public long Id { get; private set; }

#endif
            public T Value
            {
                get
                {
                    if (this == List.headNode || this == List.tailNode)
                        throw new InvalidOperationException();
                    /* At the commented out code it is assumed
                     * that Value_ is not allowed to be readout
                     * once the node was deleted.
                     * However, this behaviour does not seem useful. */
                    //T val = this.newValue;
                    //if (this.Next_.D)
                    //    return default(T);

                    Thread.MemoryBarrier();
                    return this.Value_;
                }
                set
                {
                    if (this == List.headNode || this == List.tailNode)
                        throw new InvalidOperationException();
                    Thread.MemoryBarrier();
                    this.Value_ = value;
                }
            }

            public INode Next
            {
                get
                {
                    node cursor = this;
                    if (toNext(ref cursor))
                        return cursor;
                    return null;
                }
            }
            public INode Prev
            {
                get
                {
                    node cursor = this;
                    if (toPrev(ref cursor))
                        return cursor;
                    return null;
                }
            }

            public bool Removed
            {
                get
                {
                    Thread.MemoryBarrier();
                    bool result = this.Next_.D;
                    return result;
                }
            }

            public INode InsertBefore(T newValue)
            {
                return insertBefore(newValue, this);
            }
            public INode InsertAfter(T newValue)
            {
                return insertAfter(newValue, this);
            }

            public bool Remove() // out T lastValue
            {
                if (this == List.headNode || this == List.tailNode)
                {
                    // lastValue = default(T);
                    return false;
                }
                while (true)
                {

                    Thread.MemoryBarrier();
                    List.enterTestSynchronizedBlock();
                    nodeLink next = this.Next_;
#if SynchronizedLfdll_Verbose
                    Console.WriteLine("Remove Step 0");
                    Console.WriteLine("next = {0}", List.nodeLinkDescription(next));
#endif
                    List.leaveTestSynchronizedBlock();

                    if (next.D)
                    {
                        // lastValue = default(T);
                        return false;
                    }

                    List.enterTestSynchronizedBlock();
                    bool b = compareExchangeNodeLink(ref this.Next_,
                        new nodeLink(next.P, true), next);
#if SynchronizedLfdll_Verbose
                    Console.WriteLine("Remove Step 1");
                    Console.WriteLine("compareExchangeNl(ref this.Next_, {0}, {1}) = {2}",
                        List.nodeLinkDescription(new nodeLink(next.P, true)),
                        List.nodeLinkDescription(next),
                        b);
                    Console.WriteLine("this =");
                    List.LogNode(this);
                    List.LogState();
#endif
                    List.leaveTestSynchronizedBlock();

                    if (b)
                    {
                        nodeLink prev;
                        while (true)
                        {

                            /* Not necessary because of preceding compareExchange. */
                            // Thread.MemoryBarrier();

                            List.enterTestSynchronizedBlock();
                            prev = this.Prev_;
#if SynchronizedLfdll_Verbose
                            Console.WriteLine("Remove Step 2");
                            Console.WriteLine("prev = {0}", List.nodeLinkDescription(this.Prev_));
#endif
                            List.leaveTestSynchronizedBlock();

                            if (prev.D)
                                break;

                            List.enterTestSynchronizedBlock();
                            bool b1 = compareExchangeNodeLink(ref this.Prev_,
                                new nodeLink(prev.P, true), prev);
#if SynchronizedLfdll_Verbose
                            Console.WriteLine("Remove Step 3");
                            Console.WriteLine("compareExchangeNl(ref this.Prev_, {0}, {1}) = {2}",
                                List.nodeLinkDescription(new nodeLink(prev.P, true)),
                                List.nodeLinkDescription(prev),
                                b1);
                            Console.WriteLine("this =");
                            List.LogNode(this);
                            List.LogState();
#endif
                            List.leaveTestSynchronizedBlock();

                            if (b1)
                                break;
                        }
                        List.correctPrev(prev.P, next.P);
                        //lastValue = this.newValue;
                        return true;
                    }
                }
            }

            public node(T value, LockFreeDoublyLinkedList<T> list)
            {
                this.Value_ = value;
                this.List = list;
#if DEBUG

                Id = Interlocked.Increment(ref nextId) - 1;
#endif
            }

            public node(LockFreeDoublyLinkedList<T> list)
                : this(default(T), list)
            {
                this.List = list;
            }

#if DEBUG
            // ReSharper disable once StaticFieldInGenericType
            private static long nextId = 0;

#endif
            private bool toNext(ref node cursor)
            {
                while (true)
                {
                    if (cursor == List.tailNode)
                        return false;

                    Thread.MemoryBarrier();
                    List.enterTestSynchronizedBlock();
                    node next = cursor.Next_.P;
#if SynchronizedLfdll_Verbose
                    Console.WriteLine("toNext Step 0");
                    Console.WriteLine("next =");
                    List.LogNode(next);
#endif
                    List.leaveTestSynchronizedBlock();

                    Thread.MemoryBarrier();
                    List.enterTestSynchronizedBlock();
                    bool d = next.Next_.D;
#if SynchronizedLfdll_Verbose
                    Console.WriteLine("toNext Step 1");
                    Console.WriteLine("d = {0}", d);
#endif
                    List.leaveTestSynchronizedBlock();

                    bool b = d;
                    if (b)
                    {

                        Thread.MemoryBarrier();
                        List.enterTestSynchronizedBlock();
                        b &= !cursor.Next_.Equals(new nodeLink(next, true));
#if SynchronizedLfdll_Verbose
                        Console.WriteLine("toNext Step 2");
                        Console.WriteLine("!cursor.Next_.Equals(new nodeLink(next, true)) = {0}",
                            !cursor.Next_.Equals(new nodeLink(next, true)));
#endif
                        List.leaveTestSynchronizedBlock();

                    }
                    if (b)
                    {
                        List.setMark(ref next.Prev_);

                        Thread.MemoryBarrier();
                        List.enterTestSynchronizedBlock();
                        node p = next.Next_.P;
#if SynchronizedLfdll_Verbose
                        Console.WriteLine("toNext Step 3");
                        Console.WriteLine("p =");
                        List.LogNode(p);
#endif
                        List.leaveTestSynchronizedBlock();

                        List.enterTestSynchronizedBlock();
                        bool b1 = compareExchangeNodeLink(ref cursor.Next_,
                            (nodeLink)p, (nodeLink)next);
#if SynchronizedLfdll_Verbose
                        Console.WriteLine("Remove Step 4");
                        Console.WriteLine("compareExchangeNl(ref cursor.Next_, {0}, {1}) = {2}",
                            List.nodeLinkDescription((nodeLink)p),
                            List.nodeLinkDescription((nodeLink)next),
                            b1);
                        Console.WriteLine("cursor =");
                        List.LogNode(cursor);
                        List.LogState();
#endif
                        List.leaveTestSynchronizedBlock();

                        continue;
                    }
                    cursor = next;
                    if (!d && next != List.tailNode)
                        return true;
                }
            }
            private bool toPrev(ref node cursor)
            {
                while (true)
                {
                    if (cursor == List.headNode)
                        return false;

                    Thread.MemoryBarrier();
                    List.enterTestSynchronizedBlock();
                    node prev = cursor.Prev_.P;
#if SynchronizedLfdll_Verbose
                    Console.WriteLine("toPrev Step 0");
                    Console.WriteLine("prev =");
                    List.LogNode(prev);
#endif
                    List.leaveTestSynchronizedBlock();

                    Thread.MemoryBarrier();
                    List.enterTestSynchronizedBlock();
                    bool b = prev.Next_.Equals(new nodeLink(cursor, false));
#if SynchronizedLfdll_Verbose
                    Console.WriteLine("toPrev Step 1");
                    Console.WriteLine("b = {0}", b);
#endif
                    List.leaveTestSynchronizedBlock();

                    if (b)
                    {

                        Thread.MemoryBarrier();
                        List.enterTestSynchronizedBlock();
                        b &= !cursor.Next_.D;
#if SynchronizedLfdll_Verbose
                        Console.WriteLine("toPrev Step 2");
                        Console.WriteLine("!cursor.Next_.D = {0}", !cursor.Next_.D);
#endif
                        List.leaveTestSynchronizedBlock();

                    }
                    if (b)
                    {
                        cursor = prev;
                        if (prev != List.headNode)
                            return true;
                    }
                    else
                    {
                        Thread.MemoryBarrier();
                        List.enterTestSynchronizedBlock();
                        bool b1 = cursor.Next_.D;
#if SynchronizedLfdll_Verbose
                        Console.WriteLine("toPrev Step 3");
                        Console.WriteLine("b1 = {0}", b1);
#endif
                        List.leaveTestSynchronizedBlock();

                        if (b1)
                            toNext(ref cursor);
                        else
                            List.correctPrev(prev, cursor);
                    }
                }
            }

            private INode insertBefore(T value, node cursor)
            {
                return insertBefore(value, cursor, new SpinWait());
            }
            private INode insertBefore(T value, node cursor, SpinWait spin)
            {
                if (cursor == List.headNode)
                    return InsertAfter(value);
                node node = new node(value, List);

                Thread.MemoryBarrier();
                List.enterTestSynchronizedBlock();
                node prev = cursor.Prev_.P;
#if SynchronizedLfdll_Verbose
                Console.WriteLine("insertBefore Step 0");
                Console.WriteLine("prev =");
                List.LogNode(prev);
#endif
                List.leaveTestSynchronizedBlock();

                node next;
                while (true)
                {
                    while (true)
                    {

                        Thread.MemoryBarrier();
                        List.enterTestSynchronizedBlock();
                        bool b = !cursor.Next_.D;
#if SynchronizedLfdll_Verbose
                        Console.WriteLine("insertBefore Step 1");
                        Console.WriteLine("b = {0}", b);
#endif
                        List.leaveTestSynchronizedBlock();

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
                         * especially when prev == List.tailNode. */

                        toNext(ref cursor);

                        #region Bugfix 1
                        /* Ascertain a new predecessor of cursor. */
                        Thread.MemoryBarrier();
                        List.enterTestSynchronizedBlock();
                        prev = cursor.Prev_.P;
#if SynchronizedLfdll_Verbose
                        Console.WriteLine("insertBefore Step 1.1");
                        Console.WriteLine("prev =");
                        List.LogNode(prev);
#endif
                        List.leaveTestSynchronizedBlock();
                        #endregion

                        prev = List.correctPrev(prev, cursor);
                    }
                    next = cursor;
                    node.Prev_ = new nodeLink(prev, false);
                    node.Next_ = new nodeLink(next, false);

                    List.enterTestSynchronizedBlock();
                    bool b1 = compareExchangeNodeLink(ref prev.Next_,
                        new nodeLink(node, false),
                        new nodeLink(cursor, false));
#if SynchronizedLfdll_Verbose
                    Console.WriteLine("insertBefore Step 2");
                    Console.WriteLine("compareExchangeNl(ref prev.Next_, {0}, {1}) = {2}",
                        List.nodeLinkDescription(new nodeLink(node, false)),
                        List.nodeLinkDescription(new nodeLink(cursor, false)),
                        b1);
                    Console.WriteLine("prev =");
                    List.LogNode(prev);
                    List.LogState();
#endif
                    List.leaveTestSynchronizedBlock();

                    if (b1)
                        break;

                    prev = List.correctPrev(prev, cursor);
                    spin.SpinOnce();
                }

                List.correctPrev(prev, next);
                return node;
            }

            private INode insertAfter(T value, node cursor)
            {
                SpinWait spin = new SpinWait();

                if (cursor == List.tailNode)
                    return insertBefore(value, cursor, spin);
                node node = new node(value, List);
                node prev = cursor;
                node next;
                while (true)
                {

                    Thread.MemoryBarrier();
                    List.enterTestSynchronizedBlock();
                    next = prev.Next_.P;
#if SynchronizedLfdll_Verbose
                    Console.WriteLine("insertAfter Step 0");
                    Console.WriteLine("next =");
                    List.LogNode(next);
#endif
                    List.leaveTestSynchronizedBlock();

                    node.Prev_ = new nodeLink(prev, false);
                    node.Next_ = new nodeLink(next, false);

                    List.enterTestSynchronizedBlock();
                    bool b1 = compareExchangeNodeLink(ref cursor.Next_,
                        new nodeLink(node, false),
                        new nodeLink(next, false));
#if SynchronizedLfdll_Verbose
                    Console.WriteLine("insertAfter Step 1");
                    Console.WriteLine("compareExchangeNl(ref cursor.Next_, {0}, {1}) = {2}",
                        List.nodeLinkDescription(new nodeLink(node, false)),
                        List.nodeLinkDescription(new nodeLink(next, false)),
                        b1);
                    Console.WriteLine("cursor =");
                    List.LogNode(cursor);
                    List.LogState();
#endif
                    List.leaveTestSynchronizedBlock();

                    if (b1)
                        break;

                    /* Not necessary because of preceding compareExchange. */
                    // Thread.MemoryBarrier();

                    List.enterTestSynchronizedBlock();
                    bool b = prev.Next_.D;
#if SynchronizedLfdll_Verbose
                    Console.WriteLine("insertAfter Step 2");
                    Console.WriteLine("b = {0}", b);
                    List.LogNode(next);
#endif
                    List.leaveTestSynchronizedBlock();

                    if (b)
                        return insertBefore(value, cursor, spin);
                    spin.SpinOnce();
                }
                List.correctPrev(prev, next);
                return cursor;
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
    }
}
