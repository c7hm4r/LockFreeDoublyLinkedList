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


/* For reproducibility by reference of the seed number,
 * if a error has been found.
 * However, modifies the code insofar
 * that additional functions are inserted
 * which could act like memory barriers. */
//#define SynchronizedLFDLL
/* For determination the sequential behaviour of the LFDLL operations. */
//#define RunOperationsSequentially
//#define Verbose
/* To check the correctness of the resulting list.
 * May last a long time depending on the number of threads,
 * the number of operations per thread and the size of the list
 * (the smaller, the longer).
 * Deactivate for the fast search for exceptions. */
#define CheckCorrectness
/* If a thread misses, cancel immediately
 * and don’t become entangled in a deadlock.
 * Additionally, output the seed number.
 * May exacerbate the exception handling in the IDE, though. */
#define HandleTaskExceptionImmediately
/* PopLeft is not atomic supported by the current LFDLL implementation. */
//#define PopLeft


#if SynchronizedLFDLL
    #if Verbose
        #define SynchronizedLFDLL_Verbose
    #endif
    #undef RunOperationsSequentially
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LockFreeDoublyLinkedList;
using Test.Tests.Test001_;
using Test.Tests.Test001_.OperationResultComparers;
using Test.Tests.Test001_.Operations;

namespace Test.Tests
{
    internal class Test001 : Test
    {
        public override void Main(string[] args)
        {
#if Verbose
            const bool verbose = true;
#else
            const bool verbose = false;
#endif

            int operationSequencesNumber = 3;
            const int operationNumberPerSequence = 4; 
            const int listSize = 4;

            //Seed = -1182330010;
            //Seed = 1136758711;

            //Seed = 2037835186;
            //Seed = -1713978986;
            //Seed = -61631207;
            //Seed = 2078889772;
            //Seed = 234923264;
            //Seed = -583307162;
            Random rand2 = new Random(Seed);

            TestIterationParameters iterationParameters =
                newIterationParameters(
                    listSize, operationSequencesNumber,
                    operationNumberPerSequence,
                    rand2);

#if Verbose
            Console.WriteLine("Test of LockFreeDoublyLinkedList");

            Console.WriteLine("Number of executors (Threads): " + operationSequencesNumber);
            Console.WriteLine("Operations per executor: "
                + operationNumberPerSequence);
            Console.WriteLine("List size: " + listSize);

            Console.WriteLine("seed: " + Seed);

            Console.WriteLine("startIndex-es: "
                + string.Join(" \t", iterationParameters.OperationSequences.Select(eParam => eParam.StartIndex)));

            Console.WriteLine("operations: ");
            for (int i = 0; i < iterationParameters.OperationSequences.Count; i++)
            {
                ExecutionSequenceParameters executionSequenceParameters
                    = iterationParameters.OperationSequences[i];
                Console.WriteLine("\t{0}", i);
                foreach (IOperationResultComparer op in executionSequenceParameters.Operations)
                {
                    Console.WriteLine("\t\t{0}", op);
                }
            }
#endif

#if SynchronizedLFDLL
            Tuple<List<List<operationTiming>>, LockFreeDoublyLinkedList<ListItemData>> lfdllResult
                = runOnLfdll(iterationParameters);
#else
            Tuple<List<List<operationTiming>>, LockFreeDoublyLinkedList<ListItemData>> lfdllResult
                = runOnLfdll(iterationParameters);
#endif

            List<ListItemData> lfdllResultList = lfdllResult.Item2.ToList();

            List<operationExecutionInfo> timings
                = lfdllResult.Item1
                    .SelectMany((opTimings, i) => opTimings.Select(
                        opTiming => new operationExecutionInfo(opTiming, i)))
                    .ToList();

            IEqualityComparer<ListItemData> equalityComparer
                = LinqHelper.ToEqualityComparer(
                    (ListItemData item1, ListItemData item2) =>
                        item1.NodeId == item2.NodeId
                            && item1.Value == item2.Value,
                    item =>
                        (0x51ed270b + item.GetHashCode()) * -1521134295);

#if CheckCorrectness
            bool found =
                permutations(timings)
                    .Select(
                        permTimings =>
                            runOnLinkedList(
                                iterationParameters,
                                permTimings.Select(oei => oei.ExecutorIndex)
                                    .ToList()))
                    .Any(
                        llResult =>
                            llResult.SequenceEqual(
                                lfdllResultList, equalityComparer)
                            && iterationParameters.OperationSequences.All(
                                os => os.Operations.All(o => o.LastResultsEqual)));
            if (verbose && found)
                Console.WriteLine("Gefunden.");
            if (verbose || !found)
            {
                Console.WriteLine("Test of LockFreeDoublyLinkedList");

                Console.WriteLine("Number of executors (Threads): " + operationSequencesNumber);
                Console.WriteLine("Operations per executor: "
                    + operationNumberPerSequence);
                Console.WriteLine("List size: " + listSize);

                Console.WriteLine("seed: " + Seed);

                Random initializationRandom
                    = new Random(iterationParameters.InitializationSeed);
                Console.WriteLine("initial: \t"
                    + string.Join(" \t", Enumerable.Range(0, listSize).Select(
                        i => new ListItemData(i, initializationRandom.Next(
                            listItemValueRange)))));

                Console.WriteLine("lfdllResult: \t"
                    + string.Join("\t",
                        lfdllResultList.Select(o => o.ToString())));
                Console.WriteLine("linked list results: \t");
                var enumerationEqualityComparer = LinqHelper
                    .ToEqualityComparer<IEnumerable<ListItemData>>(
                        (e1, e2) => (
                            e1.Count() == e2.Count()
                            && e1.Zip(e2, (i1, i2) => i1.NodeId == i2.NodeId && i1.Value == i2.Value)
                                .All(i => i)),
                        e1 => e1.Aggregate(
                            0x51ed270b,
                            (hash, next) => (hash + next.Value.GetHashCode()) * -1521134295));
                IEnumerable<IEnumerable<ListItemData>> perms
                    = permutations(timings)
                        .Select(
                            permTimings =>
                                runOnLinkedList(
                                    iterationParameters,
                                    permTimings.Select(oei => oei.ExecutorIndex)
                                        .ToList()))
                        .Distinct(enumerationEqualityComparer);
                foreach (IEnumerable<ListItemData> sequentialResult
                    in perms)
                {
                    Console.WriteLine(string.Join("", sequentialResult.Select(value => " \t" + value)));
                }
                Console.WriteLine("startIndex-es: " + string.Join(" \t", iterationParameters.OperationSequences.Select(eParam => eParam.StartIndex)));
                Console.WriteLine("operations:");
                foreach (operationExecutionInfo timing in timings)
                {
                    Console.WriteLine("    " + timing.ExecutorIndex
                        + " \t" + timing.Start
                        + " \t" + timing.End
                        + " \t" + timing.Operation
#if RunOperationsSequentially
                        + " \t" + timing.Operation.LastResultsEqual
#endif
                        );
                }
                if (!found)
                {
                    Console.Write("?");
                    Console.ReadLine();
                    throw new Exception();
                }
            }
#endif            
        }

