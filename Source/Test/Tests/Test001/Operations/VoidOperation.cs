using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Test.Tests.Test001_.Operations
{
    abstract class VoidOperation : Operation<object, object>
    {
        public VoidOperation(ObjectIdGenerator idGenerator)
            : base(idGenerator)
        {
        }
    }
}
