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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LockFreeDoublyLinkedList;

namespace Test.Tests.Test001_
{
	internal class LinkedListExecutionState
        : ExecutionState<LinkedList<LinkedListItem>,
            LinkedListNode<LinkedListItem>> 
    {
	    public LinkedListExecutionState(
            LinkedList<LinkedListItem> list)
            : base(list)
        {
        }
    }

	internal class LfdllExecutionState
        : ExecutionState<ILockFreeDoublyLinkedList<ListItemData>,
            ILockFreeDoublyLinkedListNode<ListItemData>> 
    {
	    public LfdllExecutionState(
            ILockFreeDoublyLinkedList<ListItemData> list)
            : base(list)
        {
        }
    }

	internal abstract class ExecutionState<ListT, NodeT> : IExecutionState<NodeT>
    {
	    public ListT List { get; private set; }
	    public List<NodeT> KnownNodes { get; private set; }

	    public ICollection<NodeT> KnownNodesCollection
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

	internal interface IExecutionState<NodeT>
    {
	    ICollection<NodeT> KnownNodesCollection { get; }
	    int CurrentIndex { get; set; }
    }
}
