using System;
using System.Collections.Generic;
using LockFreeDoublyLinkedList;

namespace Test.Tests.Test001_.Operations
{
    internal class PushRight : NodeCreationOperation
    {
        public override LinkedListNode<TestListItem> RunOnLinkedList(
            LinkedListExecutionState state)
        {
            LinkedListNode<TestListItem> prev = state.List.Last;
            if (prev == null || !prev.Value.Deleted)
                return state.AddingToKnownNodes(state.List.AddLast(new TestListItem(Value)));
            while (prev.Previous != null &&
                   prev.Previous.Value.Deleted)
            {
                prev = prev.Previous;
            }
            return state.AddingToKnownNodes(state.List.AddBefore(prev, new TestListItem(Value)));
        }

        public override LockFreeDoublyLinkedList<TestListItem>.INode
            RunOnLfdll(
            LfdllExecutionState state)
        {
            return state.AddingToKnownNodes(state.List.PushRight(new TestListItem(Value)));
        }
    
        public PushRight(ObjectIdGenerator idGenerator, long value)
            : base(idGenerator, value)
        {
        }
    }
}
