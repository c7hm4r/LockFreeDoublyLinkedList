namespace Test.Tests.Test001_.Operations
{
    class GetValue : ItemDataReturningOperation
    {
        public override ListItemData RunOnLinkedList(LinkedListExecutionState state)
        {
            return state.Current == null ? null : state.Current.Value.Data;
        }

        public override ListItemData RunOnLfdll(LfdllExecutionState state)
        {
            return state.Current == null ? null : state.Current.Value;
        }

        public GetValue(ObjectIdGenerator idGenerator)
            : base(idGenerator)
        {
        }
    }
}
