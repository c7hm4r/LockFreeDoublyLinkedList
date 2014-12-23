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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Test.Tests.Test001_
{
    class ListItemData
    {
        public long NodeId { get; private set; }
        public int Value { get; private set; }

        public ListItemData NewWithValue(int value)
        {
            return new ListItemData(NodeId, value);
        }

        public override string ToString()
        {
            return "<" + NodeId + ", " + (Value.ToString() + ">");
        }

        // override object.Equals
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            ListItemData objAsLid = obj as ListItemData;

            return NodeId == objAsLid.NodeId
                && Value == objAsLid.Value;
        }

// override object.GetHashCode
        public override int GetHashCode()
        {
            return ((0x51ed270b + Value) * -1521134295
                + NodeId.GetHashCode())  * -1521134295;
        }

        public ListItemData(long nodeId, int value)
        {
            NodeId = nodeId;
            Value = value;
        }
    }
}
