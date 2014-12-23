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

using LockFreeDoublyLinkedList;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Test.Tests.Test001_
{
    class LinkedListExecutionState
        : ExecutionState<LinkedList<LinkedListItem>,
            LinkedListNode<LinkedListItem>> 
    {
        public LinkedListExecutionState(
            LinkedList<LinkedListItem> list)
            : base(list)
        {
        }
    }

    class LfdllExecutionState
        : ExecutionState<LockFreeDoublyLinkedList<ListItemData>,
            LockFreeDoublyLinkedList<ListItemData>.INode> 
    {
        public LfdllExecutionState(
            LockFreeDoublyLinkedList<ListItemData> list)
            : base(list)
        {
        }
    }

    abstract class ExecutionState<ListT, NodeT> : IExecutionState
    {
        public ListT List { get; private set; }
        public List<NodeT> KnownNodes { get; private set; }

        public ICollection KnownNodesCollection
        {
            get { return KnownNodes; }
        }

        public int CurrentIndex { get; set; }

        /// <summary>
        /// Adds a node to the list of known nodes.
        /// </summary>
        /// <param name="node">The node to add.</param>
        /// <returns>
        /// The index of the node in the list of known nodes
        /// after the addition.
        /// </returns>
        public int AddToKnownNodes(NodeT node)
        {
            int i = KnownNodes.IndexOf(node);
            if (i < 0)
            {
                i = KnownNodes.Count;
                KnownNodes.Add(node);
            }
            return i;
        }

        /// <summary>
        /// Adds a node to the list of known nodes
        /// and returns the same node.
        /// </summary>
        /// <param name="node">The node to add.</param>
        /// <returns>The node to add.</returns>
        public NodeT AddingToKnownNodes(NodeT node)
        {
            AddToKnownNodes(node);
            return node;
        }

        public NodeT Current
        {
            get { return KnownNodes[CurrentIndex]; }
            set { CurrentIndex = AddToKnownNodes(value); }
        }

        public ExecutionState(ListT list)
        {
            List = list;
            KnownNodes = new List<NodeT>();
        }
    }

    interface IExecutionState
    {
        ICollection KnownNodesCollection { get; }
        int CurrentIndex { get; set; }
    }
}
