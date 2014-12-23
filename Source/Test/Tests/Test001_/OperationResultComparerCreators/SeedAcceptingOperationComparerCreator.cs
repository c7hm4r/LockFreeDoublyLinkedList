using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Test.Tests.Test001_.OperationResultComparers;
using Test.Tests.Test001_.Operations;

namespace Test.Tests.Test001_.OperationResultComparerCreators
{
    internal class SeedAcceptingOperationComparerCreator
        : IOperationResultComparerCreator
    {
        public IOperationResultComparer CreateOperationResultComparer(
            Random rand, ObjectIdGenerator idGenerator, Counter counter)
        {
            int seed = rand.Next();
            return new VoidOperationComparer(
                operationCreator(idGenerator, seed));
        }

        public SeedAcceptingOperationComparerCreator(
            Func<ObjectIdGenerator, int, VoidOperation> operationCreator)
        {
            this.operationCreator = operationCreator;
        }

        private Func<ObjectIdGenerator, int, VoidOperation> operationCreator;
    }
}
