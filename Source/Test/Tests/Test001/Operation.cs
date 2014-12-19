using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LockFreeDoublyLinkedList;

namespace Test.Tests.Test001_
{
    interface IOperation<out T, out U>
    {
        T RunOnLinkedList(LinkedListExecutionState state);
        U RunOnLfdll(LfdllExecutionState state);
    }

    abstract class Operation<T1, T2> : IOperation<T1, T2>
    {
        public override string ToString()
        {
            return GetType().Name;
        }

        public abstract T1 RunOnLinkedList(
            LinkedListExecutionState state);

        public abstract T2 RunOnLfdll(
            LfdllExecutionState state);

        public Operation(ObjectIdGenerator idGenerator)
        {
            IdGenerator = idGenerator;
        }

        protected ObjectIdGenerator IdGenerator { get; private set; }
    }
}
