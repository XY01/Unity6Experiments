using System;
using UnityEngine;


namespace Xy01.NodeGraph
{
    // A collection of all nodes in the project
    // TODO: Split nodes up eventually into smaller packages
 
    /// <summary> Outputs current time </summary>
    public class RuntimeTimeNode : RuntimeNode
    {
        public override void ProcessNode()
        {
            PropagateOutput("Time", Time.time);
            PropagateOutput("Time 2", Time.time * 2);
        }
    }

    /// <summary>
    /// Outputs a sin wave
    /// </summary>
    [NodeInfo("Math/Basic", "Sin")]
    public class RuntimeSinWaveNode : RuntimeNode
    {
        public override void ProcessNode()
        {
            if (InputValues.TryGetValue("Input", out float input))
            {
                PropagateOutput("Output", Mathf.Sin(input));
            }
        }
    }
    
    [NodeInfo("Material", "Mat Props")]
    public class RuntimeMaterialPropertyNode : RuntimeNode
    {
        private Material _material;

        public void Initialize(Material mat)
        {
            _material = mat;
        }

        public override void ProcessNode()
        {
            if (_material != null)
            {
                foreach (var input in InputValues)
                {
                    _material.SetFloat(input.Key, input.Value);
                }
            }
        }
    }
    
   // Basic Math Operations
    public class RuntimeClampNode : RuntimeNode
    {
        public override void ProcessNode()
        {
            if (InputValues.TryGetValue("Input", out float input) &&
                InputValues.TryGetValue("Min", out float min) &&
                InputValues.TryGetValue("Max", out float max))
            {
                PropagateOutput("Output", Mathf.Clamp(input, min, max));
            }
        }
    }
    
   
    [NodeInfo("Values/Float", "Float")]
    public class RuntimeFloatValueNode : RuntimeNode
    {
        private float _value = 0f;
        
        public void Initialize(float val)
        {
            _value = val;
        }
        
        public override void ProcessNode()
        {
            PropagateOutput("FloatValue", _value);
        }
    }

    public class LerpNode : RuntimeNode
    {
        public override void ProcessNode()
        {
            if (InputValues.TryGetValue("A", out float a) &&
                InputValues.TryGetValue("B", out float b) &&
                InputValues.TryGetValue("T", out float t))
            {
                PropagateOutput("Output", Mathf.Lerp(a, b, t));
            }
        }
    }

    public class InverseLerpNode : RuntimeNode
    {
        public override void ProcessNode()
        {
            if (InputValues.TryGetValue("A", out float a) &&
                InputValues.TryGetValue("B", out float b) &&
                InputValues.TryGetValue("Value", out float value))
            {
                PropagateOutput("Output", Mathf.InverseLerp(a, b, value));
            }
        }
    }

    public class MinNode : RuntimeNode
    {
        public override void ProcessNode()
        {
            if (InputValues.TryGetValue("A", out float a) &&
                InputValues.TryGetValue("B", out float b))
            {
                PropagateOutput("Output", Mathf.Min(a, b));
            }
        }
    }

    public class MaxNode : RuntimeNode
    {
        public override void ProcessNode()
        {
            if (InputValues.TryGetValue("A", out float a) &&
                InputValues.TryGetValue("B", out float b))
            {
                PropagateOutput("Output", Mathf.Max(a, b));
            }
        }
    }

    // Trigonometry
    public class CosNode : RuntimeNode
    {
        public override void ProcessNode()
        {
            if (InputValues.TryGetValue("Input", out float input))
            {
                PropagateOutput("Output", Mathf.Cos(input));
            }
        }
    }

    public class TanNode : RuntimeNode
    {
        public override void ProcessNode()
        {
            if (InputValues.TryGetValue("Input", out float input))
            {
                PropagateOutput("Output", Mathf.Tan(input));
            }
        }
    }

    // Noise and Random
    public class PerlinNoiseNode : RuntimeNode
    {
        public override void ProcessNode()
        {
            if (InputValues.TryGetValue("X", out float x) &&
                InputValues.TryGetValue("Y", out float y))
            {
                PropagateOutput("Output", Mathf.PerlinNoise(x, y));
            }
        }
    }

    // Advanced Math
    public class SmoothstepNode : RuntimeNode
    {
        public override void ProcessNode()
        {
            if (InputValues.TryGetValue("Edge0", out float edge0) &&
                InputValues.TryGetValue("Edge1", out float edge1) &&
                InputValues.TryGetValue("Value", out float value))
            {
                PropagateOutput("Output", Mathf.SmoothStep(edge0, edge1, value));
            }
        }
    }

    public class PingPongNode : RuntimeNode
    {
        public override void ProcessNode()
        {
            if (InputValues.TryGetValue("Input", out float input) &&
                InputValues.TryGetValue("Length", out float length))
            {
                PropagateOutput("Output", Mathf.PingPong(input, length));
            }
        }
    }

    public class RemapNode : RuntimeNode
    {
        public override void ProcessNode()
        {
            if (InputValues.TryGetValue("Value", out float value) &&
                InputValues.TryGetValue("FromMin", out float fromMin) &&
                InputValues.TryGetValue("FromMax", out float fromMax) &&
                InputValues.TryGetValue("ToMin", out float toMin) &&
                InputValues.TryGetValue("ToMax", out float toMax))
            {
                float normalizedValue = Mathf.InverseLerp(fromMin, fromMax, value);
                float remappedValue = Mathf.Lerp(toMin, toMax, normalizedValue);
                PropagateOutput("Output", remappedValue);
            }
        }
    }

    // Value Modification
    public class PowerNode : RuntimeNode
    {
        public override void ProcessNode()
        {
            if (InputValues.TryGetValue("Base", out float baseValue) &&
                InputValues.TryGetValue("Exponent", out float exponent))
            {
                PropagateOutput("Output", Mathf.Pow(baseValue, exponent));
            }
        }
    }

    public class FractionalNode : RuntimeNode
    {
        public override void ProcessNode()
        {
            if (InputValues.TryGetValue("Input", out float input))
            {
                PropagateOutput("Output", input - Mathf.Floor(input));
            }
        }
    }

    // Animation Curves
    public class AnimationCurveNode : RuntimeNode
    {
        private AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        public void SetCurve(AnimationCurve newCurve)
        {
            curve = newCurve;
        }

        public override void ProcessNode()
        {
            if (InputValues.TryGetValue("Time", out float time))
            {
                PropagateOutput("Output", curve.Evaluate(time));
            }
        }
    }

    // Value Combination
    public class BlendNode : RuntimeNode
    {
        public override void ProcessNode()
        {
            if (InputValues.TryGetValue("A", out float a) &&
                InputValues.TryGetValue("B", out float b) &&
                InputValues.TryGetValue("Weight", out float weight))
            {
                weight = Mathf.Clamp01(weight);
                PropagateOutput("Output", (a * (1 - weight)) + (b * weight));
            }
        }
    }
}
