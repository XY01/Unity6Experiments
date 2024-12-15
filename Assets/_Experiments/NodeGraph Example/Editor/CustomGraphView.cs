using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace Xy01.NodeGraph.Editor
{
    /// <summary>
    /// Sets up the graph view with the default manipulators like panning, zooming, selecting, etc.
    /// </summary>
    public class CustomGraphView : GraphView
    {
        public CustomGraphView()
        {
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            var gridBackground = new GridBackground();
            Insert(0, gridBackground);
            gridBackground.StretchToParentSize();
        }

        /// <summary>
        /// Returns a list of ports that are compatible with the given start port.
        /// TODO - not currently used. Will be used in the future to disable incompatible ports when creating connections.
        /// </summary>
        /// <param name="startPort"></param>
        /// <param name="nodeAdapter"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Removes all nodes and edges from the graph.
        /// </summary>
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
}