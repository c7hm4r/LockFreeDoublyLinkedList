using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Test.Tests.Test001_
{
    class TestListItem
    {
        public bool Deleted { get; private set; }
        public long Value { get; private set; }

        public void Delete()
        {
            Deleted = true;
        }

        public override string ToString()
        {
            return "<" + Value + ">" + (Deleted ? "D" : "");
        }

        public TestListItem(long value)
        {
            Value = value;
        }
    }
}
