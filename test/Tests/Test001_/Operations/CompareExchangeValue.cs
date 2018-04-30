#region license
// Copyright 2016 Christoph Müller
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Test.Tests.Test001_.Operations
{
    internal class CompareExchangeValue : ItemDataReturningOperation
    {
        public override ListItemData RunOnLinkedList(
            LinkedListExecutionState state)
        {
            if (state.Current == null || state.Current.Value.IsDummy)
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
            if (state.Current == null || state.Current.IsDummyNode)
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

        public override string ToString()
        {
            return base.ToString() + " " + oldValue + " " + newValue;
        }

        public CompareExchangeValue(ObjectIdGenerator idGenerator, int oldValue, int newValue)
            : base(idGenerator)
        {
            this.oldValue = oldValue;
            this.newValue = newValue;
        }

        #region private
        private readonly int oldValue;
        private readonly int newValue;
        #endregion
    }
}
