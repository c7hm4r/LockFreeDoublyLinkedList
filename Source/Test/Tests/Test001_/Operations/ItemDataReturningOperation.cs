using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Test.Tests.Test001_.Operations
{
    internal abstract class ItemDataReturningOperation
        : Operation<ListItemData, ListItemData>
    {
        public ItemDataReturningOperation(ObjectIdGenerator idGenerator)
            : base(idGenerator)
        {
        }
    }
}