        public Test001()
        {
            operationComparerCreators 
                = new List<IEnumerable<Func<Random,
                    ObjectIdGenerator, Counter, IOperationResultComparer>>>
                {
                    new List<Func<ObjectIdGenerator, int, VoidOperation>>
                    {
                        (oig, seed) => new SelectRandomKnownNode(oig, seed)
                    }
                    .Select(f =>
                        (Func<Random, ObjectIdGenerator, Counter,
                            IOperationResultComparer>) (
                            (rand, oig, counter) =>
                            {
                                int seed = rand.Next();
                                return new VoidOperationComparer(f(oig, seed));
                            }
                        )
                    ),
                    new List<Func<ObjectIdGenerator, ListItemData,
                        NodeReturningOperation>>
                    {
                        (oig, v) => new PushLeft(oig, v),
                        (oig, v) => new PushRight(oig, v),
                        (oig, v) => new InsertBefore(oig, v),
                        (oig, v) => new InsertAfter(oig, v)
                    }
                    .Select(f =>
                        (Func<Random, ObjectIdGenerator, Counter,
                            IOperationResultComparer>)
                        (
                            (rand, oig, counter) =>
                                new NodeReturningOperationComparer(
                                    f(oig, nextListItemData(rand, counter)))
                        )
                    ),
                    new List<IEnumerable<Func<ObjectIdGenerator, IOperationResultComparer>>>
                    {
                        new List<Func<ObjectIdGenerator, VoidOperation>>
                        {
                            oig => new Next(oig),
                            oig => new Previous(oig)
                        }
                        .Select(f => (Func<ObjectIdGenerator, IOperationResultComparer>) (
                            oig => new VoidOperationComparer(f(oig)))),
                        new List<Func<ObjectIdGenerator, NodeReturningOperation>>
                        { oig => new PopRightNode(oig) }
                        .Select(f => (Func<ObjectIdGenerator, IOperationResultComparer>) (
                            oig => new NodeReturningOperationComparer(f(oig)))),
                        new List<Func<ObjectIdGenerator, BoolReturningOperation>>
                        { oig => new Remove(oig) }
                        .Select(f => (Func<ObjectIdGenerator, IOperationResultComparer>) (
                            oig => new BoolReturningOperationComparer(f(oig)))),
                    }
                    .SelectMany(e => e, (e, f) => 
                        (Func<Random, ObjectIdGenerator, Counter,
                            IOperationResultComparer>)
                        ((rand, oig, counter) => f(oig))
                    ),
                    new List<Func<Random, ObjectIdGenerator,
                        Counter, IOperationResultComparer>>
                    {
                        (rand, idGenerator, counter) =>
                            new NodeReturningOperationComparer(
                                new InsertAfterIf(idGenerator,
                                    nextListItemData(rand, counter),
                                    nextItemValue(rand))),
                        (rand, idGenerator, counter) =>
                            new ItemDataReturningOperationComparer(
                                new CompareExchangeValue(idGenerator,
                                    nextItemValue(rand), nextItemValue(rand))),
                        (rand, idGenerator, counter) =>
                            new ItemDataReturningOperationComparer(
                                new GetValue(idGenerator)),
                        (rand, idGenerator, counter) =>
                            new VoidOperationComparer(
                                new SetValue(idGenerator, nextItemValue(rand)))
                    }
                }
                .SelectMany(e => e)
                .ToList();
        }

