using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;

namespace Xy01.NodeGraph.Editor
{
    // This attribute tells Unity to process RuntimeGraph assets
    [CustomEditor(typeof(RuntimeGraph))]
    public class RuntimeGraphAssetHandler : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // Draw the default inspector
            DrawDefaultInspector();
        
            // Add a button to open the graph editor
            if (GUILayout.Button("Open Graph Editor"))
            {
                OpenGraphEditor();
            }
        }

        private void OpenGraphEditor()
        {
            RuntimeGraphEditor.OpenWindow((RuntimeGraph)target);
        }

        // This enables double-click opening in the Project window
        [OnOpenAsset(1)]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            var graph = EditorUtility.InstanceIDToObject(instanceID) as RuntimeGraph;
            if (graph != null)
            {
                RuntimeGraphEditor.OpenWindow(graph);
                return true;
            }
            return false;
        }
    }
    
    [CustomEditor(typeof(RuntimeGraph))]
    public class RuntimeGraphEditor : EditorWindow
    {
        private CustomGraphView graphView;
        private RuntimeGraph currentGraph;

        [MenuItem("Window/Runtime Graph Editor")]
        public static void OpenWindow()
        {
            var window = GetWindow<RuntimeGraphEditor>("Runtime Graph");
            window.Show();
        }
        
        public static void OpenWindow(RuntimeGraph graph)
        {
            var window = GetWindow<RuntimeGraphEditor>("Runtime Graph");
            window.currentGraph = graph;
            window.LoadGraph();
            window.Show();
        }

        private void OnEnable()
        {
            InitializeGraphView();
            SetupToolbar();
        }

        private void InitializeGraphView()
        {
            graphView = new CustomGraphView
            {
                style = { flexGrow = 1 }
            };
            rootVisualElement.Add(graphView);
        }

        private void SetupToolbar()
        {
            var toolbar = new Toolbar();

            // Add graph asset selection
            var graphField = new ObjectField("Graph Asset")
            {
                objectType = typeof(RuntimeGraph),
                allowSceneObjects = false
            };
            graphField.RegisterValueChangedCallback(evt =>
            {
                currentGraph = evt.newValue as RuntimeGraph;
                if (currentGraph != null)
                {
                    LoadGraph();
                }
            });
            toolbar.Add(graphField);

            // Add node creation buttons
            var addTimeButton = new Button(() => { AddNode("TimeNode"); }) { text = "Add Time" };
            var addSinButton = new Button(() => { AddNode("SinWaveNode"); }) { text = "Add Sin Wave" };
            var addClampButton = new Button(() => { AddNode("ClampNode"); }) { text = "Add Clamp" };
            var addFloatValueButton = new Button(() => { AddNode("FloatValueNode"); }) { text = "Add Float Value" };
            var addMaterialButton = new Button(() => { AddMaterialNode(); }) { text = "Add Material" };

            toolbar.Add(addTimeButton);
            toolbar.Add(addSinButton);
            toolbar.Add(addClampButton);
            toolbar.Add(addFloatValueButton);
            toolbar.Add(addMaterialButton);

            // Add save button
            var saveButton = new Button(() => { SaveGraph(); }) { text = "Save Graph" };
            toolbar.Add(saveButton);

            rootVisualElement.Add(toolbar);
        }

        private void SaveGraph()
        {
            if (currentGraph == null)
            {
                string path = EditorUtility.SaveFilePanelInProject(
                    "Save Graph",
                    "New Graph",
                    "asset",
                    "Save graph asset"
                );

                if (string.IsNullOrEmpty(path))
                    return;

                currentGraph = CreateInstance<RuntimeGraph>();
                AssetDatabase.CreateAsset(currentGraph, path);
            }

            // Clear existing data
            currentGraph.Nodes.Clear();

            // Save nodes
            foreach (var node in graphView.nodes.ToList())
            {
                var nodeData = new RuntimeGraph.NodeData
                {
                    Guid = node.viewDataKey,
                    Position = node.GetPosition().position,
                    Type = node.GetType().Name
                };

                // Save material reference for material nodes
                if (node is MaterialPropertyNode matNode)
                {
                    nodeData.Material = matNode.GetMaterial();
                }
                
                // Save material reference for material nodes
                if (node is FloatValueNode floatNode)
                {
                    nodeData.FloatValue = floatNode.floatValue;
                }

                // Save connections
                foreach (var output in node.outputContainer.Children().OfType<Port>())
                {
                    foreach (Edge edge in output.connections)
                    {
                        nodeData.Connections.Add(new RuntimeGraph.ConnectionData
                        {
                            OutputNodeGuid = node.viewDataKey,
                            InputNodeGuid = edge.input.node.viewDataKey,
                            OutputPortName = output.portName,
                            InputPortName = edge.input.portName
                        });
                    }
                }

                currentGraph.Nodes.Add(nodeData);
            }

            EditorUtility.SetDirty(currentGraph);
            AssetDatabase.SaveAssets();
        }

        private void LoadGraph()
        {
            if (currentGraph == null) return;

            // Clear existing graph
            graphView.Clear();

            // Create dictionary to store nodes by guid
            var nodeMap = new Dictionary<string, Node>();

            // Create nodes
            foreach (var nodeData in currentGraph.Nodes)
            {
                Node node = null;
                switch (nodeData.Type)
                {
                    case "TimeNode":
                        node = new TimeNode();
                        break;
                    case "SinWaveNode":
                        node = new SinWaveNode();
                        break;
                    case "ClampNode":
                        node = new ClampNode();
                        break;
                    case "FloatValueNode":
                        node = new FloatValueNode();
                        break;
                    case "MaterialPropertyNode":
                        node = new MaterialPropertyNode(nodeData.Material);
                        break;
                }

                if (node != null)
                {
                    node.viewDataKey = nodeData.Guid;
                    node.SetPosition(new Rect(nodeData.Position, Vector2.zero));
                    graphView.AddElement(node);
                    nodeMap[nodeData.Guid] = node;
                }
            }

            // Create connections
            foreach (var nodeData in currentGraph.Nodes)
            {
                foreach (var connection in nodeData.Connections)
                {
                    if (nodeMap.TryGetValue(connection.OutputNodeGuid, out var outputNode) &&
                        nodeMap.TryGetValue(connection.InputNodeGuid, out var inputNode))
                    {
                        var outputPort = outputNode.outputContainer.Children()
                            .OfType<Port>()
                            .First(p => p.portName == connection.OutputPortName);

                        var inputPort = inputNode.inputContainer.Children()
                            .OfType<Port>()
                            .First(p => p.portName == connection.InputPortName);

                        var edge = outputPort.ConnectTo(inputPort);
                        graphView.AddElement(edge);
                    }
                }
            }
        }

        private void AddNode(string type)
        {
            Node node = null;
            switch (type)
            {
                case "TimeNode":
                    node = new TimeNode();
                    break;
                case "SinWaveNode":
                    node = new SinWaveNode();
                    break;
                case "ClampNode":
                    node = new ClampNode();
                    break;
                case "FloatValueNode":
                    node = new FloatValueNode();
                    break;
            }

            if (node != null)
            {
                node.viewDataKey = GUID.Generate().ToString();
                graphView.AddElement(node);
            }
        }

        private void AddMaterialNode()
        {
            var material = Selection.activeObject as Material;
            if (material != null)
            {
                var node = new MaterialPropertyNode(material);
                node.viewDataKey = GUID.Generate().ToString();
                graphView.AddElement(node);
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Please select a material in the Project window", "OK");
            }
        }
    }
}