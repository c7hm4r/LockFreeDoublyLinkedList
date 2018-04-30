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
    internal class SetValue : VoidOperation
    {
        public override object RunOnLinkedList(LinkedListExecutionState state)
        {
            LinkedListNode<LinkedListItem> current = state.Current;
            if (current != null
                    && current != state.List.First
                    && current != state.List.Last)
            {
                current.Value = current.Value.NewWithData(
                    current.Value.Data.NewWithValue(value));
            }
            return null;
        }

        public override object RunOnLfdll(LfdllExecutionState state)
        {
            if (state.Current != null
                    && state.Current != state.List.Head
                    && state.Current != state.List.Tail)
            {
                long nodeId = state.Current.Value.NodeId;
                state.Current.Value = new ListItemData(nodeId, value);
            }
            return null;
        }

        public SetValue(ObjectIdGenerator idGenerator, int value)
            : base(idGenerator)
        {
            this.value = value;
        }

        #region private
        private readonly int value;
        #endregion
    }
}