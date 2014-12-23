using System;
using System.Collections.Generic;
using LockFreeDoublyLinkedList;

namespace Test.Tests.Test001_.Operations
{
    class PopRightNode : NodeReturningOperation
    {
        // ReSharper disable once RedundantAssignment
        public override LinkedListNode<LinkedListItem> RunOnLinkedList(
            LinkedListExecutionState state)
        {
            LinkedListNode<LinkedListItem> last = state.List.Last;
            while (last != null && last.Value.Deleted)
                last = last.Previous;
            if (last == null)
                return null;
            last.Value.Delete();
            return state.AddingToKnownNodes(last);
        }

        public override LockFreeDoublyLinkedList<ListItemData>.INode RunOnLfdll(
            LfdllExecutionState state)
        {
            LockFreeDoublyLinkedList<ListItemData>.INode node
                = state.List.PopRightNode();
            if (node != null)
                state.AddToKnownNodes(node);
            return node;
        }

        public PopRightNode(ObjectIdGenerator idGenerator)
            : base(idGenerator)
        {
        }
    }
}
