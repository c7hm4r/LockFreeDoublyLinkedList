using System;
using System.Collections.Generic;
using LockFreeDoublyLinkedList;

namespace Test.Tests.Test001_.Operations
{
    internal class PushLeft : NodeCreationOperation
    {
        public override LinkedListNode<TestListItem> RunOnLinkedList(
            LinkedListExecutionState state)
        {
            return state.AddingToKnownNodes(state.List.AddFirst(new TestListItem(Value)));
        }

        public override LockFreeDoublyLinkedList<TestListItem>.INode
            RunOnLfdll(LfdllExecutionState state)
        {
            return state.AddingToKnownNodes(state.List.PushLeft(new TestListItem(Value)));
        }

        public PushLeft(ObjectIdGenerator idGenerator, long value)
            : base(idGenerator, value)
        {
        }
    }
}
