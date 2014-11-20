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
//#define HandleTaskExceptionImmediately
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
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using LockFreeDoublyLinkedList;

namespace Test.Tests
{
    internal class Test001 : Test
    {
        public override void Main(string[] args)
        {
            int operationSequencesNumber = 3;
            const int operationNumberPerSequence = 4; 
            const int listSize = 4;

            List<int> initial = Enumerable
                .Range(0, listSize)
                .ToList();

            //Seed = -1182330010;
            //Seed = 1136758711;
            Random rand2 = new Random(Seed);

            List<executorParams> eParamsList = Enumerable
                .Range(0, operationSequencesNumber)
                .Select(i => i * operationNumberPerSequence + listSize)
                .Select(i => Enumerable
                    .Range(0, operationNumberPerSequence)
                    .Select(j => newRandomOperation(i + j, rand2))
                    .ToList())
                .Select(
                    os =>
                        new executorParams(os,
                            rand2.Next(listSize)))
                .ToList();

#if Verbose
            Console.WriteLine("Test of LockFreeDoublyLinkedList");

            Console.WriteLine("Number of executors (Threads): " + operationSequencesNumber);
            Console.WriteLine("Operations per executor: "
                + operationNumberPerSequence);
            Console.WriteLine("List size: " + listSize);

            Console.WriteLine("seed: " + Seed);

            Console.WriteLine("initial:        "
                + string.Join("\t", initial.Select(i => i.ToString(CultureInfo.InvariantCulture))));
            Console.WriteLine("startIndex-es: "
                + string.Join(" \t", eParamsList.Select(eParam => eParam.StartIndex)));

            Console.WriteLine("operations: ");
            for (int i = 0; i < eParamsList.Count; i++)
            {
                executorParams eParamse = eParamsList[i];
                Console.WriteLine("\t{0}", i);
                foreach (operation op in eParamse.Operations)
                {
                    Console.WriteLine("\t\t{0}", op);
                }
            }
#endif

#if SynchronizedLFDLL
            Tuple<List<List<operationTiming>>, LockFreeDoublyLinkedList<object>> lfdllResult
                = runOnLfdll(initial, eParamsList, rand2);
#else
            Tuple<List<List<operationTiming>>, LockFreeDoublyLinkedList<object>> lfdllResult
                = runOnLfdll(initial, eParamsList);
#endif

            List<object> lfdllResultList = lfdllResult.Item2.ToList();

            List<operationExecutionInfo> timings
                = lfdllResult.Item1
                    .SelectMany((opTimings, i) => opTimings.Select(
                        opTiming => new operationExecutionInfo(opTiming, i)))
                    .ToList();

#if CheckCorrectness
            if (permutations(timings)
                .Select(permTimings =>
                    runOnLinkedList(initial, eParamsList,
                    permTimings.Select(oei => oei.ExecutorIndex).ToList()))
                .Any(sequentialResult =>
                    sequentialResult.SequenceEqual(lfdllResultList)))
            {
#if Verbose
                Console.WriteLine("Gefunden.");
#endif
            }
            else
            {
                Console.WriteLine("Test of LockFreeDoublyLinkedList");

                Console.WriteLine("Number of executors (Threads): " + operationSequencesNumber);
                Console.WriteLine("Operations per executor: "
                    + operationNumberPerSequence);
                Console.WriteLine("List size: " + listSize);

                Console.WriteLine("seed: " + Seed);

                Console.WriteLine("initial:        "
                    + string.Join("\t", initial.Select(i => i.ToString(CultureInfo.InvariantCulture))));
                Console.WriteLine("lfdllResult: "
                    + string.Join("\t",
                        lfdllResultList.Select(o => o.ToString())));
                Console.WriteLine("linked list results:");
                foreach (IEnumerable<object> sequentialResult in
                    permutations(timings)
                        .Select(permTimings =>
                            runOnLinkedList(initial, eParamsList,
                            permTimings.Select(oei => oei.ExecutorIndex).ToList()))
                        .Distinct(LinqHelper.ToEqualityComparer<IEnumerable<object>>(
                            (e1, e2) => e1.SequenceEqual(e2),
                            e1 => e1.Aggregate(0x51ed270b,
                                (hash, next) => hash * -1521134295))))
                {
                    Console.WriteLine(string.Join("", sequentialResult.Select(value => " \t" + value)));
                }
                Console.WriteLine("startIndex-es: " + string.Join(" \t", eParamsList.Select(eParam => eParam.StartIndex)));
                Console.WriteLine("operations:");
                foreach (operationExecutionInfo timing in timings)
                {
                    Console.WriteLine("   " + timing.ExecutorIndex
                        + " \t" + timing.Start
                        + " \t" + timing.End +
                        " \t" + timing.Operation);
                }
                Console.Write("?");
                Console.ReadLine();
                throw new Exception();
            }
#endif            
        }

