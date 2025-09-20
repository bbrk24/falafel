namespace Compiler.Models;

public class Digraph<T>
    where T : IEquatable<T>
{
    private readonly Dictionary<T, HashSet<T>> _nodes = new();

    public void Add(T value, IEnumerable<T> sources)
    {
        if (_nodes.TryGetValue(value, out var existingSources))
        {
            if (existingSources.IsSupersetOf(sources))
            {
                return;
            }

            existingSources.UnionWith(sources);
        }
        else
        {
            _nodes.Add(value, sources.ToHashSet());
        }

        foreach (var source in sources)
        {
            if (!_nodes.ContainsKey(source))
            {
                _nodes.Add(source, []);
            }
        }
    }

    public IEnumerable<T> TopologicalSort()
    {
        Dictionary<T, int> result = [];
        foreach (var value in _nodes.Keys)
        {
            TopologicalSortAncestors(value, [value], result);
        }

        return result.OrderBy(kvp => kvp.Value).Select(kvp => kvp.Key);
    }

    private int TopologicalSortAncestors(T value, IEnumerable<T> values, Dictionary<T, int> results)
    {
        if (results.TryGetValue(value, out var maxDepth))
        {
            return maxDepth;
        }

        maxDepth = 0;
        foreach (var source in _nodes[value])
        {
            if (values.Contains(source))
            {
                throw new CycleException<T>(values);
            }
            else if (results.ContainsKey(source))
            {
                continue;
            }

            var depth = TopologicalSortAncestors(source, [.. values, source], results);
            if (depth > maxDepth)
            {
                maxDepth = depth;
            }
        }

        results.Add(value, maxDepth);
        return maxDepth;
    }
}

public class CycleException<T>(IEnumerable<T> values) : InvalidOperationException("Cycle detected")
{
    public IEnumerable<T> CycleMembers { get; } = values;
}
