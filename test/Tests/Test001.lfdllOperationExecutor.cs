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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using LockFreeDoublyLinkedLists;
using Test.Tests.Test001_;

namespace Test.Tests
{
    internal partial class Test001
    {
        private class lfdllOperationExecutor
        {
            public int Name { get; }

            /* Should be executed before a
             * LFDLLOperationExecutor modifies list. */

            public void Initialize()
            {
                ILockFreeDoublyLinkedListNode<ListItemData> current =
                    state.List.Head;
                for (int i = 0; i < eParams.StartIndex; i++)
                    current = current.Next;
                state.Current = current;
            }

            public List<operationTiming> Run()
            {
#if !RunOperationsSequentially
                Thread.CurrentThread.Name = Name.ToString();
#endif
                return eParams.Operations
                    .Select(operation =>
                        new operationTiming(
                            operation,
                            processOperation(operation, counter)))
                    .ToList();
            }

            public lfdllOperationExecutor(
                ExecutionSequenceParameters eParams,
                ILockFreeDoublyLinkedList<ListItemData> list, Counter counter,
                int name)
            {
                this.eParams = eParams;
                this.counter = counter;
                Name = name;
                state = new LfdllExecutionState(list);
                state.AddToKnownNodes(list.Head);
                state.AddingToKnownNodes(list.Tail);
            }

            #region private
            private readonly ExecutionSequenceParameters eParams;
            private readonly LfdllExecutionState state;
            private readonly Counter counter;

            private Tuple<long, long> processOperation(
                IOperationResultComparer op, Counter ctr)
            {
#if SynchronizedLfdll_verbose
                Console.WriteLine("({0}) Nächste Operation: {1}", Name, op);
                Console.WriteLine("({0}) Aktueller Knoten:", Name);
                state.List.LogNode(state.Current);
#endif

                Tuple<long, long> timing = measureTime(
                    () =>
                    {
                        op.RunOnLfdll(state);
                    },
                    ctr);

#if SynchronizedLfdll_verbose
                Console.WriteLine("({0}) Beendete Operation: {1}", Name, op);
#endif

                return timing;
            }
            #endregion
        }
    }
}