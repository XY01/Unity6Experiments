using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;

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
        var addTimeButton = new Button(() => { AddNode("TimeNode"); }) { text = "Add Time Node" };
        var addSinButton = new Button(() => { AddNode("SinWaveNode"); }) { text = "Add Sin Wave Node" };
        var addMaterialButton = new Button(() => { AddMaterialNode(); }) { text = "Add Material Node" };
        
        toolbar.Add(addTimeButton);
        toolbar.Add(addSinButton);
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
        currentGraph.nodes.Clear();

        // Save nodes
        foreach (var node in graphView.nodes.ToList())
        {
            var nodeData = new RuntimeGraph.NodeData
            {
                guid = node.viewDataKey,
                position = node.GetPosition().position,
                type = node.GetType().Name
            };

            // Save material reference for material nodes
            if (node is MaterialPropertyNode matNode)
            {
                nodeData.material = matNode.GetMaterial();
            }

            // Save connections
            foreach (var output in node.outputContainer.Children().OfType<Port>())
            {
                foreach (Edge edge in output.connections)
                {
                    nodeData.connections.Add(new RuntimeGraph.ConnectionData
                    {
                        outputNodeGuid = node.viewDataKey,
                        inputNodeGuid = edge.input.node.viewDataKey,
                        outputPortName = output.portName,
                        inputPortName = edge.input.portName
                    });
                }
            }

            currentGraph.nodes.Add(nodeData);
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
        foreach (var nodeData in currentGraph.nodes)
        {
            Node node = null;
            switch (nodeData.type)
            {
                case "TimeNode":
                    node = new TimeNode();
                    break;
                case "SinWaveNode":
                    node = new SinWaveNode();
                    break;
                case "MaterialPropertyNode":
                    node = new MaterialPropertyNode(nodeData.material);
                    break;
            }

            if (node != null)
            {
                node.viewDataKey = nodeData.guid;
                node.SetPosition(new Rect(nodeData.position, Vector2.zero));
                graphView.AddElement(node);
                nodeMap[nodeData.guid] = node;
            }
        }

        // Create connections
        foreach (var nodeData in currentGraph.nodes)
        {
            foreach (var connection in nodeData.connections)
            {
                if (nodeMap.TryGetValue(connection.outputNodeGuid, out var outputNode) &&
                    nodeMap.TryGetValue(connection.inputNodeGuid, out var inputNode))
                {
                    var outputPort = outputNode.outputContainer.Children()
                        .OfType<Port>()
                        .First(p => p.portName == connection.outputPortName);
                    
                    var inputPort = inputNode.inputContainer.Children()
                        .OfType<Port>()
                        .First(p => p.portName == connection.inputPortName);

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