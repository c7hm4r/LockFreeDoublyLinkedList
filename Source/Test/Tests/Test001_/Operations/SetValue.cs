using System.Collections.Generic;

namespace Test.Tests.Test001_.Operations
{
    internal class SetValue : VoidOperation
    {
        private readonly int value;

        public override object RunOnLinkedList(LinkedListExecutionState state)
        {
            LinkedListNode<LinkedListItem> current = state.Current;
            if (current != null)
            {
                current.Value = current.Value.NewWithData(
                    current.Value.Data.NewWithValue(value));
            }
            return null;
        }

        public override object RunOnLfdll(LfdllExecutionState state)
        {
            if (state.Current != null)
            {
                long nodeId = state.Current.Value.NodeId;
                state.Current.Value = new ListItemData(nodeId, value);
            }
            return null;
        }

        public SetValue(ObjectIdGenerator idGenerator, int value)
            : base(idGenerator)
        {
            this.value = value;
        }
    }
}