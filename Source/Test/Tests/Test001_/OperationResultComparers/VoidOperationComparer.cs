using Test.Tests.Test001_.Operations;

namespace Test.Tests.Test001_.OperationResultComparers
{
    class VoidOperationComparer : OperationResultComparer<VoidOperation, object, object>
    {
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
