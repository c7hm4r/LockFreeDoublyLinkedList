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
    internal class Remove : BoolReturningOperation
    {
        public override bool RunOnLinkedList(
            LinkedListExecutionState state)
        {
            if (state.Current == null
                || state.Current == state.List.First
                || state.Current == state.List.Last
                || state.Current.Value.Deleted)
            {
                return false;
            }
            state.Current.Value.Delete();
            return true;
        }

        public override bool RunOnLfdll(
            LfdllExecutionState state)
        {
            if (state.Current == null
                    || state.Current == state.List.Head
                    || state.Current == state.List.Tail)
                return false;
            return state.Current.Remove();
        }

        public Remove(ObjectIdGenerator idGenerator)
            : base(idGenerator)
        {
        }
    }
}
