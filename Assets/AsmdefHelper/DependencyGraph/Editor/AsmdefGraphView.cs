using System.Collections.Generic;
using System.Linq;
using AsmdefHelper.DependencyGraph.Editor.DependencyNode;
using AsmdefHelper.DependencyGraph.Editor.DependencyNode.Sort;
using AsmdefHelper.DependencyGraph.Editor.NodeView;
using UnityEditor.Compilation;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace AsmdefHelper.DependencyGraph.Editor {
    public sealed class AsmdefGraphView : GraphView {
        private readonly Dictionary<string, IAsmdefNodeView> _asmdefNodeDict;


        public AsmdefGraphView(IEnumerable<Assembly> assemblies) {
            var assemblyArr = assemblies.ToArray();

            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            Insert(0, new GridBackground());

            this.AddManipulator(new SelectionDragger());

            this.AddManipulator(new ContentDragger());

            _asmdefNodeDict = new();

            foreach (var asm in assemblyArr) {
                var node = new AsmdefNode(asm.name, contentContainer) {
                    // Visibility = false,
                };
                AddElement(node);
                _asmdefNodeDict.Add(node.title, node);
            }

            var nodeProfiles = assemblyArr.Select((x, i) => new NodeProfile(new(i), x.name)).ToDictionary(x => x.Name);

            var dependencies = new List<IDependencyNode>(nodeProfiles.Count);

            foreach (var asm in assemblyArr) {
                if (!nodeProfiles.TryGetValue(asm.name, out var profile)) continue;
                var requireProfiles = asm.assemblyReferences.Where(x => nodeProfiles.ContainsKey(x.name)).Select(x => nodeProfiles[x.name]);
                var dep = new HashSetDependencyNode(profile);
                dep.SetRequireNodes(requireProfiles);
                dependencies.Add(dep);
            }

            NodeProcessor.SetBeRequiredNodes(dependencies);

            foreach (var dep in dependencies) {
                if (!_asmdefNodeDict.TryGetValue(dep.Profile.Name, out var fromNode)) {
                    continue;
                }
                foreach (var dest in dep.Destinations) {
                    if (!_asmdefNodeDict.TryGetValue(dest.Name, out var toNode)) {
                        continue;
                    }
                    fromNode.RightPort.Connect(toNode.LeftPort);
                }
            }

            foreach (var dep in dependencies) {
                if (!_asmdefNodeDict.TryGetValue(dep.Profile.Name, out var node)) continue;
                node.LeftPort.Label = $"RefBy({dep.Sources.Count})";
                node.RightPort.Label = $"RefTo({dep.Destinations.Count})";
            }

            var sortStrategy = new AlignSortStrategy(AlignParam.Default(), Vector2.zero);

            var sortedNode = sortStrategy.Sort(dependencies);

            foreach (var node in sortedNode) {
                if (_asmdefNodeDict.TryGetValue(node.Profile.Name, out var nodeView)) {
                    nodeView.SetPositionXY(node.Position);
                }

                nodeView.Visibility = false;
            }
        }


        public void SetNodeVisibility(string nodeName, bool visibleOuter) {
            if (!_asmdefNodeDict.TryGetValue(nodeName, out var node)) {
                Debug.LogWarning(nodeName + " NOT FOUND" );
                return;
            }
            Debug.Log(nodeName + " FOUND");
            node.Visibility = visibleOuter;
        }


        public override List<Port> GetCompatiblePorts(Port startAnchor, NodeAdapter nodeAdapter) {
            return ports.ToList();
        }
    }
}
