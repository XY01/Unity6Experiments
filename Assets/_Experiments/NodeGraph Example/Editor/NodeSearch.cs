using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xy01.NodeGraph;
using Xy01.NodeGraph.Editor;

namespace Xy01.NodeGraph.Editor
{
   
    public class NodeSearchWindow : ScriptableObject, ISearchWindowProvider
    {
        private CustomGraphView graphView;
        private EditorWindow editorWindow;
        private Dictionary<string, List<Type>> nodesByCategory;

        public void Init(CustomGraphView graphView, EditorWindow editorWindow)
        {
            this.graphView = graphView;
            this.editorWindow = editorWindow;
            CacheNodeTypes();
        }

        private void CacheNodeTypes()
        {
            nodesByCategory = new Dictionary<string, List<Type>>();

            // Get all types in all assemblies
            var nodeTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type =>
                    !type.IsAbstract &&
                    (typeof(Node).IsAssignableFrom(type) || typeof(RuntimeNode).IsAssignableFrom(type)) &&
                    type.GetCustomAttribute<NodeInfoAttribute>() != null);

            // Group by category
            foreach (var type in nodeTypes)
            {
                var attr = type.GetCustomAttribute<NodeInfoAttribute>();
                if (!nodesByCategory.ContainsKey(attr.Category))
                {
                    nodesByCategory[attr.Category] = new List<Type>();
                }

                nodesByCategory[attr.Category].Add(type);
            }
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var tree = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent("Create Node"))
            };

            // Add categories and their nodes
            foreach (var category in nodesByCategory.OrderBy(kvp => kvp.Key))
            {
                // Add category
                tree.Add(new SearchTreeGroupEntry(new GUIContent(category.Key)) { level = 1 });

                // Add nodes in this category
                foreach (var nodeType in category.Value.OrderBy(t =>
                             t.GetCustomAttribute<NodeInfoAttribute>().DisplayName))
                {
                    var attr = nodeType.GetCustomAttribute<NodeInfoAttribute>();
                    tree.Add(new SearchTreeEntry(new GUIContent(attr.DisplayName))
                    {
                        level = 2,
                        userData = nodeType
                    });
                }
            }

            return tree;
        }

        public bool OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
        {
            // Get mouse position
            var windowMousePosition = editorWindow.rootVisualElement.ChangeCoordinatesTo(
                editorWindow.rootVisualElement.parent,
                context.screenMousePosition - editorWindow.position.position);
            var graphMousePosition = graphView.contentViewContainer.WorldToLocal(windowMousePosition);

            return CreateNode(searchTreeEntry, graphMousePosition);
        }

        private bool CreateNode(SearchTreeEntry searchTreeEntry, Vector2 position)
        {
            if (!(searchTreeEntry.userData is Type nodeType))
                return false;

            Node node = null;

            // Handle special cases
            if (nodeType == typeof(MaterialPropertyNode))
            {
                var material = Selection.activeObject as Material;
                if (material != null)
                {
                    node = new MaterialPropertyNode(material);
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "Please select a material in the Project window", "OK");
                    return false;
                }
            }
            else
            {
                // Create instance through reflection
                try
                {
                    node = (Node)Activator.CreateInstance(nodeType);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to create node of type {nodeType.Name}: {e.Message}");
                    return false;
                }
            }

            if (node != null)
            {
                node.SetPosition(new Rect(position, Vector2.zero));
                node.viewDataKey = GUID.Generate().ToString();
                graphView.AddElement(node);
                return true;
            }

            return false;
        }
    }
}