using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Test.Tests.Test001_.Operations
{
    abstract class BoolReturningOperation : Operation<bool, bool>
    {
        public BoolReturningOperation(ObjectIdGenerator idGenerator)
            : base(idGenerator)
        {
        }
    }
}
