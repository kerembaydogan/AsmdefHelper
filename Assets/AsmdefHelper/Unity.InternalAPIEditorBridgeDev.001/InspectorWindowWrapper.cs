using UnityEditor;

namespace AsmdefHelper.Unity.InternalAPIEditorBridgeDev._001 {
    public class InspectorWindowWrapper : EditorWindow{
        private InspectorWindow _inspectorWindow;

        public void GetInspectorWindow() {
            _inspectorWindow = CreateWindow<InspectorWindow>();
        }

        public void Lock(bool isLock) {
            if (_inspectorWindow != null) {
                _inspectorWindow.isLocked = isLock;
            }
        }

        public void AllApply() {
            foreach (var editor in _inspectorWindow.tracker.activeEditors) {
#if UNITY_2021_1_OR_NEWER
                var assetImporterEditor = editor as UnityEditor.AssetImporters.AssetImporterEditor;
#else
                var assetImporterEditor = editor as UnityEditor.Experimental.AssetImporters.AssetImporterEditor;
#endif

                if (assetImporterEditor != null && assetImporterEditor.HasModified()) {
                    assetImporterEditor.ApplyAndImport();
                }
            }
        }

        public void CloseInspectorWindow() {
            if (_inspectorWindow != null) {
                _inspectorWindow.Close();
            }
        }
    }
}
