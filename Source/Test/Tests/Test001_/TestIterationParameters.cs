using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Test.Tests.Test001_
{
    class TestIterationParameters
    {
        public List<ExecutionSequenceParameters> OperationSequences
        { get; set; }

        public int InitialListLength { get; set; }
        public int InitializationSeed { get; set; }
        public int ExecutionSeed { get; set; }
    }

    class ExecutionSequenceParameters
    {
        public List<IOperationResultComparer> Operations { get; set; }
        public int StartIndex { get; set; }
    }
}
