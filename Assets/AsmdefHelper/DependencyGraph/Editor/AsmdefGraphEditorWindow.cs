using UnityEditor;
using UnityEditor.Compilation;

namespace AsmdefHelper.DependencyGraph.Editor {
    public class AsmdefGraphEditorWindow : EditorWindow, IToggleCheckDelegate {
        private static AsmdefSelectionView.AsmdefSelectionView _selectionWindow;

        private AsmdefGraphViewAsTree _graphView;


        [MenuItem("AsmdefHelper/Open DependencyGraph", priority = 2000)]
        public static void Open() {
            GetWindow<AsmdefGraphEditorWindow>("Asmdef Dependency");
        }


        private void OnEnable() {

            var asmdefs = CompilationPipeline.GetAssemblies();

            _graphView = new(asmdefs) { style = { flexGrow = 1 } };

            rootVisualElement.Add(_graphView);

            _selectionWindow = GetWindow<AsmdefSelectionView.AsmdefSelectionView>("Asmdef Selection");

            _selectionWindow.SetAsmdef(asmdefs, this);
        }


        // 片方を閉じる
        private void OnDestroy() {
            if (_selectionWindow != null) {
                _selectionWindow.Close();
            }
            _selectionWindow = null;
        }


        void IToggleCheckDelegate.OnSelectionChanged(string label, bool isChecked) {
            _graphView.SetNodeVisibility(label, isChecked);
        }
    }
}
