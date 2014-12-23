//Copyright 2014 Christoph Müller

//Licensed under the Apache License, Version 2.0 (the "License");
//you may not use this file except in compliance with the License.
//You may obtain a copy of the License at

//   http://www.apache.org/licenses/LICENSE-2.0

//Unless required by applicable law or agreed to in writing, software
//distributed under the License is distributed on an "AS IS" BASIS,
//WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//See the License for the specific language governing permissions and
//limitations under the License.

using System.Collections.Generic;

namespace Test.Tests.Test001_.Operations
{
    internal class SetValue : VoidOperation
    {
        private readonly int value;

        public override object RunOnLinkedList(LinkedListExecutionState state)
        {
            LinkedListNode<LinkedListItem> current = state.Current;
            if (current != null)
            {
                current.Value = current.Value.NewWithData(
                    current.Value.Data.NewWithValue(value));
            }
            return null;
        }

        public override object RunOnLfdll(LfdllExecutionState state)
        {
            if (state.Current != null)
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
    }
}