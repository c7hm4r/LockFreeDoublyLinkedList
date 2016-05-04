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
using LockFreeDoublyLinkedList;

namespace Test.Tests.Test001_.Operations
{
    internal class InsertAfter : NodeCreationOperation
    {
	    public override LinkedListNode<LinkedListItem> RunOnLinkedList(
            LinkedListExecutionState state)
        {
            LinkedListNode<LinkedListItem> current = state.Current;
            if (current == null)
                return null;

            if (!current.Value.Deleted)
                return state.AddingToKnownNodes(
                    state.List.AddAfter(current, new LinkedListItem(Value)));
            LinkedListNode<LinkedListItem> prev = current;
            while (prev.Previous != null &&
                    prev.Previous.Value.Deleted)
            {
                prev = prev.Previous;
            }
            return state.AddingToKnownNodes(state.List.AddBefore(prev, new LinkedListItem(Value)));
        }

	    public override ILockFreeDoublyLinkedListNode<ListItemData> RunOnLfdll(
            LfdllExecutionState state)
        {
            ILockFreeDoublyLinkedListNode<ListItemData> current = state.Current;
            if (current == null)
                return null;
            return state.AddingToKnownNodes(current.InsertAfter(Value));
        }

	    public InsertAfter(ObjectIdGenerator idGenerator, ListItemData value)
            : base(idGenerator, value)
        { }
    }
}