        private readonly Func<int, operation>[] operationFactories =
        {
            insertAfter.Create,
            insertBefore.Create,
            remove.Create,
            next.Create,
            previous.Create,
            pushLeft.Create,
            pushRight.Create,
//            popLeft.Create,
            popRight.Create
        };

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

        private Tuple<List<List<operationTiming>>, LockFreeDoublyLinkedList<object>> runOnLfdll(
            IEnumerable<int> initial, List<executorParams> operationParamses
#if SynchronizedLFDLL
, Random random
#endif
)
        {
            LockFreeDoublyLinkedList<object> lfdll
                = new LockFreeDoublyLinkedList<object>();
            foreach (int o in initial)
                lfdll.PushRight(o);

            var counter = new Counter();

            List<lfdllOperationExecutor> executors =
                operationParamses.Select(
                    (operationParams, name) =>
                        new lfdllOperationExecutor(operationParams, lfdll,
                            counter, name)).ToList();

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

#if SynchronizedLFDLL
            while (true)
            {
                if (nextStepWaitHandles.Count == 0)
                    break;
                int nextHandleIndex = random.Next(nextStepWaitHandles.Count);

                AutoResetEvent nextHandle
                    = nextStepWaitHandles[nextHandleIndex];
                nextHandle.Set();
                lfdll.StepCompletedWaitHandle.WaitOne();
            }
#endif

            // ReSharper disable once CoVariantArrayConversion
            Task.WaitAll(executorTasks.ToArray());

            return new Tuple<List<List<operationTiming>>, LockFreeDoublyLinkedList<object>>(
                executorTasks.Select(t => t.Result).ToList(), lfdll);
        }

        private IEnumerable<object> runOnLinkedList(List<int> initial,
            List<executorParams> executionParams,
            List<int> executorStepOrder)
        {
            var list = new LinkedList<object>(initial.Cast<object>());

            List<linkedListOperationExecutor> executors =
                executionParams.Select(
                    eParams => new linkedListOperationExecutor(eParams, list))
                    .ToList();

            foreach (linkedListOperationExecutor executor in executors)
                executor.Initialize();

            foreach (int executorIndex in executorStepOrder)
                executors[executorIndex].SingleStep();

            return list.Where(value => value != null);
        }

        //private int findFirstByBinarySearch<T>(this List<T> list, Func<T, bool> comparison, int start)
        //{
        //    int lower = 0;
        //    int upper = list.Count;
        //    while (true)
        //    {
        //        if (lower == upper)
        //            return lower;
        //        if (comparison(list[start]))
        //            upper = start;
        //        else
        //            lower = start + 1;
        //        start = (lower + upper) / 2;
        //    }
        //}

        private operation newRandomOperation(int newNodeValue, Random rand2)
        {
            return operationFactories
                [rand2.Next(operationFactories.Length)]
                .Invoke(newNodeValue);
        }

        //private class 

        private static Tuple<long, long> measureTime(Action action, Counter counter)
        {
            long start = counter.Count();
            action();
            long end = counter.Count();

            return new Tuple<long, long>(start, end);
        }

        private class operationTiming
        {
            public readonly operation Operation;
            public readonly long Start, End;

            public operationTiming(operation operation,
                Tuple<long, long> timing)
            {
                Operation = operation;
                Start = timing.Item1;
                End = timing.Item2;
            }
        }

        private class executorParams
        {
            public List<operation> Operations { get; private set; }
            public int StartIndex { get; private set; }

            public executorParams(
                List<operation> operations, int startIndex)
            {
                Operations = operations;
                StartIndex = startIndex;
            }
        }

        private class lfdllOperationExecutor
        {
            public int Name { get; private set; }
            
            /* Should be executed before a
             * LFDLLOperationExecutor modifies list. */
            public void Initialize()
            {
                current = list.Head;
                for (int i = 0; i < eParams.StartIndex + 1; i++)
                    current = current.Next;
            }

            public List<operationTiming> Run()
            {
                Thread.CurrentThread.Name = Name.ToString();
                return eParams.Operations
                    .Select(operation =>
                        new operationTiming(
                            operation,
                            processOperation(operation, counter)))
                    .ToList();
            }

