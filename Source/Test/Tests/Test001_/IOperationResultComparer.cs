namespace Test.Tests.Test001_
{
    abstract class OperationResultComparer<TOperation, TLfdllResult, TLlResult>
        : IOperationResultComparer
        where TOperation : IOperation<TLlResult, TLfdllResult>
    {
        public TOperation Operation { get; private set; }

        public void RunOnLfdll(LfdllExecutionState state)
        {
            LfdllResult = Operation.RunOnLfdll(state);
        }

        public void RunOnLinkedList(LinkedListExecutionState state)
        {
            LlResult = Operation.RunOnLinkedList(state);
        }

        public abstract bool LastResultsEqual { get; }

        public TLfdllResult LfdllResult { get; private set; }
        public TLlResult LlResult { get; private set; }

        public override string ToString()
        {
            return Operation.ToString() + ": " + LfdllResult + ", " + LlResult;
        }

        public OperationResultComparer(TOperation operation)
        {
            Operation = operation;
        }
    }

    interface IOperationResultComparer
    {
        void RunOnLfdll(LfdllExecutionState state);
        void RunOnLinkedList(LinkedListExecutionState state);

        bool LastResultsEqual { get; }        
    }
}
