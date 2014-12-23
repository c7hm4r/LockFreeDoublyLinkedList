using System;
using System.Collections.Generic;
using LockFreeDoublyLinkedList;

namespace Test.Tests.Test001_.Operations
{
    internal class Next : VoidOperation
    {
        public override object RunOnLinkedList(
            LinkedListExecutionState state)
        {
            LinkedListNode<LinkedListItem> current = state.Current;
            if (current == null)
            {
                current = state.List.Last;
                while (current != null && current.Value.Deleted)
                    current = current.Previous;
            }
            else
            {
                do
                {
                    current = current.Next;
                } while (current != null
                         && current.Value.Deleted);
            }
            state.Current = current;
            return null;
        }

        public override object RunOnLfdll(
            LfdllExecutionState state)
        {
            LockFreeDoublyLinkedList<ListItemData>.INode current = state.Current;
            if (current == null)
            {
                current = state.List.Tail.Prev;
                if (current == state.List.Head)
                    current = null;
            }
            else
            {
                current = current.Next;
                if (current == state.List.Tail)
                    current = null;
            }
            state.Current = current;
            return null;
        }

        public Next(ObjectIdGenerator idGenerator) : base(idGenerator)
        {
        }
    }
}
