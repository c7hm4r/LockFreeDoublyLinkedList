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
    internal class Previous : VoidOperation
    {
	    public override object RunOnLinkedList(
            LinkedListExecutionState state)
        {
            LinkedListNode<LinkedListItem> previous;
            if (state.Current == null)
            {
                previous = state.List.First;
                while (previous != null && previous.Value.Deleted)
                    previous = previous.Next;
            }
            else
            {
                previous = state.Current.Previous;
                while (previous != null && previous.Value.Deleted)
                    previous = previous.Previous;
            }
            state.Current = previous;
            return null;
        }

	    public override object RunOnLfdll(
            LfdllExecutionState state)
        {
            ILockFreeDoublyLinkedListNode<ListItemData> current = state.Current;
            if (current == null)
            {
                current = state.List.Head.Next;
                if (current == state.List.Tail)
                    current = null;
            }
            else
            {
                current = current.Prev;
                if (current == state.List.Head)
                    current = null;
            }
            state.Current = current;
            return null;
        }

	    public Previous(ObjectIdGenerator idGenerator) : base(idGenerator)
        {
        }
    }
}
