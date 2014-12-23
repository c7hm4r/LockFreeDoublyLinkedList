using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Test.Tests.Test001_.Operations
{
    internal class CompareExchangeValue : ItemDataReturningOperation
    {
        private readonly int oldValue;
        private readonly int newValue;

        public override ListItemData RunOnLinkedList(
            LinkedListExecutionState state)
        {
            if (state.Current == null)
                return null;
            ListItemData prevalentData = state.Current.Value.Data;
            if (prevalentData.Value != oldValue)
                return prevalentData;
            state.Current.Value =
                state.Current.Value.NewWithData(
                    prevalentData.NewWithValue(newValue));
            return prevalentData;
        }

        public override ListItemData RunOnLfdll(LfdllExecutionState state)
        {
            if (state.Current == null)
                return null;
            ListItemData oldData = state.Current.Value;
            while (true)
            {
                if (oldData.Value != oldValue)
                    return oldData;
                ListItemData prevalentData = state.Current.CompareExchangeValue(
                    oldData.NewWithValue(newValue), oldData);
                if (ReferenceEquals(prevalentData, oldData))
                    return prevalentData;
                oldData = prevalentData;
            }
        }

        public CompareExchangeValue(ObjectIdGenerator idGenerator, int oldValue, int newValue)
            : base(idGenerator)
        {
            this.oldValue = oldValue;
            this.newValue = newValue;
        }
    }
}
