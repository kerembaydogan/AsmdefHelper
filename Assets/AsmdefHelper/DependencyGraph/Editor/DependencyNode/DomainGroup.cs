using System;
using System.Collections.Generic;
using System.Linq;

namespace AsmdefHelper.DependencyGraph.Editor.DependencyNode
{
    public class DomainGroup
    {
        private readonly Dictionary<string, List<DomainUnit>> _dict;


        public DomainGroup()
        {
            _dict = new();
        }


        public void Create(IEnumerable<string> all)
        {
            _dict.Clear();
            foreach (var str in all)
            {
                var unit = new DomainUnit(str, '.');
                if (!unit.HasSubDomain())
                {
                    unit = new(str, '-');
                }
                if (!unit.HasSubDomain())
                {
                    unit = new(str, '_');
                }

                if (!unit.HasSubDomain())
                {
                    _dict.Add(unit.FullName, new() { unit });
                }
                else
                {
                    if (_dict.TryGetValue(unit.TopDomain, out var list))
                    {
                        list.Add(unit);
                    }
                    else
                    {
                        _dict.Add(unit.TopDomain, new() { unit });
                    }
                }
            }
            var soloKeys = GetSoloDomains().ToArray();

            foreach (var key in soloKeys)
            {
                var unit = _dict[key].FirstOrDefault();
                if (unit == null || key == unit.FullName)
                {
                    continue;
                }

                _dict.Remove(key);
                var newKey = unit.FullName;
                if (_dict.ContainsKey(newKey))
                {
                    _dict[newKey].Add(unit);
                }
                else
                {
                    _dict.Add(newKey, new() { new(unit.FullName, '\0') });
                }
            }
        }


        public IEnumerable<string> GetTopDomains() => _dict.Keys;

        public IEnumerable<string> GetSoloDomains() => _dict.Where(x => x.Value.Count == 1).Select(x => x.Key);

        public IEnumerable<string> GetTopDomainsWithSomeSubDomains() => _dict.Keys.Except(GetSoloDomains());


        public IEnumerable<DomainUnit> GetSubDomains(string topDomain)
        {
            if (_dict.TryGetValue(topDomain, out var list))
            {
                return list;
            }
            return Array.Empty<DomainUnit>();
        }
    }
}