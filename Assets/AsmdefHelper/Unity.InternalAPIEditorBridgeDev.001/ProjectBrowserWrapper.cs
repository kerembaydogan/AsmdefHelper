using UnityEditor;

namespace AsmdefHelper.Unity.InternalAPIEditorBridgeDev._001 {
    public class ProjectBrowserWrapper : EditorWindow {
        private ProjectBrowser _projectBrowser;

        public void GetProjectBrowser() {
            _projectBrowser = GetWindow<ProjectBrowser>();
        }

        public void SetSearch(string searchText) {
            if (_projectBrowser != null) {
                _projectBrowser.SetSearch(searchText);
            }
        }
    }
}
