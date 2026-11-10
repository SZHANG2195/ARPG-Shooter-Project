using System.Collections.Generic;
using lethal.core.config;
using lethal.core.domain.payloads;
using lethal.core.domain.stats;
using lethal.gameplay.stats.components.stat_dependencies.enums;

namespace lethal.gameplay.stats.components.stat_dependencies;

public sealed class StatGraph
{
    private readonly record struct Edge(StatId To, PayloadId Via, EdgeKind Kind);
    private readonly Dictionary<StatId, List<Edge>> _adjacencyList = new();
    private readonly ErrorConfig _errorConfig;

    private const string Source = nameof(StatGraph);
    
    public StatGraph(ErrorConfig errorConfig) => _errorConfig = errorConfig;

    public bool TryAdd(PayloadId via, PayloadFootprint footprint)
    {
        //two passes ensure that we don't add any edges if a cycle is detected, otherwise we may end up with dangling edges
        foreach (var (read, kind) in footprint.Reads)
        {
            if (kind == EdgeKind.Resource) continue;

            foreach (var write in footprint.PrimaryWrites)
            {
                if (kind == EdgeKind.Evaluation && WouldCreateCycle(via, read, write))
                {
                    var message = $"Payload '{via}' creates a stat dependency cycle: {read} -> {write}. Addition rejected.";
                    _errorConfig.ReportError(Source, message);
                

                    if (!_errorConfig.StrictValidation) return false;
                }   
            }
        }

        foreach (var (read, kind) in footprint.Reads)
        {
            if (kind == EdgeKind.Resource) continue;

            foreach (var write in footprint.PrimaryWrites)
            {
                var edge = new Edge(write, via, kind);
                if (!_adjacencyList.TryGetValue(read, out var list)) 
                {
                    _adjacencyList[read] = list = new();
                }
                list.Add(edge);
            }
        }

        return true;
    }   

    public void Remove(PayloadId via, PayloadFootprint footprint)
    {
        foreach (var (read, kind) in footprint.Reads)
        {
            //statgraphs shouldn't handle resources as it is more complex due to frequent updates
            //this should be handled in the resource component itself
            if (kind == EdgeKind.Resource || !_adjacencyList.TryGetValue(read, out var edges)) continue;

            //point of removing all edges is since on a technical level multiple reads and writes
            //may exist, we need to properly remove all edges that match the payload and kind, otherwise we may leave dangling edges
            edges.RemoveAll(edge => edge.Via == via && edge.Kind == kind);

            if (edges.Count == 0) _adjacencyList.Remove(read);

        }
    }

    public void CollectDependents(StatId statId, HashSet<StatId> collected)
    {
        var stack = new Stack<StatId>();
        stack.Push(statId);

        //simple dfs traversal, we prevent infinite loops by never adding an already added edge
        //such a case would be against design anyways
        while (stack.Count > 0) 
        {
            if (_adjacencyList.TryGetValue(stack.Pop(), out var edges)) 
            {
                foreach (var edge in edges) 
                {
                    if (collected.Add(edge.To)) stack.Push(edge.To);
                }
            }
        }
    }

    private bool WouldCreateCycle(PayloadId via, StatId read, StatId write)
    {
        if (read == write) return true;

        // Check if writing back to 'read' can already reach 'write' through existing edges
        return Reaches(write, read, new HashSet<StatId>(), new List<(StatId Stat, PayloadId Via)>());
    }

    private bool Reaches(StatId from, StatId goal, HashSet<StatId> seen, List<(StatId Stat, PayloadId Via)> path)
    {
        if (!seen.Add(from) || !_adjacencyList.TryGetValue(from, out var edges)) return false;
        foreach (var edge in edges)
        {
            //a mirror is actually perfectly fine because it doesn't actually create a true dependency
            //mirrors only affect final value by making the payload multipliers also apply to it without making new modifiers
            if (edge.Kind != EdgeKind.Evaluation) continue;

            path.Add((edge.To, edge.Via));
            if (edge.To == goal || Reaches(edge.To, goal, seen, path)) return true;
            path.RemoveAt(path.Count - 1);
        }
        return false;
    }
}