        private const int listItemValueRange = 3;

        private readonly List<Func<Random, ObjectIdGenerator, Counter, IOperationResultComparer>>
            operationComparerCreators;

        private ListItemData nextListItemData(Random rand, Counter counter)
        {
            return new ListItemData(counter.Count(), nextItemValue(rand));
        }

        private int nextItemValue(Random rand)
        {
            return rand.Next(listItemValueRange);
        }

        private IEnumerable<List<operationExecutionInfo>> permutations(
            List<operationExecutionInfo> executionInfos)
        {
            int i;
            
            /* Sortieren nach Startzeitpunkt */
            executionInfos.Sort(
                (t1, t2) => t1.Start.CompareTo(t2.Start));

            var permutation =
                new LinkedList<operationExecutionInfo>(executionInfos);

            yield return permutation.ToList();

            var nodes =
                new LinkedListNode<operationExecutionInfo>
                    [executionInfos.Count];
            LinkedListNode<operationExecutionInfo> node = permutation.First;
            for (i = 0; i < executionInfos.Count; i++)
            {
                nodes[i] = node;
                // ReSharper disable once PossibleNullReferenceException
                node = node.Next;
            }

            i = permutation.Count - 1;
            LinkedListNode<operationExecutionInfo> lastNode = nodes[i];
            node = lastNode;
            bool seen = true;
            while (true)
            {
                /* node ist der Knoten mit dem höchsten Startzeitpunkt,
                 * der sich in der Liste befindet, falls er sich in der Liste befindet. */
                if (seen)
                {
                    LinkedListNode<operationExecutionInfo> prev = node.Previous;
                    if (prev == null || prev.Value.End < node.Value.Start)
                    {
                        permutation.Remove(node);
                        i--;
                        if (i == 0)
                            yield break;
                        node = nodes[i];
                    }
                    else
                    {
                        permutation.Remove(node);
                        permutation.AddBefore(prev, node);
                        if (node == lastNode)
                            yield return permutation.ToList();
                        else
                            seen = false;
                    }
                }
                else
                {
                    if (node == lastNode)
                    {
                        yield return permutation.ToList();
                        seen = true;
                    }
                    else
                    {
                        i++;
                        node = nodes[i];
                        permutation.AddLast(node);
                    }
                }
            }
        }

