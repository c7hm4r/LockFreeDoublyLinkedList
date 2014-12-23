using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Test.Tests.Test001_.Operations;

namespace Test.Tests.Test001_.OperationResultComparers
{
    internal class ItemDataReturningOperationComparer
        : OperationResultComparer<ItemDataReturningOperation, ListItemData, ListItemData>
    {
        public override bool LastResultsEqual
        {
            get { return Equals(LfdllResult, LlResult); }
        }

        public ItemDataReturningOperationComparer(
            ItemDataReturningOperation operation) : base(operation)
        {
        }
    }
}
