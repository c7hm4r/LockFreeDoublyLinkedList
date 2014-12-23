using Test.Tests.Test001_.Operations;

namespace Test.Tests.Test001_.OperationResultComparers
{
    class BoolReturningOperationComparer : OperationResultComparer<BoolReturningOperation, bool, bool>
    {
        public override bool LastResultsEqual
        {
            get { return LfdllResult == LlResult; }
        }

        public BoolReturningOperationComparer(BoolReturningOperation operation)
            : base(operation)
        {
        }
    }
}
