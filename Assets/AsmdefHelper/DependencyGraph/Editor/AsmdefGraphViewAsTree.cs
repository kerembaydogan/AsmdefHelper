using System;
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
    public sealed class AsmdefGraphViewAsTree : GraphView
    {
        private readonly Dictionary<string, IAsmdefNodeView> _asmdefNodeDict = new();

        private readonly Dictionary<string, IDependencyNode> _dependencies2;

        public static List<string> AsmNames = new();


        public AsmdefGraphViewAsTree(IEnumerable<Assembly> assemblies)
        {
            var textAsset = Resources.Load<TextAsset>("asmdef_helper/asmdef_names");

            AsmNames = new(textAsset.text.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries));

            var assemblyArr = assemblies.Where(e => AsmNames.Contains(e.name)).ToArray();

            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            Insert(0, new GridBackground());

            this.AddManipulator(new SelectionDragger());

            this.AddManipulator(new ContentDragger());

            List<string> asmdefPathList = new();

            foreach (var asmRoot in assemblyArr)
            {
                GenerateRecursiveDict(asmRoot, asmdefPathList, "");
            }

            foreach (var asmdefPath in asmdefPathList)
            {
                _asmdefNodeDict.Add(asmdefPath, new AsmdefNode(asmdefPath, contentContainer));
            }

            var nodeProfiles2 = asmdefPathList.Select((path, _) => new NodeProfile(new(path), path)).ToDictionary(np => np.Name);

            _dependencies2 = new(nodeProfiles2.Count);
            foreach (var asmRoot in assemblyArr)
            {
                GenerateRecursiveDependenciesDict(asmRoot, nodeProfiles2, _dependencies2, "");
            }
            NodeProcessor.SetBeRequiredNodes(_dependencies2.Values);

            foreach (var dep in _dependencies2.Values)
            {
                if (!_asmdefNodeDict.TryGetValue(dep.Profile.Name, out var fromNode)) continue;
                foreach (var dest in dep.Destinations)
                {
                    if (!_asmdefNodeDict.TryGetValue(dest.Name, out var toNode)) continue;
                    fromNode.RightPort.Connect(toNode.LeftPort);
                }
            }

            foreach (var dep in _dependencies2.Values)
            {
                if (!_asmdefNodeDict.TryGetValue(dep.Profile.Name, out var node)) continue;
                node.LeftPort.Label = $"RefBy({dep.Sources.Count})";
                node.RightPort.Label = $"RefTo({dep.Destinations.Count})";
            }

            AlignSortStrategy sortStrategy = new(AlignParam.Default(), Vector2.zero);

            var sortedNode = sortStrategy.Sort(_dependencies2.Values);

            foreach (var nodeFromSorted in sortedNode)
            {
                if (!_asmdefNodeDict.TryGetValue(nodeFromSorted.Profile.Name, out var node)) continue;
                node.SetPositionXY(nodeFromSorted.Position);
                node.Visibility = false;
            }

            var total = 0;

            foreach (var dep in _dependencies2.Values)
            {
                if (!_asmdefNodeDict.TryGetValue(dep.Profile.Name, out var node))
                {
                    Debug.LogWarning(dep.Profile.Name + " GRAPH ELEMENT NOT ADDED TO GRAPH VIEW");
                    continue;
                }
                node.LeftPort.Label = $"RefBy({dep.Sources.Count})";
                node.RightPort.Label = $"RefTo({dep.Destinations.Count})";

                Debug.Log(dep.Profile.Name + " GRAPH ELEMENT ADDED TO GRAPH VIEW");

                AddElement(node as GraphElement);
            }

            Debug.Log(_dependencies2.Count + " GRAPH ELEMENT ADDED TO GRAPH VIEW");
        }


        private static void GenerateRecursiveDict(Assembly asm, ICollection<string> dict, string previousPath)
        {
            var currentPath = previousPath + asm.name;

            dict.Add(currentPath);

            var asmAssemblyReferencesUnfiltered = asm.assemblyReferences;
            var asmAssemblyReferences = asmAssemblyReferencesUnfiltered.Where(e => AsmNames.Contains(e.name)).ToArray();

            foreach (var asmChild in asmAssemblyReferences)
            {
                GenerateRecursiveDict(asmChild, dict, currentPath + "->");
            }
        }


        private static void GenerateRecursiveDependenciesDict(Assembly asm, IReadOnlyDictionary<string, NodeProfile> nodeProfiles, IDictionary<string, IDependencyNode> dict, string previousPath)
        {
            var currentPath = previousPath + asm.name;

            if (!nodeProfiles.TryGetValue(currentPath, out var profile)) return;
            var asmAssemblyReferencesUnfiltered = asm.assemblyReferences;
            var asmAssemblyReferences = asmAssemblyReferencesUnfiltered.Where(e => AsmNames.Contains(e.name)).ToArray();

            var requireProfiles = asmAssemblyReferences.Where(x => nodeProfiles.ContainsKey(currentPath + "->" + x.name)).Select(x => nodeProfiles[currentPath + "->" + x.name]).ToArray();
            var dep = new HashSetDependencyNode(profile);
            dep.SetRequireNodes(requireProfiles);

            dict.Add(currentPath, dep);
            foreach (var asmChild in asmAssemblyReferences)
            {
                GenerateRecursiveDependenciesDict(asmChild, nodeProfiles, dict, currentPath + "->");
            }
        }


        public void SetNodeVisibility(string nodeName, bool visibleOuter)
        {
            
            if (!_asmdefNodeDict.TryGetValue(nodeName, out var node))
            {
                Debug.LogWarning(key + " NOT FOUND");
                return;
            }

            node.Visibility = visibleOuter;

            var tryGetValue = _dependencies2.TryGetValue(key, out var dep);

            if (!tryGetValue) return;
            Debug.Log(dep.Destinations.Count);

            foreach (var valueDestination in dep.Destinations)
            {
                SetNodeVisibility(valueDestination.Name, visibleOuter);
            }
        }


        public override List<Port> GetCompatiblePorts(Port startAnchor, NodeAdapter nodeAdapter) => ports.ToList();
    }
}