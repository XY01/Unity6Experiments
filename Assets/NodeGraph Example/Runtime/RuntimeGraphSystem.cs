using UnityEngine;
using System;
using System.Collections.Generic;

namespace Xy01.NodeGraph
{
// Serializable graph data
    [CreateAssetMenu(fileName = "New Graph", menuName = "Custom/Runtime Graph")]
    public class RuntimeGraph : ScriptableObject
    {
        [Serializable]
        public class NodeData
        {
            public string Guid;
            public string Type;
            public Vector2 Position;
            public List<ConnectionData> Connections = new();
            public Material Material; // For material nodes
        }

        [Serializable]
        public class ConnectionData
        {
            public string OutputNodeGuid;
            public string InputNodeGuid;
            public string OutputPortName;
            public string InputPortName;
        }

        public List<NodeData> Nodes = new();
    }

// Runtime node base class
    public abstract class RuntimeNode
    {
        public string Guid { get; set; }
        protected Dictionary<string, float> InputValues = new();

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