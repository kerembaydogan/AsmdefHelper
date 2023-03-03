using System.Collections.Generic;
using System.Linq;
using AsmdefHelper.DependencyGraph.Editor.DependencyNode;
using AsmdefHelper.DependencyGraph.Editor.DependencyNode.Sort;
using AsmdefHelper.DependencyGraph.Editor.NodeView;
using UnityEditor.Compilation;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace AsmdefHelper.DependencyGraph.Editor
{
    public sealed class AsmdefGraphView : GraphView
    {
        private readonly Dictionary<string, IAsmdefNodeView> _asmdefNodeDict;
        private readonly Dictionary<string, NodeProfile> _nodeProfiles;
        private readonly Dictionary<string, IDependencyNode> _dependencies;
        private readonly AlignSortStrategy _sortStrategy;
        private readonly IEnumerable<SortedNode> _sortedNode;


        public AsmdefGraphView(IEnumerable<Assembly> assemblies)
        {
            var assemblyArr = assemblies.ToArray();

            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            Insert(0, new GridBackground());

            this.AddManipulator(new SelectionDragger());

            this.AddManipulator(new ContentDragger());

            _asmdefNodeDict = new();

            foreach (var asm in assemblyArr)
            {
                var node = new AsmdefNode(asm.name, contentContainer)
                {
                    // Visibility = false,
                };
                AddElement(node);
                _asmdefNodeDict.Add(node.title, node);
            }

            _nodeProfiles = assemblyArr.Select((x, i) => new NodeProfile(new(i), x.name)).ToDictionary(x => x.Name);

            _dependencies = new(_nodeProfiles.Count);

            foreach (var asm in assemblyArr)
            {
                if (!_nodeProfiles.TryGetValue(asm.name, out var profile)) continue;
                var requireProfiles = asm.assemblyReferences.Where(x => _nodeProfiles.ContainsKey(x.name)).Select(x => _nodeProfiles[x.name]);
                var dep = new HashSetDependencyNode(profile);
                dep.SetRequireNodes(requireProfiles);
                _dependencies.Add(profile.Name, dep);
            }

            NodeProcessor.SetBeRequiredNodes(_dependencies.Values);

            foreach (var dep in _dependencies.Values)
            {
                if (!_asmdefNodeDict.TryGetValue(dep.Profile.Name, out var fromNode)) continue;
                foreach (var dest in dep.Destinations)
                {
                    if (!_asmdefNodeDict.TryGetValue(dest.Name, out var toNode)) continue;
                    fromNode.RightPort.Connect(toNode.LeftPort);
                }
            }

            foreach (var dep in _dependencies.Values)
            {
                if (!_asmdefNodeDict.TryGetValue(dep.Profile.Name, out var node)) continue;
                node.LeftPort.Label = $"RefBy({dep.Sources.Count})";
                node.RightPort.Label = $"RefTo({dep.Destinations.Count})";
            }

            _sortStrategy = new(AlignParam.Default(), Vector2.zero);

            _sortedNode = _sortStrategy.Sort(_dependencies.Values);

            foreach (var node in _sortedNode)
            {
                if (!_asmdefNodeDict.TryGetValue(node.Profile.Name, out var nodeView)) continue;
                nodeView.SetPositionXY(node.Position);
                nodeView.Visibility = false;
            }
        }


        public void SetNodeVisibility(string nodeName, bool visibleOuter)
        {
            Debug.Log(_asmdefNodeDict);
            Debug.Log(_dependencies);
            Debug.Log(_nodeProfiles);
            Debug.Log(_sortStrategy);
            Debug.Log(_sortedNode);

            if (!_asmdefNodeDict.TryGetValue(nodeName, out var node))
            {
                Debug.LogWarning(nodeName + " NOT FOUND");
                return;
            }
            Debug.Log(nodeName + " FOUND");
            node.Visibility = visibleOuter;

            var tryGetValue = _dependencies.TryGetValue(nodeName, out var dep);

            if (tryGetValue)
            {
                Debug.Log(dep.Destinations.Count);

                foreach (var valueDestination in dep.Destinations)
                {
                    Debug.Log(nodeName + " " + valueDestination.Name + "/" + valueDestination.Id);
                    // if (visibleOuter) SetNodeVisibility(valueDestination.Name, true);
                    SetNodeVisibility(valueDestination.Name, visibleOuter);
                }
            }
        }


        public override List<Port> GetCompatiblePorts(Port startAnchor, NodeAdapter nodeAdapter)
        {
            return ports.ToList();
        }
    }
}