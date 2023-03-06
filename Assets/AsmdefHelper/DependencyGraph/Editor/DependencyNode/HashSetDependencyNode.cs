using System.Collections.Generic;

namespace AsmdefHelper.DependencyGraph.Editor.DependencyNode
{
    public class HashSetDependencyNode : IDependencyNode
    {
        public int RecursiveChildSize { get; }
        public int Depth1 { get; }
        public int Depth2 { get; }
        public NodeProfile Profile { get; }
        public ICollection<NodeProfile> Sources => _sources;
        public ICollection<NodeProfile> Destinations => _destinations;

        private readonly HashSet<NodeProfile> _sources;
        private readonly HashSet<NodeProfile> _destinations;


        public HashSetDependencyNode(NodeProfile profile, int depth1, int depth2)
        {
            Depth1 = depth1;
            Depth2 = depth2;
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