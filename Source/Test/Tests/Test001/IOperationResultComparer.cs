namespace Test.Tests.Test001_
{
    abstract class OperationResultComparer<TOperation> : IOperationResultComparer
    {
        public TOperation Operation { get; private set; } 

        public abstract void RunOnLfdll(LfdllExecutionState state);
        public abstract void RunOnLinkedList(LinkedListExecutionState state);

        public abstract bool LastResultsEqual { get; }

        public override string ToString()
        {
            return Operation.ToString();
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
