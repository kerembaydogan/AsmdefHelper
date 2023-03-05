using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AsmdefHelper.DependencyGraph.Editor.DependencyNode;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.UIElements;

namespace AsmdefHelper.DependencyGraph.Editor.AsmdefSelectionView {
    public class AsmdefSelectionView : EditorWindow {
        private const int ToggleCount = 1000;
        private static EditorWindow _graphWindow;
        private readonly ToggleDict _groupMasterToggleDict = new();
        private readonly ToggleDict _toggleDict = new();
        private IToggleCheckDelegate _toggleDelegate;


        public void OnEnable() {
            _graphWindow = GetWindow<AsmdefGraphEditorWindow>();
            var root = rootVisualElement;
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/AsmdefHelper/DependencyGraph/Editor/AsmdefSelectionView/AsmdefSelectionView.uxml");

            if (visualTree == null) {
                visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/dev.n5y.asmdefhelper/AsmdefHelper/DependencyGraph/Editor/AsmdefSelectionView/AsmdefSelectionView.uxml");
            }

#if UNITY_2020_1_OR_NEWER
            VisualElement labelFromUxml = visualTree.Instantiate();
#else
            VisualElement labelFromUXML = visualTree.CloneTree();
#endif
            root.Add(labelFromUxml);
        }


        public void SetAsmdef(IEnumerable<Assembly> assemblies, IToggleCheckDelegate toggleDelegate) {
            
            var assemblyArr = assemblies.Where(e => AsmdefGraphViewAsTree.AsmNames.Contains(e.name)).ToArray();

            var sortedAssemblies = assemblyArr.OrderBy(x => x.name).ToArray();

            var scrollView = rootVisualElement.Q<ScrollView>(className: "ScrollView");

            _toggleDict.Clear();

            for (var i = 0; i < ToggleCount; i++) {

                var toggle = rootVisualElement.Q<Toggle>(className: $"toggle{i}");

                if (toggle == null) {
                    Debug.LogError("toggle== null");
                    continue;
                }

                if (i < sortedAssemblies.Length) {
                    var assemblyName = sortedAssemblies[i].name;
                    toggle.text = assemblyName;
                    toggle.value = false;
                    _toggleDict.Add(assemblyName, new UiElementToggle(toggle));
                } else {
                    scrollView.Remove(toggle);
                }
            }

            var group = new DomainGroup();
            group.Create(sortedAssemblies.Select(x => x.name));
            var tops = group.GetTopDomainsWithSomeSubDomains().ToArray();
            foreach (var top in tops) {
                var topToggle = new Toggle { text = top, value = false };
                var slaveToggles = new List<IToggle>();
                Toggle firstToggle = null;
                var domains = group.GetSubDomains(top).ToArray();
                foreach (var domain in domains) {
                    var isLast = domains.Last() == domain;
                    if (!_toggleDict.TryGetToggle(domain.FullName, out var toggle)) continue;
                    toggle.Name = domain.HasSubDomain() ? $"{(isLast ? "└" : "├")} {domain.SubDomain}" : toggle.Name;
                    slaveToggles.Add(toggle);
                    if (firstToggle == null && toggle is UiElementToggle y) {
                        firstToggle = y.Toggle;
                    }
                }

                var toggleGroup = new ToggleGroup(new UiElementToggle(topToggle), slaveToggles);
                if (firstToggle != null) {
                    var index = scrollView.IndexOf(firstToggle);
                    // グループに属する toggle は box に入れる
                    var box = new Box();
                    scrollView.Insert(index,     topToggle);
                    scrollView.Insert(index + 1, box);
                    foreach (var slaveToggle in slaveToggles) {
                        if (slaveToggle is UiElementToggle x) {
                            box.Add(x.Toggle);
                        }
                    }
                }

                _groupMasterToggleDict.Add(top, toggleGroup);
            }

            _toggleDelegate = toggleDelegate;
        }


        private void OnGUI() {
            var updatedGroups = _groupMasterToggleDict.ScanUpdate().ToArray();
            _groupMasterToggleDict.OverwriteToggles(updatedGroups.Select(x => x.Item1));
            var updated = _toggleDict.ScanUpdate().ToArray();
            foreach (var x in updated) {
                var (key, current) = x;
                _toggleDelegate?.OnSelectionChanged(key, current);
            }
        }


        private async void OnDestroy() {
            await Task.Delay(1);
            if (_graphWindow != null) _graphWindow.Close();
            _graphWindow = null;
        }
    }
}
