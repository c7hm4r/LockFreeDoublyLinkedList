using System;
using System.Collections.Generic;
using LockFreeDoublyLinkedList;

namespace Test.Tests.Test001_.Operations
{
    internal class Previous : VoidOperation
    {
        public override object RunOnLinkedList(
            LinkedListExecutionState state)
        {
            LinkedListNode<TestListItem> previous;
            if (state.Current == null)
            {
                previous = state.List.First;
                while (previous != null && previous.Value.Deleted)
                    previous = previous.Next;
            }
            else
            {
                previous = state.Current.Previous;
                while (previous != null && previous.Value.Deleted)
                    previous = previous.Previous;
            }
            state.Current = previous;
            return null;
        }

        public override object RunOnLfdll(
            LfdllExecutionState state)
        {
            LockFreeDoublyLinkedList<TestListItem>.INode
                current = state.Current;
            if (current == null)
            {
                current = state.List.Head.Next;
                if (current == state.List.Tail)
                    current = null;
            }
            else
            {
                current = current.Prev;
                if (current == state.List.Head)
                    current = null;
            }
            state.Current = current;
            return null;
        }

        public Previous(ObjectIdGenerator idGenerator) : base(idGenerator)
        {
        }
    }
}
