using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AsmdefHelper.DependencyGraph.Editor.DependencyNode.Sort
{
    public class KeremSortStrategy : ISortStrategy
    {
        private readonly Dictionary<string, int> _nodeSizes;


        public KeremSortStrategy(Dictionary<string, int> nodeSizes)
        {
            _nodeSizes = nodeSizes;
        }


        public IEnumerable<SortedNode> Sort(IEnumerable<IDependencyNode> nodes)
        {
            var nodeArr = nodes.ToArray();

            var posDict = nodeArr.ToDictionary(x => x.Profile, _ => Vector2.zero);

            foreach (var node in nodeArr)
            {
                // var vector2 = new Vector2(512 * node.Depth1, 128* nodeSizes[node.]);
                var vector2 = new Vector2(512 * node.Depth1, 128 * _nodeSizes[node.Profile.Id.value]);

                Debug.Log(vector2.x + " " + vector2.y);

                posDict[node.Profile] = vector2;
            }

            return posDict.Select(x => new SortedNode { Profile = x.Key, Position = x.Value });
        }
    }
}