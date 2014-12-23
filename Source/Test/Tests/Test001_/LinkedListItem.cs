using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Test.Tests.Test001_
{
    class LinkedListItem
    {
        public bool Deleted { get; private set; }
        public ListItemData Data { get; private set; }

        public void Delete()
        {
            Deleted = true;
        }

        public LinkedListItem NewWithData(ListItemData data)
        {
            return new LinkedListItem(data)
            {
                Deleted = Deleted,
            };
        }

        public override string ToString()
        {
            return "<" + Data + ">" + (Deleted ? "D" : "");
        }

        public LinkedListItem(ListItemData data)
        {
            Data = data;
        }
    }
}
