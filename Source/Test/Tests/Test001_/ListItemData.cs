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
