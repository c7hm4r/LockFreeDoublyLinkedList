using System;
using System.Collections.Generic;
using LockFreeDoublyLinkedList;

namespace Test.Tests.Test001_.Operations
{
    internal class PushLeft : NodeCreationOperation
    {
        public override LinkedListNode<LinkedListItem> RunOnLinkedList(
            LinkedListExecutionState state)
        {
            return state.AddingToKnownNodes(state.List.AddFirst(new LinkedListItem(Value)));
        }

        public override LockFreeDoublyLinkedList<ListItemData>.INode
            RunOnLfdll(LfdllExecutionState state)
        {
            return state.AddingToKnownNodes(state.List.PushLeft(Value));
        }

        public PushLeft(ObjectIdGenerator idGenerator, ListItemData value)
            : base(idGenerator, value)
        {
        }
    }
}
