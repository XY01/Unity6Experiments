using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEditor.UIElements;

namespace Xy01.NodeGraph.Editor
{
    // Methods to create all the nodes and their input/output ports
    
    /// <summary> A node that returns the current time, time * 2 etc </summary>
    public class TimeNode : Node
    {
        public TimeNode()
        {
            title = "Time";

            var timeOut = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(float));
            timeOut.portName = "Time";
            outputContainer.Add(timeOut);
            
            var time2Out = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(float));
            time2Out.portName = "Time 2";
            outputContainer.Add(time2Out);

            RefreshExpandedState();
            RefreshPorts();
        }
    }

    /// <summary>
    /// Outputs the sin of an input float value 
    /// </summary>
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
    
    public class ClampNode : Node
    {
        public ClampNode()
        {
            title = "Clamp";

            // Input ports
            var input = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(float));
            input.portName = "Input";
            inputContainer.Add(input);
            
            var minValue = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(float));
            minValue.portName = "Min";
            inputContainer.Add(minValue);
            
            var maxValue = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(float));
            maxValue.portName = "Max";
            inputContainer.Add(maxValue);

            // Output port
            var output = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(float));
            output.portName = "Output";
            outputContainer.Add(output);

            RefreshExpandedState();
            RefreshPorts();
        }
    }
    
    
    public class FloatValueNode : Node 
    {
        public float floatValue = 0f;

        public FloatValueNode()
        {
            title = "Float Value";

            // Create float field
            var floatField = new FloatField("FloatValue")
            {
                value = floatValue
            };
            floatField.RegisterValueChangedCallback(evt =>
            {
                floatValue = evt.newValue;
            });
            
        
            // Add the float field to the main container
            mainContainer.Add(floatField);

            // Just the output port
            var output = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(float));
            output.portName = "FloatValue";
            outputContainer.Add(output);

            RefreshExpandedState();
            RefreshPorts();
        }
    }
   

    /// <summary>
    /// Creates a node from a selected material that exposes nodes for all properties that can accept float values
    /// </summary>
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
        }
    }
}