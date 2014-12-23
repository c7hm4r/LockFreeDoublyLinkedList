namespace Test.Tests.Test001_.Operations
{
    class Remove : BoolReturningOperation
    {
        public override bool RunOnLinkedList(
            LinkedListExecutionState state)
        {
            if (state.Current == null
                || state.Current.Value.Deleted)
            {
                return false;
            }
            state.Current.Value.Delete();
            return true;
        }

        public override bool RunOnLfdll(
            LfdllExecutionState state)
        {
            if (state.Current == null)
                return false;
            return state.Current.Remove();
        }

        public Remove(ObjectIdGenerator idGenerator)
            : base(idGenerator)
        {
        }
    }
}
