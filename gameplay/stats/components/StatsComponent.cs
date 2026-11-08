using System.Collections.Generic;
using lethal.common.registries.tags;
using lethal.core.config;
using lethal.core.domain.characters;
using lethal.core.domain.stats;
using lethal.core.domain.tags;
using lethal.gameplay.payloads.instances;
using lethal.gameplay.payloads.sources;
using lethal.gameplay.stats.components.stat_dependencies;

namespace lethal.gameplay.stats.components;
public sealed partial class StatsComponent
{
    private readonly Dictionary<PayloadSourceHandle, StatSourceEntry> _activeSources = new();
    private readonly Dictionary<StatId, StatState> _states = new();
    private readonly StatGraph _statGraph;
    private readonly ErrorConfig _errorConfig;
    private readonly PayloadFootprintHandler _footprintHandler;
    private readonly HashSet<StatId> _scratch = new();

    private const string Source = nameof(StatsComponent);

    private StatsComponent(Dictionary<StatId, double> resolvedBaseStats, StatGraph statGraph, ErrorConfig errorConfig, PayloadFootprintHandler footprintHandler)
    {
        _statGraph = statGraph;
        _errorConfig = errorConfig;
        _footprintHandler = footprintHandler;

        foreach (var kvp in  resolvedBaseStats)
        {
            _states[kvp.Key] = new StatState(kvp.Value);
        }
    }

    public static StatsComponent Create(CharacterDefinition characterDefinition, StatRegistry statRegistry, ErrorConfig errorConfig, PayloadFootprintHandler footprintHandler)
    {
        var statGraph = new StatGraph(errorConfig);
        var resolvedBaseStats = new Dictionary<StatId, double>();

        foreach (var statDef in statRegistry.All)
        {
            resolvedBaseStats[statDef.Id] = statDef.DefaultValue;
        }
        foreach (var (statId, baseValue) in characterDefinition.BaseStats)
        {
            if (!statRegistry.Contains(statId))
            {
                errorConfig.ReportError(Source, $"Character '{characterDefinition.Id}' has base value for unknown stat '{statId}'.");
                continue;
            }
            resolvedBaseStats[statId] = baseValue;
        }
        return new StatsComponent(resolvedBaseStats, statGraph, errorConfig, footprintHandler);
    }

    public bool InstallSource(PayloadSourceHandle sourceHandle, TagSet sourceTags, IEnumerable<StatPayloadInstance> payloadInstances, IEnumerable<StatSourceValue> sourceValues)
    {
        RemoveSource(sourceHandle);

        var sourceEntry = new StatSourceEntry(sourceTags);
        _activeSources[sourceHandle] = sourceEntry;

        foreach (var payloadInstance in payloadInstances)
        {
            if (InstallPayload(sourceEntry, payloadInstance)) continue;
            RemoveSource(sourceHandle);
            return false;
        }

        foreach (var sourceValue in sourceValues)
        {
            InstallSourceValue(sourceEntry, sourceValue);
        }

        return true;
    }

    public bool RemoveSource(PayloadSourceHandle sourceHandle)
    {
        if (!_activeSources.Remove(sourceHandle, out var sourceEntry)) return false;

        foreach (var payloadInstance in sourceEntry.Instances)
        {
            var footprint = _footprintHandler.Of(payloadInstance.PayloadId);
            _statGraph.Remove(payloadInstance.PayloadId, footprint);

            foreach (var stat in footprint.PrimaryWrites)   
            { 
                var state = _states[stat];
                state.PrimaryInstances.Remove(payloadInstance);
                MarkDirty(stat); 
            }
            foreach (var stat in footprint.SecondaryWrites) 
            {
                var state = _states[stat];
                state.SecondaryInstances.Remove(payloadInstance); 
                MarkDirty(stat); 
            }
            foreach (var stat in footprint.Drains)
            {
                var state = _states[stat];
                state.DrainInstances.Remove(payloadInstance);
                MarkDirty(stat);
            }
        }

        foreach (var (stat, lane) in sourceEntry.PushedKeys)
        {
            _states[stat].SourceValues.RemoveAll(v => v.SourceHandle == sourceHandle && v.Lane == lane);
            if (lane == Lane.Default) MarkDirty(stat);
            else MarkLaneDirty(stat, lane);
        }

        return true;
    }

    private bool InstallPayload(StatSourceEntry sourceEntry, StatPayloadInstance payloadInstance)
    {
        var footprint = _footprintHandler.Of(payloadInstance.PayloadId);
        if (!_statGraph.TryAdd(payloadInstance.PayloadId, footprint)) return false;

        sourceEntry.Instances.Add(payloadInstance);

        foreach (var stat in footprint.PrimaryWrites)   
        { 
            var state = _states[stat];
            state.PrimaryInstances.Add(payloadInstance);
            MarkDirty(stat);
        }
        foreach (var stat in footprint.SecondaryWrites) 
        { 
            var state = _states[stat];
            state.SecondaryInstances.Add(payloadInstance);
            MarkDirty(stat);
        }
        foreach (var stat in footprint.Drains)
        {
            var state = _states[stat];
            state.DrainInstances.Add(payloadInstance);
            MarkDirty(stat);
        }
        return true;
    }

    private void InstallSourceValue(StatSourceEntry sourceEntry, StatSourceValue sourceValue)
    {
        var state = _states[sourceValue.Stat];
        state.SourceValues.Add(sourceValue);
        sourceEntry.PushedKeys.Add((sourceValue.Stat, sourceValue.Lane));

        if (sourceValue.Lane == Lane.Default) MarkDirty(sourceValue.Stat);
        else MarkLaneDirty(sourceValue.Stat, sourceValue.Lane);
    }

    private void MarkDirty(StatId stat)
    {
        var state = _states[stat];
        if (state.IsFullyDirty()) return;

        state.MarkAllDirty();

        _scratch.Clear();
        _statGraph.CollectDependents(stat, _scratch);
        foreach (var dependent in _scratch)
        {
            _states[dependent].MarkAllDirty();
        }
    }

    private void MarkLaneDirty(StatId stat, Lane lane)
    {
        var state = _states[stat];
        state.MarkLaneDirty(lane);

        _scratch.Clear();
        _statGraph.CollectDependents(stat, _scratch);
        foreach (var dependent in _scratch)
        {
            _states[dependent].MarkAllDirty();
        }
    }

    private void RefreshBase(StatState state)
    {
        
    }
}