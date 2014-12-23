using System;
using System.Collections.Generic;
using LockFreeDoublyLinkedList;

namespace Test.Tests.Test001_.Operations
{
    internal class PushRight : NodeCreationOperation
    {
        public override LinkedListNode<LinkedListItem> RunOnLinkedList(
            LinkedListExecutionState state)
        {
            LinkedListNode<LinkedListItem> prev = state.List.Last;
            if (prev == null || !prev.Value.Deleted)
                return state.AddingToKnownNodes(state.List.AddLast(new LinkedListItem(Value)));
            while (prev.Previous != null &&
                   prev.Previous.Value.Deleted)
            {
                prev = prev.Previous;
            }
            return state.AddingToKnownNodes(state.List.AddBefore(prev, new LinkedListItem(Value)));
        }

        public override LockFreeDoublyLinkedList<ListItemData>.INode
            RunOnLfdll(
            LfdllExecutionState state)
        {
            return state.AddingToKnownNodes(state.List.PushRight(Value));
        }
    
        public PushRight(ObjectIdGenerator idGenerator, ListItemData value)
            : base(idGenerator, value)
        {
        }
    }
}
