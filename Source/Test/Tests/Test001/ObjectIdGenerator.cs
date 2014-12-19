using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Test.Tests.Test001_
{
    class ObjectIdGenerator
    {
        public long GetId(object o)
        {
            bool b;
            return generator.GetId(o, out b);
        }

        private ObjectIDGenerator generator = new ObjectIDGenerator();
    }
}
