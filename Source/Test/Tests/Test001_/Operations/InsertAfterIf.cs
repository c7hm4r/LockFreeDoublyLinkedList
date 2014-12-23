using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LockFreeDoublyLinkedList;

namespace Test.Tests.Test001_.Operations
{
    internal class InsertAfterIf : NodeCreationOperation
    {
        public override LinkedListNode<LinkedListItem> RunOnLinkedList(LinkedListExecutionState state)
        {
            if (state.Current == null)
                return null;
            ListItemData oldData = state.Current.Value.Data;
            if (oldData.Value != prevalentValue
                || state.Current.Value.Deleted)
            {
                state.AddToKnownNodes(null);
                return null;
            }
            return
                state.AddingToKnownNodes(
                    state.List.AddAfter(state.Current, new LinkedListItem(Value)));
        }

        public override LockFreeDoublyLinkedList<ListItemData>.INode RunOnLfdll(LfdllExecutionState state)
        {
            if (state.Current == null)
                return null;
            return
                state.AddingToKnownNodes(
                    state.Current.InsertAfterIf(
                        Value, data => data.Value == prevalentValue));
        }

        public override string ToString()
        {
            return base.ToString() + " if value == " + prevalentValue;
        }

        public InsertAfterIf(
            ObjectIdGenerator idGenerator, ListItemData value,
            int prevalentValue)
            : base(idGenerator, value)
        {
            this.prevalentValue = prevalentValue;
        }

        private readonly int prevalentValue;
    }
}
