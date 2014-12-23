using System;
using System.Collections.Generic;
using LockFreeDoublyLinkedList;
using Test.Tests.Test001_.Operations;

namespace Test.Tests.Test001_.OperationResultComparers
{
    class NodeReturningOperationComparer
        : OperationResultComparer<NodeReturningOperation,
            LockFreeDoublyLinkedList<ListItemData>.INode,
            LinkedListNode<LinkedListItem>>
    {
        public override bool LastResultsEqual
        {
            get
            {
                if (LlResult == null)
                    return LfdllResult == null;
                if (LfdllResult == null)
                    return false;
                return LlResult.Value.Data.Equals(LfdllResult.Value);
            }
        }
        public NodeReturningOperationComparer(NodeReturningOperation operation)
            : base(operation)
        {
        }
    }
}
