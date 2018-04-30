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
#define CheckCorrectness
/* If a thread misses, cancel immediately
 * and don’t become entangled in a deadlock.
 * Additionally, output the seed number.
 * May exacerbate the exception handling in the IDE, though. */
#define HandleTaskExceptionImmediately
/* PopLeft is not atomic supported by the current LFDLL implementation. */
//#define PopLeft


#if SynchronizedLfdll
#if Verbose
#define SynchronizedLfdll_verbose
#endif
#undef RunOperationsSequentially
#endif

using System;
using Test.Tests.Test001_;

namespace Test.Tests
{
    internal partial class Test001
    {
        private class operationTiming
        {
            public readonly IOperationResultComparer Operation;
            public readonly long Start, End;

            public operationTiming(IOperationResultComparer operation,
                Tuple<long, long> timing)
            {
                Operation = operation;
                Start = timing.Item1;
                End = timing.Item2;
            }
        }
    }
}