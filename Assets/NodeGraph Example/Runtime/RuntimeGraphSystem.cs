using UnityEngine;
using System;
using System.Collections.Generic;

// Serializable graph data
[CreateAssetMenu(fileName = "New Graph", menuName = "Custom/Runtime Graph")]
public class RuntimeGraph : ScriptableObject
{
    [Serializable]
    public class NodeData
    {
        public string guid;
        public string type;
        public Vector2 position;
        public List<ConnectionData> connections = new List<ConnectionData>();
        public Material material; // For material nodes
    }

    [Serializable]
    public class ConnectionData
    {
        public string outputNodeGuid;
        public string inputNodeGuid;
        public string outputPortName;
        public string inputPortName;
    }

    public List<NodeData> nodes = new List<NodeData>();
}

// Runtime node base class
public abstract class RuntimeNode
{
    public string Guid { get; set; }
    protected Dictionary<string, float> inputValues = new Dictionary<string, float>();
    protected internal Dictionary<string, List<(RuntimeNode node, string portName)>> outputConnections 
        = new Dictionary<string, List<(RuntimeNode, string)>>();

    public virtual void ProcessNode()
    {
        // Override in derived classes
    }

    public void SetInputValue(string portName, float value)
    {
        inputValues[portName] = value;
    }

    protected void PropagateOutput(string portName, float value)
    {
        if (outputConnections.TryGetValue(portName, out var connections))
        {
            foreach (var (node, inputPort) in connections)
            {
                node.SetInputValue(inputPort, value);
            }
        }
    }
}

// Runtime node implementations
public class RuntimeTimeNode : RuntimeNode
{
    public override void ProcessNode()
    {
        PropagateOutput("Time", Time.time);
    }
}

public class RuntimeSinWaveNode : RuntimeNode
{
    public override void ProcessNode()
    {
        if (inputValues.TryGetValue("Input", out float input))
        {
            PropagateOutput("Output", Mathf.Sin(input));
        }
    }
}

public class RuntimeMaterialPropertyNode : RuntimeNode
{
    private Material material;
    
    public void Initialize(Material mat)
    {
        material = mat;
    }

    public override void ProcessNode()
    {
        if (material != null)
        {
            foreach (var input in inputValues)
            {
                material.SetFloat(input.Key, input.Value);
            }
        }
    }
}
