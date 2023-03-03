using AsmdefHelper.DependencyGraph.Editor.NodeView;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace AsmdefHelper.DependencyGraph.Editor {
    public class AsmdefNode : UiElementsNodeView, IAsmdefNodeView {
        private readonly GraphViewPort _leftPort;
        private readonly GraphViewPort _rightPort;
        public IPort LeftPort => _leftPort;
        public IPort RightPort => _rightPort;

        public AsmdefNode(string nodeName, VisualElement parentContentContainer) {
            Label = nodeName;

            _leftPort = new(parentContentContainer, Direction.Input) { Label = "Ref By" };
            inputContainer.Add(LeftPort as Port); // as right side

            _rightPort = new(parentContentContainer, Direction.Output) { Label = "Ref To" };
            outputContainer.Add(RightPort as Port); // as left side
        }

        public override bool Visibility {
            get => base.Visibility;
            set {
                base.Visibility = value;
                foreach (var edge in _rightPort.connections) {
                    edge.visible = edge.input.node.visible & visible;
                }
                foreach (var edge in _leftPort.connections) {
                    edge.visible = edge.output.node.visible & visible;
                }
            }
        }
    }
}
