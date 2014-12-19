using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Test.Tests.Test001_.Operations
{
    class SelectRandomKnownNode : VoidOperation
    {
        public override object RunOnLinkedList(LinkedListExecutionState state)
        {
            return run(state);
        }

        public override object RunOnLfdll(LfdllExecutionState state)
        {
            return run(state);
        }

        public override string ToString()
        {
            return base.ToString() + ", seed: " + seed;
        }

        public SelectRandomKnownNode(
            ObjectIdGenerator idGenerator, int seed)
            : base(idGenerator)
        {
            this.seed = seed;
        }

        private int seed;

        private object run(IExecutionState state)
        {
            state.CurrentIndex = new Random(seed)
                .Next(state.KnownNodesCollection.Count);
            return null;
        }
    }
}
