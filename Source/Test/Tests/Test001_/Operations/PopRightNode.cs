﻿#region license
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
using LockFreeDoublyLinkedLists;

namespace Test.Tests.Test001_.Operations
{
	internal class PopRightNode : NodeReturningOperation
    {
	    // ReSharper disable once RedundantAssignment
	    public override LinkedListNode<LinkedListItem> RunOnLinkedList(
            LinkedListExecutionState state)
        {
            LinkedListNode<LinkedListItem> last = state.List.Last;
            while (last != null && last.Value.Deleted)
                last = last.Previous;
            if (last == null)
                return null;
            last.Value.Delete();
            return state.AddingToKnownNodes(last);
        }

	    public override ILockFreeDoublyLinkedListNode<ListItemData> RunOnLfdll(
            LfdllExecutionState state)
        {
            ILockFreeDoublyLinkedListNode<ListItemData> node
                = state.List.PopRightNode();
            if (node != null)
                state.AddToKnownNodes(node);
            return node;
        }

	    public PopRightNode(ObjectIdGenerator idGenerator)
            : base(idGenerator)
        {
        }
    }
}
