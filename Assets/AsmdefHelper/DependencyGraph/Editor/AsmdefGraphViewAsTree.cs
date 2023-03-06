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

        private static List<string> _asmNamesIgnored = new();


        public AsmdefGraphViewAsTree(IEnumerable<Assembly> assemblies)
        {
            var textAsset = Resources.Load<TextAsset>("asmdef_helper/asmdef_names");
            var textAssetIgnored = Resources.Load<TextAsset>("asmdef_helper/asmdef_names_ignored");

            AsmNames = new(textAsset.text.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries));
            _asmNamesIgnored = new(textAssetIgnored.text.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries));

            var assemblyArr = assemblies.Where(e => AsmNames.Contains(e.name) && !_asmNamesIgnored.Contains(e.name)).ToArray();

            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            Insert(0, new GridBackground());

            this.AddManipulator(new SelectionDragger());

            this.AddManipulator(new ContentDragger());

            this.AddManipulator(new RectangleSelector());

            List<string> asmdefPathList = new(1000);

            foreach (var asmRoot in assemblyArr)
            {
                GenerateRecursiveDict(asmRoot, asmdefPathList, "");
            }

            asmdefPathList.Sort();

            Debug.LogWarning(asmdefPathList.Count);

            var nodeSizes = GenerateNodeSizes(asmdefPathList);

            foreach (var s in asmdefPathList)
            {
                Debug.Log(s);
            }

            foreach (var asmdefPath in asmdefPathList)
            {
                // var nodeName = asmdefPath.Contains("->") ? asmdefPath[(asmdefPath.LastIndexOf("->", StringComparison.Ordinal) + 2)..] : asmdefPath;

                _asmdefNodeDict.Add(asmdefPath, new AsmdefNode(asmdefPath, contentContainer));
            }

            var nodeProfiles2 = asmdefPathList.Select((path, _) => new NodeProfile(new(path), path)).ToDictionary(np => np.Name);

            _dependencies2 = new(nodeProfiles2.Count);

            var depth = 0;

            foreach (var asmRoot in assemblyArr)
            {
                GenerateRecursiveDependenciesDict(asmRoot, nodeProfiles2, _dependencies2, "", depth++, 0);
            }

            Debug.LogError("nodeProfiles2.Count " + nodeProfiles2.Count);

            Debug.LogError("_dependencies2.Count" + _dependencies2.Count);

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
                node.LeftPort.Label = $"{dep.Depth1}/{dep.Depth2}  RefBy({dep.Sources.Count})";
                node.RightPort.Label = $"RefTo({dep.Destinations.Count})";
            }

            KeremSortStrategy sortStrategy = new(nodeSizes);

            var sortedNodes = sortStrategy.Sort(_dependencies2.Values);

            foreach (var nodeFromSorted in sortedNodes)
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
                    Debug.LogError(dep.Profile.Name + " GRAPH ELEMENT NOT ADDED TO GRAPH VIEW");
                    continue;
                }

                Debug.Log(dep.Profile.Name + " GRAPH ELEMENT ADDED TO GRAPH VIEW");

                AddElement(node as GraphElement);

                total++;
            }

            Debug.Log(total + "/" + _dependencies2.Count + " GRAPH ELEMENT ADDED TO GRAPH VIEW");
        }


        private static Dictionary<string, int> GenerateNodeSizes(IEnumerable<string> asmdefPathList)
        {
            var dictionary = asmdefPathList.ToDictionary(x => x, y => asmdefPathList.Count(p => p.StartsWith(y)));

            foreach (var keyValuePair in dictionary)
            {
                Debug.Log(keyValuePair.Key + " : " + keyValuePair.Value);
            }

            return dictionary;
        }


        private static void GenerateRecursiveDict(Assembly asm, ICollection<string> dict, string previousPath)
        {
            var currentPath = previousPath + asm.name;

            dict.Add(currentPath);

            // var asmAssemblyReferences = asm.assemblyReferences;
            var asmAssemblyReferences = asm.assemblyReferences.Where(e => !_asmNamesIgnored.Contains(e.name)).ToArray();

            foreach (var asmChild in asmAssemblyReferences)
            {
                GenerateRecursiveDict(asmChild, dict, currentPath + "->");
            }
        }


        private static void GenerateRecursiveDependenciesDict(Assembly asm, IReadOnlyDictionary<string, NodeProfile> nodeProfiles, IDictionary<string, IDependencyNode> dict, string previousPath, int depth1, int depth2)
        {
            var currentPath = previousPath + asm.name;

            if (!nodeProfiles.TryGetValue(currentPath, out var profile)) return;

            // var asmAssemblyReferences = asm.assemblyReferences;
            var asmAssemblyReferences = asm.assemblyReferences.Where(e => !_asmNamesIgnored.Contains(e.name)).ToArray();

            var requireProfiles = asmAssemblyReferences.Where(x => nodeProfiles.ContainsKey(currentPath + "->" + x.name)).Select(x => nodeProfiles[currentPath + "->" + x.name]).ToArray();
            var dep = new HashSetDependencyNode(profile, depth1, depth2);

            depth1++;

            dep.SetRequireNodes(requireProfiles);
            dict.Add(currentPath, dep);

            foreach (var asmChild in asmAssemblyReferences)
            {
                GenerateRecursiveDependenciesDict(asmChild, nodeProfiles, dict, currentPath + "->", depth1, depth2);
                depth2++;
            }
        }


        public void SetNodeVisibility(string nodeName, bool visibleOuter)
        {
            if (!_asmdefNodeDict.TryGetValue(nodeName, out var node))
            {
                Debug.LogWarning(nodeName + " NOT FOUND");
                return;
            }

            node.Visibility = visibleOuter;

            var tryGetValue = _dependencies2.TryGetValue(nodeName, out var dep);

            if (!tryGetValue) return;

            foreach (var valueDestination in dep.Destinations)
            {
                SetNodeVisibility(valueDestination.Name, visibleOuter);
            }
        }


        public override List<Port> GetCompatiblePorts(Port startAnchor, NodeAdapter nodeAdapter) => ports.ToList();
    }
}