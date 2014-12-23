using System;
using System.Collections.Generic;
using LockFreeDoublyLinkedList;

namespace Test.Tests.Test001_.Operations
{
    internal class InsertBefore : NodeCreationOperation
    {
        public override LinkedListNode<LinkedListItem>
            RunOnLinkedList(
            LinkedListExecutionState state)
        {
            LinkedListNode<LinkedListItem> current = state.Current;
            if (current == null)
                return null;
            LinkedListNode<LinkedListItem> prev = current;
            while (prev.Previous != null &&
                   prev.Previous.Value.Deleted)
            {
                prev = prev.Previous;
            }
            return state.AddingToKnownNodes(state.List.AddBefore(prev, new LinkedListItem(Value)));
        }

        public override LockFreeDoublyLinkedList<ListItemData>.INode RunOnLfdll(
            LfdllExecutionState state)
        {
            LockFreeDoublyLinkedList<ListItemData>.INode current
                = state.Current;
            if (current == null)
                return null;
            return state.AddingToKnownNodes(current.InsertBefore(Value));
        }

        public InsertBefore(ObjectIdGenerator idGenerator, ListItemData value)
            : base(idGenerator, value)
        { }
    }
}
