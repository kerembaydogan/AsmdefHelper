using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace AsmdefHelper.DependencyGraph.Editor.NodeView {
    public class GraphViewPort : Port, IPort {
        private readonly VisualElement _parentContentContainer;

        public GraphViewPort(VisualElement contentContainer, Direction directionType) : base(Orientation.Horizontal,
            directionType, Capacity.Multi, typeof(Port)) {
            _parentContentContainer = contentContainer;
        }

        public string Label { set => portName = value; get => portName; }
        public Vector2 Position => new(GetPosition().x, GetPosition().y);

        public void Connect(IPort port) {
            if (port is Port graphViewPort) {
                _parentContentContainer.Add(ConnectTo(graphViewPort));
            }
        }
    }
}
