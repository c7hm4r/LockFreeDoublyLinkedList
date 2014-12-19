using LockFreeDoublyLinkedList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Test.Tests.Test001_.Operations
{
    abstract class NodeCreationOperation
        : NodeReturningOperation
    {
        public long Value { get; private set; }

        public override string ToString()
        {
            return base.ToString() + " <" + Value + ">";
        }

        public NodeCreationOperation(
            ObjectIdGenerator idGenerator, long value)
            : base(idGenerator)
        {
            Value = value;
        }
    }
}
