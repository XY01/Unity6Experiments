
// Runtime graph executor

using System.Collections.Generic;
using UnityEngine;
using UnityEngine;
using System;
using System.Collections.Generic;
public class GraphExecutor : MonoBehaviour
{
    public RuntimeGraph graph;
    private Dictionary<string, RuntimeNode> runtimeNodes = new Dictionary<string, RuntimeNode>();
    
    void Start()
    {
        if (graph != null)
        {
            InitializeGraph();
        }
    }

    void Update()
    {
        ExecuteGraph();
    }

    private void InitializeGraph()
    {
        // Create runtime nodes
        foreach (var nodeData in graph.nodes)
        {
            RuntimeNode node = CreateNode(nodeData);
            if (node != null)
            {
                node.Guid = nodeData.guid;
                runtimeNodes[nodeData.guid] = node;
            }
        }

        // Set up connections
        foreach (var nodeData in graph.nodes)
        {
            foreach (var connection in nodeData.connections)
            {
                if (runtimeNodes.TryGetValue(connection.outputNodeGuid, out var outputNode) &&
                    runtimeNodes.TryGetValue(connection.inputNodeGuid, out var inputNode))
                {
                    outputNode.outputConnections.TryGetValue(connection.outputPortName, out var connections);
                    if (connections == null)
                    {
                        connections = new List<(RuntimeNode, string)>();
                        outputNode.outputConnections[connection.outputPortName] = connections;
                    }
                    connections.Add((inputNode, connection.inputPortName));
                }
            }
        }
    }

    private RuntimeNode CreateNode(RuntimeGraph.NodeData data)
    {
        switch (data.type)
        {
            case "TimeNode":
                return new RuntimeTimeNode();
            case "SinWaveNode":
                return new RuntimeSinWaveNode();
            case "MaterialPropertyNode":
                var matNode = new RuntimeMaterialPropertyNode();
                matNode.Initialize(data.material);
                return matNode;
            default:
                return null;
        }
    }

    private void ExecuteGraph()
    {
        // Execute all nodes (order matters, so we start with time nodes)
        foreach (var node in runtimeNodes.Values)
        {
            if (node is RuntimeTimeNode)
            {
                node.ProcessNode();
            }
        }

        // Then process the rest
        foreach (var node in runtimeNodes.Values)
        {
            if (!(node is RuntimeTimeNode))
            {
                node.ProcessNode();
            }
        }
    }
}