using System;
using System.Collections.Generic;
using LockFreeDoublyLinkedList;

namespace Test.Tests.Test001_.Operations
{
    internal class InsertBefore : NodeCreationOperation
    {
        public override LinkedListNode<TestListItem>
            RunOnLinkedList(
            LinkedListExecutionState state)
        {
            LinkedListNode<TestListItem> current = state.Current;
            if (current == null)
                return null;
            LinkedListNode<TestListItem> prev = current;
            while (prev.Previous != null &&
                   prev.Previous.Value.Deleted)
            {
                prev = prev.Previous;
            }
            return state.AddingToKnownNodes(state.List.AddBefore(prev, new TestListItem(Value)));
        }

        public override LockFreeDoublyLinkedList<TestListItem>.INode RunOnLfdll(
            LfdllExecutionState state)
        {
            LockFreeDoublyLinkedList<TestListItem>.INode current
                = state.Current;
            if (current == null)
                return null;
            return state.AddingToKnownNodes(current.InsertBefore(new TestListItem(Value)));
        }

        public InsertBefore(ObjectIdGenerator idGenerator, long value)
            : base(idGenerator, value)
        { }
    }
}