        private Tuple<List<List<operationTiming>>,
            LockFreeDoublyLinkedList<ListItemData>>
            runOnLfdll(TestIterationParameters parameters)
        {
            LockFreeDoublyLinkedList<ListItemData> lfdll
                = new LockFreeDoublyLinkedList<ListItemData>();

            Random initializationRandom
                = new Random(parameters.InitializationSeed);

            foreach (int o in Enumerable.Range(0, parameters.InitialListLength))
                lfdll.PushRight(
                    new ListItemData(
                        o, initializationRandom.Next(listItemValueRange)));

            var timer = new Counter();

            List<lfdllOperationExecutor> executors =
                parameters.OperationSequences.Select(
                    (operationParams, name) =>
                        new lfdllOperationExecutor(operationParams, lfdll,
                            timer, name)).ToList();

            foreach (lfdllOperationExecutor executor in executors)
                executor.Initialize();

#if SynchronizedLFDLL
            List<AutoResetEvent> nextStepWaitHandles = executors.Select(
                executor => new AutoResetEvent(false)).ToList();
#endif

            var executorTasks = new List<Task<List<operationTiming>>>();
            for (int i = 0; i < executors.Count; i++)
            {
                lfdllOperationExecutor executor = executors[i];
#if SynchronizedLFDLL
                AutoResetEvent nextStepWaitHandle = nextStepWaitHandles[i];
#endif
                Task<List<operationTiming>> task = new Task<List<operationTiming>>(
                    () =>
                    {
                        List<operationTiming> timings;
#if HandleTaskExceptionImmediately
                        try
                        {
#endif
#if SynchronizedLFDLL
                            lfdll.NextStepWaitHandle.Value
                                = nextStepWaitHandle;
#endif
                            timings = executor.Run();
#if SynchronizedLFDLL
                            lfdll.NextStepWaitHandle.Value.WaitOne();
                            nextStepWaitHandles.Remove(
                                lfdll.NextStepWaitHandle.Value);
                            lfdll.StepCompletedWaitHandle.Set();
#endif
#if HandleTaskExceptionImmediately
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.StackTrace);
                            Console.WriteLine("Exception in Thread " + Thread.CurrentThread.Name);
                            throw;
                        }
#endif
                        return timings;
                    });
                executorTasks.Add(task);
            }

            foreach (Task<List<operationTiming>> task in executorTasks)
            {
#if RunOperationsSequentially
                task.RunSynchronously();
#else
                task.Start();
#endif
            }

            Random executionRandom = new Random(parameters.ExecutionSeed);
#if SynchronizedLFDLL_Verbose
            List<int> handleIndexes = new List<int>();
#endif

#if SynchronizedLFDLL
            while (true)
            {
                if (nextStepWaitHandles.Count == 0)
                    break;
                int nextHandleIndex = executionRandom.Next(nextStepWaitHandles.Count);
#if SynchronizedLFDLL_Verbose
                handleIndexes.Add(nextHandleIndex);
#endif

                AutoResetEvent nextHandle
                    = nextStepWaitHandles[nextHandleIndex];
                nextHandle.Set();
                lfdll.StepCompletedWaitHandle.WaitOne();
            }
#endif

            // ReSharper disable once CoVariantArrayConversion
            Task.WaitAll(executorTasks.ToArray());

#if SynchronizedLFDLL_Verbose
            Console.WriteLine(string.Join(" ", handleIndexes));
#endif

            return new Tuple<List<List<operationTiming>>,
                LockFreeDoublyLinkedList<ListItemData>>(
                executorTasks.Select(t => t.Result).ToList(), lfdll);
        }

        private IEnumerable<ListItemData> runOnLinkedList(
            TestIterationParameters parameters,
            List<int> executorStepOrder)
        {
            Random initializationRandom =
                new Random(parameters.InitializationSeed);

            var list = new LinkedList<LinkedListItem>(
                Enumerable.Range(0, parameters.InitialListLength)
                    .Select(
                        i =>
                            new LinkedListItem(
                                new ListItemData(
                                    i,
                                    initializationRandom.Next(
                                        listItemValueRange)))));

            List<linkedListOperationExecutor> executors =
                parameters.OperationSequences.Select(
                    eParams => new linkedListOperationExecutor(eParams, list))
                    .ToList();

            foreach (linkedListOperationExecutor executor in executors)
                executor.Initialize();

            for (int i = 0; i < executorStepOrder.Count; i++)
            {
                int executorIndex = executorStepOrder[i];
                executors[executorIndex].SingleStep();
            }

            return list.Where(value => !value.Deleted).Select(item => item.Data);
        }

