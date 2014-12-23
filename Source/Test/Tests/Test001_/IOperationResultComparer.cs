//Copyright 2014 Christoph Müller

//Licensed under the Apache License, Version 2.0 (the "License");
//you may not use this file except in compliance with the License.
//You may obtain a copy of the License at

//   http://www.apache.org/licenses/LICENSE-2.0

//Unless required by applicable law or agreed to in writing, software
//distributed under the License is distributed on an "AS IS" BASIS,
//WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//See the License for the specific language governing permissions and
//limitations under the License.

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
