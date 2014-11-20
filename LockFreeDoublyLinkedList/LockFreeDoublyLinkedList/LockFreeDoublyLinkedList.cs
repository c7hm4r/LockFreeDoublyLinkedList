//#define SynchronizedLfdll
//#define Verbose

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
    public interface ILockFreeDoublyLinkedList<T> : IEnumerable<T>
        where T : class
    {
#if SynchronizedLfdll
        ThreadLocal<AutoResetEvent> NextStepWaitHandle { get; }
        AutoResetEvent StepCompletedWaitHandle { get; }
        Counter StepCounter { get; }
#if SynchronizedLfdll_Verbose
        void LogState();
        void LogNode(LockFreeDoublyLinkedList<T>.INode node);
#endif
#endif // SynchronizedLfdll

        LockFreeDoublyLinkedList<T>.INode Head { get; }
        LockFreeDoublyLinkedList<T>.INode Tail { get; }

        LockFreeDoublyLinkedList<T>.INode PushLeft(T value);
        LockFreeDoublyLinkedList<T>.INode PushRight(T value);

#if PopLeft
        Tuple<T> PopLeft();
#endif
        Tuple<T> PopRight();
    }

    public static class LockFreeDoublyLinkedList<T>
        where T : class
    {
        public static ILockFreeDoublyLinkedList<T> Create()
        {
            return new lfdll();
        }

        private static bool compareExchangeNodeLink(ref nodeLink location,
            nodeLink value, nodeLink comparandByValue)
        {
            return ThreadingAdditions
                .ConditionalCompareExchange<nodeLink>(ref location,
                value, original => original.Equals(comparandByValue));
        }

        private class lfdll : ILockFreeDoublyLinkedList<T>
        {
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
                node current = HeadNode;
                while (current != null)
                {
                    LogNode(current);
                    current = current.Next_.P;
                }
            }

            public string NodeLinkDescription(nodeLink link)
            {
                return "(" + nodeName(link.P)
                    + ", " + link.D + ")";
            }

            public void LogNodeLink(nodeLink link)
            {
                Console.WriteLine(NodeLinkDescription(link));
            }

            public void LogNode(LockFreeDoublyLinkedList<T>.INode inode)
            {
                node node = (node)inode;
                Console.WriteLine(nodeName(node));
                if (node != null)
                {
                    Console.WriteLine("    .Prev_ = "
                        + NodeLinkDescription(node.Prev_));
                    Console.WriteLine("    .Next_ = "
                        + NodeLinkDescription(node.Next_));
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
                if (node == HeadNode)
                    return "HeadNode";
                if (node == TailNode)
                    return "TailNode";
                return "Node " + node.Value;
            }
#endif
#endif // SynchronizedLfdll

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

            public INode Head { get { return HeadNode; } }
            public INode Tail { get { return TailNode; } }

            public INode PushLeft(T value)
            {
                SpinWait spin = new SpinWait();
                node node = new node(value, this);
                node prev = HeadNode;

                Thread.MemoryBarrier();
                EnterTestSynchronizedBlock();
                node next = prev.Next_.P;
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
                    node.Prev_ = new nodeLink(prev, false);
                    node.Next_ = new nodeLink(next, false);

                    EnterTestSynchronizedBlock();
                    bool b = compareExchangeNodeLink(ref prev.Next_,
                        new nodeLink(node, false),
                        new nodeLink(next, false));
#if SynchronizedLfdll_Verbose
                    Console.WriteLine("PushLeft {0} Step 1", value);
                    Console.WriteLine("compareExchangeNl(ref prev.Next_, {0}, {1}) = {2}",
                        NodeLinkDescription(new nodeLink(node, false)),
                        NodeLinkDescription(new nodeLink(next, false)),
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
                    next = prev.Next_.P;
#if SynchronizedLfdll_Verbose
                    Console.WriteLine("PushLeft {0} Step 2", value);
                    Console.WriteLine("next =");
                    LogNode(next);
#endif
                    LeaveTestSynchronizedBlock();

                    spin.SpinOnce();
                }
                pushEnd(node, next, spin);
                return node;
            }
            public INode PushRight(T value)
            {
                SpinWait spin = new SpinWait();
                node node = new node(value, this);
                node next = TailNode;

                Thread.MemoryBarrier();
                EnterTestSynchronizedBlock();
                node prev = next.Prev_.P;
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
                    node.Prev_ = new nodeLink(prev, false);
                    node.Next_ = new nodeLink(next, false);

                    EnterTestSynchronizedBlock();
                    bool b = compareExchangeNodeLink(ref prev.Next_,
                        new nodeLink(node, false),
                        new nodeLink(next, false));
#if SynchronizedLfdll_Verbose
                    Console.WriteLine("PushRight {0} Step 1", value);
                    Console.WriteLine("compareExchangeNl(ref prev.Next_, {0}, {1}) = {2}",
                        NodeLinkDescription(new nodeLink(node, false)),
                        NodeLinkDescription(new nodeLink(next, false)),
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
                return node;
            }

#if PopLeft
            public Tuple<T> PopLeft()
            {
                SpinWait spin = new SpinWait();
                node prev = HeadNode;
                while (true)
                {
                    Thread.MemoryBarrier();
                    EnterTestSynchronizedBlock();
                    node node = prev.Next_.P;
#if SynchronizedLfdll_Verbose
                    Console.WriteLine("PopLeft Step 0");
                    Console.WriteLine("node = ");
                    LogNode(node);
#endif
                    LeaveTestSynchronizedBlock();

                    if (node == TailNode)
                        return null;

                    Thread.MemoryBarrier();
                    EnterTestSynchronizedBlock();
                    nodeLink next = node.Next_;
#if SynchronizedLfdll_Verbose
                    Console.WriteLine("PopLeft Step 1");
                    Console.WriteLine("next = {0}", NodeLinkDescription(next));
#endif
                    LeaveTestSynchronizedBlock();

                    if (next.D)
                    {
                        SetMark(ref node.Prev_);

                        EnterTestSynchronizedBlock();
                        bool b1 = compareExchangeNodeLink(ref prev.Next_,
                            new nodeLink(next.P, false),
                            (nodeLink)node);
#if SynchronizedLfdll_Verbose
                        Console.WriteLine("PopLeft Step 2");
                        Console.WriteLine("compareExchangeNl(ref prev.Next_, {0}, {1}) = {2}",
                            NodeLinkDescription(new nodeLink(next.P, false)),
                            NodeLinkDescription((nodeLink)node),
                            b1);
                        Console.WriteLine("prev = ");
                        LogNode(prev);
                        LogState();
#endif
                        LeaveTestSynchronizedBlock();

                        continue;
                    }

                    EnterTestSynchronizedBlock();
                    bool b = compareExchangeNodeLink(ref node.Next_,
                        new nodeLink(next.P, true), next);
#if SynchronizedLfdll_Verbose
                    Console.WriteLine("PopLeft Step 3");
                    Console.WriteLine("compareExchangeNl(ref node.Next_, {0}, {1}) = {2}",
                        NodeLinkDescription(new nodeLink(next.P, true)),
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
                        return new Tuple<T>(node.Value_);
                    }

                    spin.SpinOnce();
                }
            }
#endif
            public Tuple<T> PopRight()
            {
                SpinWait spin = new SpinWait();
                node next = TailNode;

                Thread.MemoryBarrier();
                EnterTestSynchronizedBlock();
                node node = next.Prev_.P;
#if SynchronizedLfdll_Verbose
                Console.WriteLine("PopRight Step 0");
                Console.WriteLine("node  =");
                LogNode(node);
#endif
                LeaveTestSynchronizedBlock();

                while (true)
                {

                    Thread.MemoryBarrier();
                    EnterTestSynchronizedBlock();
                    bool b = !node.Next_.Equals(new nodeLink(next, false));
#if SynchronizedLfdll_Verbose
                    Console.WriteLine("PopRight Step 1");
                    Console.WriteLine("b = {0}", b);
#endif
                    LeaveTestSynchronizedBlock();

                    if (b)
                    {
                        node = CorrectPrev(node, next);
                        continue;
                    }

                    if (node == HeadNode)
                        return null;

                    EnterTestSynchronizedBlock();
                    bool b1 = compareExchangeNodeLink(ref node.Next_,
                        new nodeLink(next, true),
                        new nodeLink(next, false));
#if SynchronizedLfdll_Verbose
                    Console.WriteLine("PopRight Step 2");
                    Console.WriteLine("compareExchangeNl(ref node.Next_, {0}, {1}) = {2}",
                        NodeLinkDescription(new nodeLink(next, true)),
                        NodeLinkDescription(new nodeLink(next, false)),
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
                        node prev = node.Prev_.P;
#if SynchronizedLfdll_Verbose
                        Console.WriteLine("PopRight Step 3");
                        Console.WriteLine("prev =");
                        LogNode(prev);
#endif
                        LeaveTestSynchronizedBlock();

                        CorrectPrev(prev, next);
                        return new Tuple<T>(node.Value_);
                    }

                    spin.SpinOnce();
                }
            }

            public node HeadNode { get; private set; }
            public node TailNode { get; private set; }

            public void SetMark(ref nodeLink link)
            {
                Thread.MemoryBarrier();
                EnterTestSynchronizedBlock();
                nodeLink node = link;
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
                    nodeLink prevalent = Interlocked.CompareExchange<nodeLink>(
                        ref link, new nodeLink(node.P, true), node);
#if SynchronizedLfdll_Verbose
                    Console.WriteLine("SetMark Step 1");
                    Console.WriteLine("compareExchange(link, {0}, {1}) = {2}",
                        NodeLinkDescription(new nodeLink(node.P, true)),
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

            public node CorrectPrev(node prev, node node)
            {
                return CorrectPrev(prev, node, new SpinWait());
            }

            public node CorrectPrev(node prev, node node, SpinWait spin)
            {
                node lastLink = null;
                while (true)
                {

                    Thread.MemoryBarrier();
                    EnterTestSynchronizedBlock();
                    nodeLink link1 = node.Prev_;
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
                    nodeLink prev2 = prev.Next_;
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
                            bool b1 = compareExchangeNodeLink(ref lastLink.Next_,
                                (nodeLink)prev2.P, (nodeLink)prev);
#if SynchronizedLfdll_Verbose
                            Console.WriteLine("CorrectPrev Step 2");
                            Console.WriteLine("compareExchangeNl(lastLink.Next_, {0}, {1}) = {2}",
                                NodeLinkDescription((nodeLink)prev2.P),
                                NodeLinkDescription((nodeLink)prev),
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
                        new nodeLink(prev, false), link1);
#if SynchronizedLfdll_Verbose
                    Console.WriteLine("CorrectPrev Step 4");
                    Console.WriteLine("compareExchangeNl(node.Prev_, {0}, {1}) = {2}",
                        NodeLinkDescription(new nodeLink(prev, false)),
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

            public lfdll()
            {
                HeadNode = new node(this);
                TailNode = new node(this);

                HeadNode.Prev_ = new nodeLink(null, false);
                HeadNode.Next_ = new nodeLink(TailNode, false);
                TailNode.Prev_ = new nodeLink(HeadNode, false);
                TailNode.Next_ = new nodeLink(null, false);
            }

            private void pushEnd(node node, node next, SpinWait spin)
            {
                while (true)
                {

                    Thread.MemoryBarrier();
                    EnterTestSynchronizedBlock();
                    nodeLink link1 = next.Prev_;
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
                        b |= !node.Next_.Equals(new nodeLink(next, false));
#if SynchronizedLfdll_Verbose
                        Console.WriteLine("pushEnd Step 1");
                        Console.WriteLine("!node.Next_.Equals(new nodeLink(next, false) = {0}",
                            !node.Next_.Equals(new nodeLink(next, false)));
#endif
                        LeaveTestSynchronizedBlock();
                    }
                    if (b)
                        break;

                    EnterTestSynchronizedBlock();
                    bool b1 = compareExchangeNodeLink(ref next.Prev_,
                        new nodeLink(node, false), link1);
#if SynchronizedLfdll_Verbose
                    Console.WriteLine("pushEnd Step 2");
                    Console.WriteLine("compareExchangeNl(next.Prev_, {0}, {1}) = {2}",
                        NodeLinkDescription(new nodeLink(node, false)),
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
        }

        private class node : INode
        {
            // ReSharper disable once InconsistentNaming
            public nodeLink Next_;
            // ReSharper disable once InconsistentNaming
            public nodeLink Prev_;
            // ReSharper disable once InconsistentNaming
            public T Value_;
            public readonly lfdll List;

#if DEBUG
            public long Id { get; private set; }

#endif
            public T Value
            {
                get
                {
                    if (this == List.HeadNode || this == List.TailNode)
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
                    if (this == List.HeadNode || this == List.TailNode)
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
                if (this == List.HeadNode || this == List.TailNode)
                {
                    // lastValue = default(T);
                    return false;
                }
                while (true)
                {

                    Thread.MemoryBarrier();
                    List.EnterTestSynchronizedBlock();
                    nodeLink next = this.Next_;
#if SynchronizedLfdll_Verbose
                    Console.WriteLine("Remove Step 0");
                    Console.WriteLine("next = {0}", List.NodeLinkDescription(next));
#endif
                    List.LeaveTestSynchronizedBlock();

                    if (next.D)
                    {
                        // lastValue = default(T);
                        return false;
                    }

                    List.EnterTestSynchronizedBlock();
                    bool b = compareExchangeNodeLink(ref this.Next_,
                        new nodeLink(next.P, true), next);
#if SynchronizedLfdll_Verbose
                    Console.WriteLine("Remove Step 1");
                    Console.WriteLine("compareExchangeNl(ref this.Next_, {0}, {1}) = {2}",
                        List.NodeLinkDescription(new nodeLink(next.P, true)),
                        List.NodeLinkDescription(next),
                        b);
                    Console.WriteLine("this =");
                    List.LogNode(this);
                    List.LogState();
#endif
                    List.LeaveTestSynchronizedBlock();

                    if (b)
                    {
                        nodeLink prev;
                        while (true)
                        {

                            /* Not necessary because of preceding compareExchange. */
                            // Thread.MemoryBarrier();

                            List.EnterTestSynchronizedBlock();
                            prev = this.Prev_;
#if SynchronizedLfdll_Verbose
                            Console.WriteLine("Remove Step 2");
                            Console.WriteLine("prev = {0}", List.NodeLinkDescription(this.Prev_));
#endif
                            List.LeaveTestSynchronizedBlock();

                            if (prev.D)
                                break;

                            List.EnterTestSynchronizedBlock();
                            bool b1 = compareExchangeNodeLink(ref this.Prev_,
                                new nodeLink(prev.P, true), prev);
#if SynchronizedLfdll_Verbose
                            Console.WriteLine("Remove Step 3");
                            Console.WriteLine("compareExchangeNl(ref this.Prev_, {0}, {1}) = {2}",
                                List.NodeLinkDescription(new nodeLink(prev.P, true)),
                                List.NodeLinkDescription(prev),
                                b1);
                            Console.WriteLine("this =");
                            List.LogNode(this);
                            List.LogState();
#endif
                            List.LeaveTestSynchronizedBlock();

                            if (b1)
                                break;
                        }
                        List.CorrectPrev(prev.P, next.P);
                        //lastValue = this.newValue;
                        return true;
                    }
                }
            }

            public node(T value, lfdll list)
            {
                this.Value_ = value;
                this.List = list;
#if DEBUG

                Id = Interlocked.Increment(ref nextId) - 1;
#endif
            }

            public node(lfdll list)
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
                    if (cursor == List.TailNode)
                        return false;

                    Thread.MemoryBarrier();
                    List.EnterTestSynchronizedBlock();
                    node next = cursor.Next_.P;
#if SynchronizedLfdll_Verbose
                    Console.WriteLine("toNext Step 0");
                    Console.WriteLine("next =");
                    List.LogNode(next);
#endif
                    List.LeaveTestSynchronizedBlock();

                    Thread.MemoryBarrier();
                    List.EnterTestSynchronizedBlock();
                    bool d = next.Next_.D;
#if SynchronizedLfdll_Verbose
                    Console.WriteLine("toNext Step 1");
                    Console.WriteLine("d = {0}", d);
#endif
                    List.LeaveTestSynchronizedBlock();

                    bool b = d;
                    if (b)
                    {

                        Thread.MemoryBarrier();
                        List.EnterTestSynchronizedBlock();
                        b &= !cursor.Next_.Equals(new nodeLink(next, true));
#if SynchronizedLfdll_Verbose
                        Console.WriteLine("toNext Step 2");
                        Console.WriteLine("!cursor.Next_.Equals(new nodeLink(next, true)) = {0}",
                            !cursor.Next_.Equals(new nodeLink(next, true)));
#endif
                        List.LeaveTestSynchronizedBlock();

                    }
                    if (b)
                    {
                        List.SetMark(ref next.Prev_);

                        Thread.MemoryBarrier();
                        List.EnterTestSynchronizedBlock();
                        node p = next.Next_.P;
#if SynchronizedLfdll_Verbose
                        Console.WriteLine("toNext Step 3");
                        Console.WriteLine("p =");
                        List.LogNode(p);
#endif
                        List.LeaveTestSynchronizedBlock();

                        List.EnterTestSynchronizedBlock();
                        bool b1 = compareExchangeNodeLink(ref cursor.Next_,
                            (nodeLink)p, (nodeLink)next);
#if SynchronizedLfdll_Verbose
                        Console.WriteLine("Remove Step 4");
                        Console.WriteLine("compareExchangeNl(ref cursor.Next_, {0}, {1}) = {2}",
                            List.NodeLinkDescription((nodeLink)p),
                            List.NodeLinkDescription((nodeLink)next),
                            b1);
                        Console.WriteLine("cursor =");
                        List.LogNode(cursor);
                        List.LogState();
#endif
                        List.LeaveTestSynchronizedBlock();

                        continue;
                    }
                    cursor = next;
                    if (!d && next != List.TailNode)
                        return true;
                }
            }
            private bool toPrev(ref node cursor)
            {
                while (true)
                {
                    if (cursor == List.HeadNode)
                        return false;

                    Thread.MemoryBarrier();
                    List.EnterTestSynchronizedBlock();
                    node prev = cursor.Prev_.P;
#if SynchronizedLfdll_Verbose
                    Console.WriteLine("toPrev Step 0");
                    Console.WriteLine("prev =");
                    List.LogNode(prev);
#endif
                    List.LeaveTestSynchronizedBlock();

                    Thread.MemoryBarrier();
                    List.EnterTestSynchronizedBlock();
                    bool b = prev.Next_.Equals(new nodeLink(cursor, false));
#if SynchronizedLfdll_Verbose
                    Console.WriteLine("toPrev Step 1");
                    Console.WriteLine("b = {0}", b);
#endif
                    List.LeaveTestSynchronizedBlock();

                    if (b)
                    {

                        Thread.MemoryBarrier();
                        List.EnterTestSynchronizedBlock();
                        b &= !cursor.Next_.D;
#if SynchronizedLfdll_Verbose
                        Console.WriteLine("toPrev Step 2");
                        Console.WriteLine("!cursor.Next_.D = {0}", !cursor.Next_.D);
#endif
                        List.LeaveTestSynchronizedBlock();

                    }
                    if (b)
                    {
                        cursor = prev;
                        if (prev != List.HeadNode)
                            return true;
                    }
                    else
                    {
                        Thread.MemoryBarrier();
                        List.EnterTestSynchronizedBlock();
                        bool b1 = cursor.Next_.D;
#if SynchronizedLfdll_Verbose
                        Console.WriteLine("toPrev Step 3");
                        Console.WriteLine("b1 = {0}", b1);
#endif
                        List.LeaveTestSynchronizedBlock();

                        if (b1)
                            toNext(ref cursor);
                        else
                            List.CorrectPrev(prev, cursor);
                    }
                }
            }

            private INode insertBefore(T value, node cursor)
            {
                return insertBefore(value, cursor, new SpinWait());
            }
            private INode insertBefore(T value, node cursor, SpinWait spin)
            {
                if (cursor == List.HeadNode)
                    return InsertAfter(value);
                node node = new node(value, List);

                Thread.MemoryBarrier();
                List.EnterTestSynchronizedBlock();
                node prev = cursor.Prev_.P;
#if SynchronizedLfdll_Verbose
                Console.WriteLine("insertBefore Step 0");
                Console.WriteLine("prev =");
                List.LogNode(prev);
#endif
                List.LeaveTestSynchronizedBlock();

                node next;
                while (true)
                {
                    while (true)
                    {

                        Thread.MemoryBarrier();
                        List.EnterTestSynchronizedBlock();
                        bool b = !cursor.Next_.D;
#if SynchronizedLfdll_Verbose
                        Console.WriteLine("insertBefore Step 1");
                        Console.WriteLine("b = {0}", b);
#endif
                        List.LeaveTestSynchronizedBlock();

                        if (b)
                            break;

                        /* Since cursor was deleted
                         * the method CorrectPrev has not returned a node 
                         * which is logically before cursor;
                         * the return value shall not have semantic meaning.
                         * As CorrectPrev apparently exprects
                         * a logical predecessor of node / cursor,
                         * prev cannot be passed to the method.
                         * This is dire for program execution
                         * especially when prev == List.TailNode. */

                        toNext(ref cursor);

                        #region Bugfix 1
                        /* Ascertain a new predecessor of cursor. */
                        Thread.MemoryBarrier();
                        List.EnterTestSynchronizedBlock();
                        prev = cursor.Prev_.P;
#if SynchronizedLfdll_Verbose
                        Console.WriteLine("insertBefore Step 1.1");
                        Console.WriteLine("prev =");
                        List.LogNode(prev);
#endif
                        List.LeaveTestSynchronizedBlock();
                        #endregion

                        prev = List.CorrectPrev(prev, cursor);
                    }
                    next = cursor;
                    node.Prev_ = new nodeLink(prev, false);
                    node.Next_ = new nodeLink(next, false);

                    List.EnterTestSynchronizedBlock();
                    bool b1 = compareExchangeNodeLink(ref prev.Next_,
                        new nodeLink(node, false),
                        new nodeLink(cursor, false));
#if SynchronizedLfdll_Verbose
                    Console.WriteLine("insertBefore Step 2");
                    Console.WriteLine("compareExchangeNl(ref prev.Next_, {0}, {1}) = {2}",
                        List.NodeLinkDescription(new nodeLink(node, false)),
                        List.NodeLinkDescription(new nodeLink(cursor, false)),
                        b1);
                    Console.WriteLine("prev =");
                    List.LogNode(prev);
                    List.LogState();
#endif
                    List.LeaveTestSynchronizedBlock();

                    if (b1)
                        break;

                    prev = List.CorrectPrev(prev, cursor);
                    spin.SpinOnce();
                }

                List.CorrectPrev(prev, next);
                return node;
            }

            private INode insertAfter(T value, node cursor)
            {
                SpinWait spin = new SpinWait();

                if (cursor == List.TailNode)
                    return insertBefore(value, cursor, spin);
                node node = new node(value, List);
                node prev = cursor;
                node next;
                while (true)
                {

                    Thread.MemoryBarrier();
                    List.EnterTestSynchronizedBlock();
                    next = prev.Next_.P;
#if SynchronizedLfdll_Verbose
                    Console.WriteLine("insertAfter Step 0");
                    Console.WriteLine("next =");
                    List.LogNode(next);
#endif
                    List.LeaveTestSynchronizedBlock();

                    node.Prev_ = new nodeLink(prev, false);
                    node.Next_ = new nodeLink(next, false);

                    List.EnterTestSynchronizedBlock();
                    bool b1 = compareExchangeNodeLink(ref cursor.Next_,
                        new nodeLink(node, false),
                        new nodeLink(next, false));
#if SynchronizedLfdll_Verbose
                    Console.WriteLine("insertAfter Step 1");
                    Console.WriteLine("compareExchangeNl(ref cursor.Next_, {0}, {1}) = {2}",
                        List.NodeLinkDescription(new nodeLink(node, false)),
                        List.NodeLinkDescription(new nodeLink(next, false)),
                        b1);
                    Console.WriteLine("cursor =");
                    List.LogNode(cursor);
                    List.LogState();
#endif
                    List.LeaveTestSynchronizedBlock();

                    if (b1)
                        break;

                    /* Not necessary because of preceding compareExchange. */
                    // Thread.MemoryBarrier();

                    List.EnterTestSynchronizedBlock();
                    bool b = prev.Next_.D;
#if SynchronizedLfdll_Verbose
                    Console.WriteLine("insertAfter Step 2");
                    Console.WriteLine("b = {0}", b);
                    List.LogNode(next);
#endif
                    List.LeaveTestSynchronizedBlock();

                    if (b)
                        return insertBefore(value, cursor, spin);
                    spin.SpinOnce();
                }
                List.CorrectPrev(prev, next);
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
    }
}
