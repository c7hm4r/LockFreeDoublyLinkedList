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
    internal class InsertAfterIf : NodeCreationOperation
    {
	    public override LinkedListNode<LinkedListItem> RunOnLinkedList(LinkedListExecutionState state)
        {
            if (state.Current == null)
                return null;
            ListItemData oldData = state.Current.Value.Data;
            if (oldData.Value != prevalentValue
                || state.Current.Value.Deleted)
            {
                state.AddToKnownNodes(null);
                return null;
            }
            return
                state.AddingToKnownNodes(
                    state.List.AddAfter(state.Current, new LinkedListItem(Value)));
        }

	    public override ILockFreeDoublyLinkedListNode<ListItemData> RunOnLfdll(
			LfdllExecutionState state)
        {
            if (state.Current == null)
                return null;
            return
                state.AddingToKnownNodes(
                    state.Current.InsertAfterIf(
                        Value, data => data.Value == prevalentValue));
        }

	    public override string ToString()
        {
            return base.ToString() + " if value == " + prevalentValue;
        }

	    public InsertAfterIf(
            ObjectIdGenerator idGenerator, ListItemData value,
            int prevalentValue)
            : base(idGenerator, value)
        {
            this.prevalentValue = prevalentValue;
        }

	    #region private
	    private readonly int prevalentValue;
	    #endregion
    }
}
