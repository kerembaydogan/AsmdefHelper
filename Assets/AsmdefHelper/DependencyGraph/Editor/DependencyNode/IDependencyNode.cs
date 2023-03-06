using System.Collections.Generic;

namespace AsmdefHelper.DependencyGraph.Editor.DependencyNode {
    public interface IDependencyNode {
        int Depth1 { get; }
        
        int RecursiveChildSize { get; }
        
        int Depth2 { get; }
        NodeProfile Profile { get; }
        ICollection<NodeProfile> Sources  { get; }
        ICollection<NodeProfile> Destinations { get; }
    }
}
