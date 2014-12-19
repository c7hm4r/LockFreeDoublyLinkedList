using Test.Tests.Test001_.Operations;

namespace Test.Tests.Test001_.OperationResultComparers
{
    class VoidOperationComparer : OperationResultComparer<VoidOperation>
    {
        public override void RunOnLfdll(LfdllExecutionState state)
        {
            Operation.RunOnLfdll(state);
        }

        public override void RunOnLinkedList(LinkedListExecutionState state)
        {
            Operation.RunOnLinkedList(state);
        }

        public override bool LastResultsEqual
        {
            get { return true; }
        }

        public VoidOperationComparer(VoidOperation operation)
            : base(operation)
        {
        }
    }
}
