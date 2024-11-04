using System.Collections.Generic;
using UnityEngine;

namespace Xy01.NodeGraph
{
    /// <summary>
    /// Base class for all runtime nodes
    /// </summary>
    public abstract class RuntimeNode
    {
        /// <summary>
        /// Unique identifier for this node
        /// </summary>
        public string Guid { get; set; }
        /// <summary>
        /// All inputs to this node
        /// TODO - currently these are just floats, but could be extended to hold other types in future
        /// </summary>
        protected Dictionary<string, float> InputValues = new();
        /// <summary>
        /// A dictionary of all output connections from this node to other nodes
        /// As each port can connect to multiple inputs on other nodes it holds a list of other nodes it connects too and the name of the port it connects from
        /// </summary>
        protected internal Dictionary<string, List<(RuntimeNode node, string portName)>> OutputConnections = new();

        public virtual void ProcessNode()
        {
            // Override in derived classes
        }

        public void SetInputValue(string portName, float value)
        {
            InputValues[portName] = value;
        }

        protected void PropagateOutput(string portName, float value)
        {
            if (OutputConnections.TryGetValue(portName, out var connections))
            {
                foreach (var (node, inputPort) in connections)
                {
                    node.SetInputValue(inputPort, value);
                }
            }
        }
    }
}