            private Tuple<long, long> processOperation(operation op, Counter counter)
            {
#if SynchronizedLFDLL_Verbose
                Console.WriteLine("({0}) Nächste Operation: {1}", Name, op);
                Console.WriteLine("({0}) Aktueller Knoten:", Name);
                list.LogNode(current);
#endif

                Tuple<long, long> timing = measureTime(
                    () =>
                    {
                        op.RunOnLfdll(list, ref current);
                    },
                    counter);

#if SynchronizedLFDLL_Verbose
                Console.WriteLine("({0}) Beendete Operation: {1}", Name, op);
#endif

                return timing;
            }

            public lfdllOperationExecutor(
                executorParams eParams,
                LockFreeDoublyLinkedList<object> list, Counter counter,
                int name)
            {
                this.eParams = eParams;
                this.counter = counter;
                this.list = list;
                this.Name = name;
            }

            private executorParams eParams;
            private LockFreeDoublyLinkedList<object> list;
            private LockFreeDoublyLinkedList<object>.INode current;
            private Counter counter;
        }

        private class linkedListOperationExecutor
        {
            public void Initialize()
            {
                current = list.First;
                for (int i = 0; i < eParams.StartIndex; i++)
                {
                    // ReSharper disable once PossibleNullReferenceException
                    current = current.Next;
                }
            }

            public void SingleStep()
            {
                operation operation
                    = eParams.Operations[nextOperation];
                operation.RunOnLinkedList(list, ref current);
                nextOperation++;
            }

            public linkedListOperationExecutor(
                executorParams eParams,
                LinkedList<object> list)
            {
                this.eParams = eParams;
                this.list = list;
            }

            private executorParams eParams;
            private LinkedList<object> list;
            private int nextOperation = 0;
            private LinkedListNode<object> current;
        }

        private class operationExecutionInfo
        {
            public readonly int ExecutorIndex;
            public readonly operation Operation;
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

        private abstract class operation
        {
            public abstract void RunOnLinkedList(
                LinkedList<object> list,
                ref LinkedListNode<object> current);

            public abstract void RunOnLfdll(
                LockFreeDoublyLinkedList<object> list,
                ref LockFreeDoublyLinkedList<object>.INode current);

            public override string ToString()
            {
                return GetType().Name;
            }
        }

        private class insertAfter : operation
        {
            public override void RunOnLinkedList(
                LinkedList<object> list, ref LinkedListNode<object> current)
            {
                if (current != null)
                {
                    if (current.Value == null)
                    {
                        LinkedListNode<object> prev = current;
                        while (prev.Previous != null &&
                               prev.Previous.Value == null)
                        {
                            prev = prev.Previous;
                        }
                        list.AddBefore(prev, value);
                    }
                    else
                        list.AddAfter(current, value);
                }
            }

            public override void RunOnLfdll(
                LockFreeDoublyLinkedList<object> list, ref LockFreeDoublyLinkedList<object>.INode current)
            {
                if (current != null)
                    current.InsertAfter(value);
            }

            public static operation Create(int value)
            {
                return new insertAfter(value);
            }

            public override string ToString()
            {
                return base.ToString() + " " + value;
            }

            private readonly int value;

            private insertAfter(int value)
            {
                this.value = value;
            }
        }

        private class insertBefore : operation
        {
            public override void RunOnLinkedList(
                LinkedList<object> list, ref LinkedListNode<object> current)
            {
                if (current != null)
                {
                    LinkedListNode<object> prev = current;
                    while (prev.Previous != null &&
                            prev.Previous.Value == null)
                    {
                        prev = prev.Previous;
                    }
                    list.AddBefore(prev, value);
                }
            }

            public override void RunOnLfdll(
                LockFreeDoublyLinkedList<object> list, ref LockFreeDoublyLinkedList<object>.INode current)
            {
                if (current != null)
                    current.InsertBefore(value);
            }

            public static operation Create(int value)
            {
                return new insertBefore(value);
            }

            public override string ToString()
            {
                return base.ToString() + " " + value;
            }

            private readonly int value;

            private insertBefore(int value)
            {
                this.value = value;
            }
        }

        private class remove : operation
        {
            public override void RunOnLinkedList(
                LinkedList<object> list, ref LinkedListNode<object> current)
            {
                if (current != null)
                    current.Value = null;
            }

            public override void RunOnLfdll(
                LockFreeDoublyLinkedList<object> list, ref LockFreeDoublyLinkedList<object>.INode current)
            {
                if (current != null)
                    current.Remove();
            }

