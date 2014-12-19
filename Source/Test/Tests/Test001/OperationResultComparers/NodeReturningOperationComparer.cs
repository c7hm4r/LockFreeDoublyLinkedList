using System;
using System.Collections.Generic;
using LockFreeDoublyLinkedList;
using Test.Tests.Test001_.Operations;

namespace Test.Tests.Test001_.OperationResultComparers
{
    class NodeReturningOperationComparer : OperationResultComparer<NodeReturningOperation>
    {
        public override void RunOnLfdll(LfdllExecutionState state)
        {
            lfdllResult = Operation.RunOnLfdll(state);
        }

        public override void RunOnLinkedList(LinkedListExecutionState state)
        {
            llResult = Operation.RunOnLinkedList(state);
        }

        public override bool LastResultsEqual
        {
            get
            {
                if (llResult == null)
                    return lfdllResult == null;
                if (lfdllResult == null)
                    return false;
                return llResult.Value.Value
                    == lfdllResult.Value.Value;
            }
        }

        public NodeReturningOperationComparer(NodeReturningOperation operation)
            : base(operation)
        {
        }

        private LinkedListNode<TestListItem> llResult = null;
        private LockFreeDoublyLinkedList<TestListItem>.INode lfdllResult = null;
    }
}
