using Compiler.Models;
using FluentAssertions;

namespace Compiler.Tests;

public class DigraphTests
{
    [Fact]
    public void Digraph_TopologicalSort_GivesACorrectValue()
    {
        // based on the example diagram from Wikipedia
        var digraph = new Digraph<int>();
        var nodes = new Dictionary<int, IEnumerable<int>>()
        {
            { 5, [] },
            { 7, [] },
            { 3, [] },
            { 11, [5, 7] },
            { 8, [7, 3] },
            { 2, [11] },
            { 9, [11, 8] },
            { 10, [11, 3] },
        };
        foreach (var kvp in nodes.OrderBy(_ => Random.Shared.Next()))
        {
            digraph.Add(kvp.Key, kvp.Value);
        }

        var sorted = digraph.TopologicalSort().ToList();

        sorted.IndexOf(5).Should().BeLessThan(sorted.IndexOf(11));

        sorted.IndexOf(7).Should().BeLessThan(sorted.IndexOf(11)).And.BeLessThan(sorted.IndexOf(8));

        sorted.IndexOf(3).Should().BeLessThan(sorted.IndexOf(8)).And.BeLessThan(sorted.IndexOf(10));

        sorted
            .IndexOf(11)
            .Should()
            .BeLessThan(sorted.IndexOf(2))
            .And.BeLessThan(sorted.IndexOf(9))
            .And.BeLessThan(sorted.IndexOf(10));

        sorted.IndexOf(8).Should().BeLessThan(sorted.IndexOf(9));
    }

    [Fact]
    public void Digraph_TopologicalSort_Throws_CycleException_WhenCycleExists()
    {
        var digraph = new Digraph<int>();

        digraph.Add(1, [2, 3]);
        digraph.Add(3, [4]);
        digraph.Add(4, [1]);

        digraph
            .Enumerating(sut => sut.TopologicalSort())
            .Should()
            .Throw<CycleException<int>>()
            .Where(e => Enumerable.SequenceEqual(e.CycleMembers, new List<int> { 1, 3, 4 }));
    }
}
