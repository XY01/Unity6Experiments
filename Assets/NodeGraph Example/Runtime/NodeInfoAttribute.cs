using System;

namespace Xy01.NodeGraph
{
    /// <summary>
    /// Attribute to mark node types with category and display name so that it can be added to a searchable list
    /// of nodes that accessed when editing a graph
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class NodeInfoAttribute : Attribute
    {
        /// <summary>
        /// Nodes catagory, i.e. math, logic, material etc
        /// </summary>
        public string Category { get; private set; }
        /// <summary>
        /// Name of the node in the searchable list under the catagory
        /// </summary>
        public string DisplayName { get; private set; }

        public NodeInfoAttribute(string category, string displayName = null)
        {
            Category = category;
            DisplayName = displayName ?? "Missing Name";
        }
    }
}
