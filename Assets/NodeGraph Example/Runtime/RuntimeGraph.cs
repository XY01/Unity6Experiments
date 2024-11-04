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
            public float FloatValue; // For material nodes
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
}