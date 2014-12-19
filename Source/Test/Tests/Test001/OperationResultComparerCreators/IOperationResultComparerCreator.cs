using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Test.Tests.Test001_.OperationResultComparerCreators
{
    interface IOperationResultComparerCreator
    {
        IOperationResultComparer CreateOperationResultComparer(
            Random rand, ObjectIdGenerator idGenerator, Counter counter);
    }
}
