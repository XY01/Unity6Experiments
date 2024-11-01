using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;

public class CustomGraphView : GraphView
{
    public CustomGraphView()
    {
        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
        
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());
        
        var grid = new GridBackground();
        Insert(0, grid);
        grid.StretchToParentSize();
    }

    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        var compatiblePorts = new List<Port>();
        ports.ForEach((port) =>
        {
            if (startPort != port && startPort.node != port.node && startPort.direction != port.direction)
            {
                compatiblePorts.Add(port);
            }
        });
        return compatiblePorts;
    }

    public void Clear()
    {
        foreach (var node in nodes.ToList())
        {
            RemoveElement(node);
        }

        foreach (var edge in edges.ToList())
        {
            RemoveElement(edge);
        }
    }
}

public class TimeNode : Node
{
    public TimeNode()
    {
        title = "Time";
        
        var output = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(float));
        output.portName = "Time";
        outputContainer.Add(output);
        
        RefreshExpandedState();
        RefreshPorts();
    }
}

public class SinWaveNode : Node
{
    public SinWaveNode()
    {
        title = "Sin Wave";
        
        // Input port
        var input = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(float));
        input.portName = "Input";
        inputContainer.Add(input);
        
        // Output port
        var output = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(float));
        output.portName = "Output";
        outputContainer.Add(output);
        
        RefreshExpandedState();
        RefreshPorts();
    }
}

public class MaterialPropertyNode : Node
{
    private Material material;
    private Dictionary<string, Port> propertyPorts = new Dictionary<string, Port>();

    public MaterialPropertyNode(Material material)
    {
        this.material = material;
        title = "Material Properties";
        
        var materialField = new ObjectField("Material")
        {
            objectType = typeof(Material),
            value = material
        };
        materialField.RegisterValueChangedCallback(evt =>
        {
            material = evt.newValue as Material;
            RefreshPorts();
        });
        mainContainer.Add(materialField);
        
        RefreshPorts();
    }

    public Material GetMaterial()
    {
        return material;
    }

    private void RefreshPorts()
    {
        // Clear existing ports
        foreach (var port in propertyPorts.Values)
        {
            inputContainer.Remove(port);
        }
        propertyPorts.Clear();

        if (material != null)
        {
            MaterialProperty[] properties = MaterialEditor.GetMaterialProperties(new UnityEngine.Object[] { material });
            foreach (MaterialProperty prop in properties)
            {
                if (prop.type == MaterialProperty.PropType.Float)
                {
                    var port = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(float));
                    port.portName = prop.name;
                    inputContainer.Add(port);
                    propertyPorts[prop.name] = port;
                }
            }
        }

        RefreshExpandedState();
        //RefreshPorts();
    }
}