        private TestIterationParameters newIterationParameters(int listSize, int operationSequencesNumber, int operationNumberPerSequence, Random rand)
        {
            var counter = new Counter(listSize);
            var idGenerator = new ObjectIdGenerator();
            return new TestIterationParameters()
            {
                InitialListLength = listSize,
                OperationSequences = LinqHelper.Repeat(
                        operationSequencesNumber,
                        () => new ExecutionSequenceParameters
                        {
                            StartIndex = rand.Next(listSize),
                            Operations =
                                LinqHelper.Repeat(
                                    operationNumberPerSequence,
                                    () => newRandomOperationResultComparer(
                                        rand, counter, idGenerator)).ToList(),
                        }
                    )
                    .ToList(),
                InitializationSeed = rand.Next(),
                ExecutionSeed = rand.Next(),
            };
        }

        private IOperationResultComparer newRandomOperationResultComparer
            (Random rand2, Counter c, ObjectIdGenerator idGenerator)
        {
            return operationComparerCreators[
                rand2.Next(operationComparerCreators.Count)](
                    rand2, idGenerator, c);
        }

        private static Tuple<long, long> measureTime(Action action, Counter counter)
        {
            long start = counter.Count();
            action();
            long end = counter.Count();

            return new Tuple<long, long>(start, end);
        }

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

        private class lfdllOperationExecutor
        {
            public int Name { get; private set; }
            
            /* Should be executed before a
             * LFDLLOperationExecutor modifies list. */
            public void Initialize()
            {
                LockFreeDoublyLinkedList<ListItemData>.INode current =
                    state.List.Head;
                for (int i = 0; i < eParams.StartIndex + 1; i++)
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

            private Tuple<long, long> processOperation(
                IOperationResultComparer op, Counter counter)
            {
#if SynchronizedLFDLL_Verbose
                Console.WriteLine("({0}) Nächste Operation: {1}", Name, op);
                Console.WriteLine("({0}) Aktueller Knoten:", Name);
                state.List.LogNode(state.Current);
#endif

                Tuple<long, long> timing = measureTime(
                    () =>
                    {
                        op.RunOnLfdll(state);
                    },
                    counter);

#if SynchronizedLFDLL_Verbose
                Console.WriteLine("({0}) Beendete Operation: {1}", Name, op);
#endif

                return timing;
            }

            public lfdllOperationExecutor(
                ExecutionSequenceParameters eParams,
                LockFreeDoublyLinkedList<ListItemData> list, Counter counter,
                int name)
            {
                this.eParams = eParams;
                this.counter = counter;
                Name = name;
                state = new LfdllExecutionState(list);
            }

            private ExecutionSequenceParameters eParams;
            private LfdllExecutionState state;
            private Counter counter;
        }

        private class linkedListOperationExecutor
        {
            public void Initialize()
            {
                LinkedListNode<LinkedListItem> current = state.List.First;
                for (int i = 0; i < eParams.StartIndex; i++)
                {
                    // ReSharper disable once PossibleNullReferenceException
                    current = current.Next;
                }
                state.Current = current;
            }

            public void SingleStep()
            {
                IOperationResultComparer operation
                    = eParams.Operations[nextOperation];
                operation.RunOnLinkedList(state);
                nextOperation++;
            }

            public linkedListOperationExecutor(
                ExecutionSequenceParameters eParams,
                LinkedList<LinkedListItem> list)
            {
                this.state = state = new LinkedListExecutionState(list);
                this.eParams = eParams;
            }

            private ExecutionSequenceParameters eParams;
            private LinkedListExecutionState state;
            private int nextOperation = 0;
        }

        private class operationExecutionInfo
        {
            public readonly int ExecutorIndex;
            public readonly IOperationResultComparer Operation;
            public readonly long Start, End;

            public operationExecutionInfo(
                operationTiming operationTiming, int executorIndex)
            {
                ExecutorIndex = executorIndex;
                Operation = operationTiming.Operation;
                Start = operationTiming.Start;
                End = operationTiming.End;
            }
        }
    }
}