            public static operation Create(int value)
            {
                return new remove();
            }
        }

        private class next : operation
        {
            public override void RunOnLinkedList(
                LinkedList<object> list, ref LinkedListNode<object> current)
            {
                if (current == null)
                {
                    current = list.Last;
                    while (current != null && current.Value == null)
                        current = current.Previous;
                }
                else
                {
                    do
                        current = current.Next;
                    while (current != null && current.Value == null);
                }
            }

            public override void RunOnLfdll(
                LockFreeDoublyLinkedList<object> list, ref LockFreeDoublyLinkedList<object>.INode current)
            {
                if (current == null)
                {
                    current = list.Tail.Prev;
                    if (current == list.Head)
                        current = null;
                }
                else
                {
                    current = current.Next;
                    if (current == list.Tail)
                        current = null;
                }
            }

            public static operation Create(int value)
            {
                return new next();
            }
        }

        private class previous : operation
        {
            public override void RunOnLinkedList(
                LinkedList<object> list, ref LinkedListNode<object> current)
            {
                if (current == null)
                {
                    current = list.First;
                    while (current != null && current.Value == null)
                        current = current.Next;
                }
                else
                {
                    do
                        current = current.Previous;
                    while (current != null && current.Value == null);
                }
            }

            public override void RunOnLfdll(
                LockFreeDoublyLinkedList<object> list, ref LockFreeDoublyLinkedList<object>.INode current)
            {
                if (current == null)
                {
                    current = list.Head.Next;
                    if (current == list.Tail)
                        current = null;
                }
                else
                {
                    current = current.Prev;
                    if (current == list.Head)
                        current = null;
                }
            }

            public static operation Create(int value)
            {
                return new previous();
            }
        }

        private class pushLeft : operation
        {
            public override void RunOnLinkedList(
                LinkedList<object> list, ref LinkedListNode<object> current)
            {
                list.AddFirst(value);
            }

            public override void RunOnLfdll(
                LockFreeDoublyLinkedList<object> list, ref LockFreeDoublyLinkedList<object>.INode current)
            {
                list.PushLeft(value);
            }

            public static operation Create(int value)
            {
                return new pushLeft(value);
            }

            public override string ToString()
            {
                return base.ToString() + " " + value;
            }

            private readonly int value;

            private pushLeft(int value)
            {
                this.value = value;
            }
        }

        private class pushRight : operation
        {
            public override void RunOnLinkedList(
                LinkedList<object> list, ref LinkedListNode<object> current)
            {
                LinkedListNode<object> prev = list.Last;
                if (prev == null || prev.Value != null)
                {
                    list.AddLast(value);
                    return;
                }
                while (prev.Previous != null &&
                       prev.Previous.Value == null)
                {
                    prev = prev.Previous;
                }
                list.AddBefore(prev, value);
            }

            public override void RunOnLfdll(
                LockFreeDoublyLinkedList<object> list, ref LockFreeDoublyLinkedList<object>.INode current)
            {
                list.PushRight(value);
            }

            public static operation Create(int value)
            {
                return new pushRight(value);
            }

            public override string ToString()
            {
                return base.ToString() + " " + value;
            }

            private readonly int value;

            private pushRight(int value)
            {
                this.value = value;
            }
        }

#if PopLeft
        private class popLeft : operation
        {
            // ReSharper disable once RedundantAssignment
            public override void RunOnLinkedList(
                LinkedList<object> list, ref LinkedListNode<object> current)
            {
                LinkedListNode<object> first = list.First;
                while (first != null && first.Value == null)
                    first = first.Next;
                if (first != null)
                    first.Value = null;
            }

            public override void RunOnLfdll(
                LockFreeDoublyLinkedList<object> list,
                ref LockFreeDoublyLinkedList<object>.INode current)
            {
                list.PopLeft();
            }

            public static operation Create(int value)
            {
                return new popLeft();
            }
        }
#endif

        private class popRight : operation
        {
            // ReSharper disable once RedundantAssignment
            public override void RunOnLinkedList(
                LinkedList<object> list, ref LinkedListNode<object> current)
            {
                LinkedListNode<object> last = list.Last;
                while (last != null && last.Value == null)
                    last = last.Previous;
                if (last != null)
                    last.Value = null;
            }

            public override void RunOnLfdll(
                LockFreeDoublyLinkedList<object> list, ref LockFreeDoublyLinkedList<object>.INode current)
            {
                list.PopRight();
            }

            public static operation Create(int value)
            {
                return new popRight();
            }
        }
    }
}