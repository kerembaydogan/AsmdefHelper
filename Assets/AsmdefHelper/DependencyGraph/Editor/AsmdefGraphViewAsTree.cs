﻿using System.Collections.Generic;
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


        public AsmdefGraphViewAsTree(IEnumerable<Assembly> assemblies)
        {
            // var assemblyArr = assemblies.ToArray();
            var assemblyArr = assemblies.Where(e => e.name is "GameCode" or "PuzzleSudoku").ToArray();
            // var assemblyArr = assemblies.Where(e => e.name is "UnityEngine.TestRunner" or "UnityEditor.TestRunner").ToArray();

            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            Insert(0, new GridBackground());

            this.AddManipulator(new SelectionDragger());

            this.AddManipulator(new ContentDragger());

            List<string> asmdefPathList = new();
            GenerateRecursiveDict(assemblyArr, asmdefPathList);
            asmdefPathList.Sort();

            foreach (var asmdefPath in asmdefPathList)
            {
                var node = new AsmdefNode(asmdefPath, contentContainer);
                AddElement(node);
                _asmdefNodeDict.Add(asmdefPath, node);
            }

            var nodeProfiles2 = asmdefPathList.Select((path, _) => new NodeProfile(new(path), path)).ToDictionary(np => np.Name);

            _dependencies2 = new(nodeProfiles2.Count);

            GenerateRecursiveDependenciesDict(assemblyArr, nodeProfiles2, _dependencies2);

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

            foreach (var dep in _dependencies2.Values)
            {
                if (!_asmdefNodeDict.TryGetValue(dep.Profile.Name, out var node)) continue;
                node.LeftPort.Label = $"RefBy({dep.Sources.Count})";
                node.RightPort.Label = $"RefTo({dep.Destinations.Count})";

                if (dep.Sources.Count == 0 && dep.Destinations.Count == 0)
                {
                    node.Visibility = false;
                }
            }
        }


        private static void GenerateRecursiveDict(IEnumerable<Assembly> assemblyArr, ICollection<string> dict, string root = "")
        {
            foreach (var asm in assemblyArr)
            {
                dict.Add(root + "/" + asm.name);
                GenerateRecursiveDict(asm.assemblyReferences, dict, root + "/" + asm.name);
            }
        }


        private static void GenerateRecursiveDependenciesDict(IEnumerable<Assembly> assemblyArr, IReadOnlyDictionary<string, NodeProfile> nodeProfiles, IDictionary<string, IDependencyNode> dict, string rootAsmName = "", string rootProfileName = "")
        {
            foreach (var asm in assemblyArr)
            {
                var asmName = rootAsmName + "/" + asm.name;

                if (!nodeProfiles.TryGetValue(asmName, out var profile)) continue;
                var requireProfiles = asm.assemblyReferences.Where(x => nodeProfiles.ContainsKey(asmName + "/" + x.name)).Select(x => nodeProfiles[asmName + "/" + x.name]).ToArray();
                var dep = new HashSetDependencyNode(profile);
                dep.SetRequireNodes(requireProfiles);
                var profileName = rootProfileName + profile.Name;

                dict.Add(profileName, dep);

                GenerateRecursiveDependenciesDict(asm.assemblyReferences, nodeProfiles, dict, asmName, profileName);
            }
        }


        public void SetNodeVisibility(string nodeName, bool visibleOuter)
        {
            var key = nodeName.StartsWith("/") ? nodeName : "/" + nodeName;

            if (!_asmdefNodeDict.TryGetValue(key, out var node))
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