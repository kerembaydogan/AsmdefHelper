using System.Collections.Generic;

namespace AsmdefHelper.DependencyGraph.Editor.DependencyNode {
    public class HashSetDependencyNode : IDependencyNode {
        public NodeProfile Profile { get; }
        public ICollection<NodeProfile> Sources => _sources;
        public ICollection<NodeProfile> Destinations => _destinations;

        private readonly HashSet<NodeProfile> _sources;
        private readonly HashSet<NodeProfile> _destinations;

        public HashSetDependencyNode(NodeProfile profile) {
            Profile = profile;
            _sources = new();
            _destinations = new();
        }


        public override string ToString()
        {
            return $"{nameof(Profile)}: {Profile}, {nameof(Sources)}: {Sources}, {nameof(Destinations)}: {Destinations}";
        }
    }
}
