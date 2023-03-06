using UnityEditor;
using UnityEngine;

namespace AsmdefHelper.DependencyGraph.Editor
{
    public class GraphEditorWindow : EditorWindow
    {
        private Rect _windowRect = new(100 + 100, 100, 100, 100);
        private Rect _windowRect2 = new(100, 100, 100, 100);


        [MenuItem("Window/Graph Editor Window")]
        private static void Init()
        {
            GetWindow(typeof(GraphEditorWindow));
        }


        private void OnGUI()
        {
            Handles.BeginGUI();
            Handles.DrawBezier(_windowRect.center, _windowRect2.center, new Vector2(_windowRect.xMax + 50f, _windowRect.center.y), new Vector2(_windowRect2.xMin - 50f, _windowRect2.center.y), Color.red, null, 5f);
            Handles.EndGUI();

            BeginWindows();
            _windowRect = GUI.Window(0,  _windowRect,  WindowFunction, "Box1");
            _windowRect2 = GUI.Window(1, _windowRect2, WindowFunction, "Box2");

            EndWindows();
        }


        private static void WindowFunction(int windowID)
        {
            GUI.DragWindow();
        }
    }
}