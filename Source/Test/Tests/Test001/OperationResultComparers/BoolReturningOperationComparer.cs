using Test.Tests.Test001_.Operations;

namespace Test.Tests.Test001_.OperationResultComparers
{
    class BoolReturningOperationComparer : OperationResultComparer<BoolReturningOperation>
    {
        public override void RunOnLfdll(LfdllExecutionState state)
        {
            lfdllResult = Operation.RunOnLfdll(state);
        }

        public override void RunOnLinkedList(LinkedListExecutionState state)
        {
            linkedListResult = Operation.RunOnLinkedList(state);
        }

        public override bool LastResultsEqual
        {
            get { return lfdllResult == linkedListResult; }
        }

        public BoolReturningOperationComparer(BoolReturningOperation operation)
            : base(operation)
        {
        }

        private bool lfdllResult, linkedListResult;
    }
}
