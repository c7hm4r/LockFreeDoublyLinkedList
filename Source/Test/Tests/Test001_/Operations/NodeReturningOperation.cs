using LockFreeDoublyLinkedList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Test.Tests.Test001_.Operations
{
    abstract class NodeReturningOperation : Operation<
            LinkedListNode<LinkedListItem>,
            LockFreeDoublyLinkedList<ListItemData>.INode>
    {
        public NodeReturningOperation(ObjectIdGenerator idGenerator) : base(idGenerator)
        {
        }
    }
}
