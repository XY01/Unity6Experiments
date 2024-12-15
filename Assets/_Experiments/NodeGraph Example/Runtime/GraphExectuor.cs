
using System.Collections.Generic;
using UnityEngine;

namespace Xy01.NodeGraph
{
    /// <summary>
    /// Executes a node graph at runtime
    /// </summary>
    [ExecuteInEditMode]
    public class GraphExecutor : MonoBehaviour
    {
        bool CanExecute => Graph != null;
        
        public RuntimeGraph Graph;
        private Dictionary<string, RuntimeNode> _runtimeNodes = new();

        void Start()
        {
            if (!CanExecute) return;
            
            InitializeGraph();
            
        }

        void Update()
        {
            if (!CanExecute) return;
            
            ExecuteGraph();
        }

        private void InitializeGraph()
        {
            // Create runtime nodes
            foreach (var nodeData in Graph.Nodes)
            {
                RuntimeNode node = CreateNode(nodeData);
                if (node != null)
                {
                    node.Guid = nodeData.Guid;
                    _runtimeNodes[nodeData.Guid] = node;
                }
            }

            // Set up connections
            foreach (var nodeData in Graph.Nodes)
            {
                foreach (var connection in nodeData.Connections)
                {
                    if (_runtimeNodes.TryGetValue(connection.OutputNodeGuid, out var outputNode) &&
                        _runtimeNodes.TryGetValue(connection.InputNodeGuid, out var inputNode))
                    {
                        outputNode.OutputConnections.TryGetValue(connection.OutputPortName, out var connections);
                        if (connections == null)
                        {
                            connections = new List<(RuntimeNode, string)>();
                            outputNode.OutputConnections[connection.OutputPortName] = connections;
                        }

                        connections.Add((inputNode, connection.InputPortName));
                    }
                }
            }
        }

        private RuntimeNode CreateNode(RuntimeGraph.NodeData data)
        {
            switch (data.Type)
            {
                case "TimeNode":
                    return new RuntimeTimeNode();
                case "SinWaveNode":
                    return new RuntimeSinWaveNode();
                case "ClampNode":
                    return new RuntimeClampNode();
                case "MaterialPropertyNode":
                    var matNode = new RuntimeMaterialPropertyNode();
                    matNode.Initialize(data.Material);
                    return matNode;
                case "FloatValueNode":
                    var floatNode = new RuntimeFloatValueNode();
                    floatNode.Initialize(data.FloatValue);
                    return floatNode;
                default:
                    return null;
            }
        }

        private void ExecuteGraph()
        {
            // Execute all nodes (order matters, so we start with time nodes)
            foreach (var node in _runtimeNodes.Values)
            {
                if (node is RuntimeTimeNode)
                {
                    node.ProcessNode();
                }
            }

            // Then process the rest
            foreach (var node in _runtimeNodes.Values)
            {
                if (!(node is RuntimeTimeNode))
                {
                    node.ProcessNode();
                }
            }
        }
    }
}