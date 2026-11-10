using System;
using System.Collections.Generic;
using lethal.common.registries.payloads;
using lethal.common.registries.tags;
using lethal.core.config;
using lethal.core.domain.characters;
using lethal.core.domain.context;
using lethal.core.domain.payloads;
using lethal.core.domain.payloads.enums;
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
    private readonly PayloadRegistry _payloadRegistry;
    private readonly HashSet<StatId> _scratch = new();

    private const string Source = nameof(StatsComponent);

    private StatsComponent(Dictionary<StatId, double> resolvedBaseStats, StatGraph statGraph, ErrorConfig errorConfig, PayloadFootprintHandler footprintHandler, PayloadRegistry payloadRegistry)
    {
        _statGraph = statGraph;
        _errorConfig = errorConfig;
        _footprintHandler = footprintHandler;
        _payloadRegistry = payloadRegistry;

        foreach (var kvp in  resolvedBaseStats)
        {
            _states[kvp.Key] = new StatState(kvp.Key, kvp.Value);
        }
    }

    public static StatsComponent Create(CharacterDefinition characterDefinition, StatRegistry statRegistry, PayloadRegistry payloadRegistry, ErrorConfig errorConfig, PayloadFootprintHandler footprintHandler)
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
                var message = $"Character '{characterDefinition.Id}' has base value for unknown stat '{statId}'.";
                errorConfig.ReportError(Source, message);
                continue;
            }
            resolvedBaseStats[statId] = baseValue;
        }
        return new StatsComponent(resolvedBaseStats, statGraph, errorConfig, footprintHandler, payloadRegistry);
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
            if (lane == StatStateLane.Default) MarkDirty(stat);
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

        if (sourceValue.Lane == StatStateLane.Default) MarkDirty(sourceValue.Stat);
        else MarkLaneDirty(sourceValue.Stat, sourceValue.Lane);
    }

    private void MarkDirty(StatId statId)
    {
        var state = _states[statId];
        if (state.IsFullyDirty()) return;

        state.MarkAllDirty();

        _scratch.Clear();
        _statGraph.CollectDependents(statId, _scratch);
        foreach (var dependent in _scratch)
        {
            _states[dependent].MarkAllDirty();
        }
    }

    private void MarkLaneDirty(StatId statId, StatStateLane lane)
    {
        var state = _states[statId];
        state.MarkLaneDirty(lane);

        _scratch.Clear();
        _statGraph.CollectDependents(statId, _scratch);
        foreach (var dependent in _scratch)
        {
            _states[dependent].MarkAllDirty();
        }
    }

    public StatResult GetStatResult(StatId statId, StatStateLane lane = StatStateLane.Default)
    {
        var state = _states[statId];

        ref var laneResult = ref GetLaneResult(state, lane);
        if (laneResult.IsDirty) 
        {
            EvaluateStat(statId, lane, state, ref laneResult);
            laneResult.IsDirty = false;
        }

        return laneResult.Result;
    }

    private ref StatLaneResult GetLaneResult(StatState state, StatStateLane lane)
    {
        if (lane == StatStateLane.Default) return ref state.Default;

        if (state.Lanes is null)
        {
            state.Lanes = new StatLaneResult[2];
            for (int i = 0; i < state.Lanes.Length; i++) state.Lanes[i].IsDirty = true;
        }
        return ref state.Lanes[(int)lane - 1];
    }

    private (StatResult LaneA, StatResult? LaneB) GetStatResultsAcrossLanes(StatId sourceStatId)
    {
        //temporary bool
        bool isDualWielding = false;
        if (isDualWielding)
        {
            var laneAResult = GetStatResult(sourceStatId, StatStateLane.Mainhand);
            var laneBResult = GetStatResult(sourceStatId, StatStateLane.Offhand);
            
            return (laneAResult, laneBResult);

        } 
        else
        {
            var defaultResult = GetStatResult(sourceStatId, StatStateLane.Default);
            return (defaultResult, null);
        }
    }

    private StatResult EvaluateStat(StatId statId, StatStateLane lane, StatState state, ref StatLaneResult laneResult)
    {
        double baseValue = ResolveBaseValue(state, lane);
        laneResult.Result = StatResult.Seed(baseValue);

        double accumulatedFlatValue = ResolveFlatValue(state, lane);
        laneResult.Result = laneResult.Result.ApplyFlatValue(accumulatedFlatValue);

        double? flatOverrideValue = ResolveFlatOverrideValue(state, lane);
        if (flatOverrideValue is { } resolvedFlatOverrideValue) laneResult.Result = laneResult.Result.ApplyFlatOverrideValue(resolvedFlatOverrideValue);

        double accumulatedDerivedFlatValue = ResolveDerivedFlatValue(state);
        laneResult.Result = laneResult.Result.ApplyDerivedPreConversionValue(accumulatedDerivedFlatValue);

        double accumulatedConversionFlatValue = ResolveConversionFlatValue(state);
        laneResult.Result = laneResult.Result.ApplyConvertedValue(accumulatedConversionFlatValue);

        var (accumulatedAdditiveValue, accumulatedMultiplicativeValue) = ResolveMultiplierValue(state);
        laneResult.Result = laneResult.Result.ApplyMultiplierValue(accumulatedAdditiveValue, accumulatedMultiplicativeValue);

        double accumulatedDerivedPostMultiplierValue = ResolveDerivedPostMultiplierValue(state);
        laneResult.Result = laneResult.Result.ApplyDerivedPostMultiplierValue(accumulatedDerivedPostMultiplierValue);

        double accumulatedDerivedFinalValue = ResolveDerivedResolvedValue(state);
        laneResult.Result = laneResult.Result.ApplyDerivedResolvedValue(accumulatedDerivedFinalValue);

        double? finalOverrideValue = ResolveFinalOverrideValue(state, lane);
        if (finalOverrideValue is { } resolvedFinalOverrideValue) laneResult.Result = laneResult.Result.ApplyFinalOverrideValue(resolvedFinalOverrideValue);

        return new StatResult(0, 0, 0, 0, 0 , 0, 0);
    }

    private double ResolveBaseValue(StatState state, StatStateLane lane)
    {
        double? defaultOverrideValue = null;
        bool hasDefaultOverride = false;

        foreach (var payloadInstance in state.PrimaryInstances)
        {
            var footprint = _footprintHandler.Of(payloadInstance.PayloadId);

            var payloadData = _payloadRegistry.Get(payloadInstance.PayloadId).PayloadData;
            if (!PassesContextCheck(payloadData)) continue;

            if (footprint.SetsDefault)
            {
                if (hasDefaultOverride)
                {
                    string statId = state.Id.ToString();
                    var message = $"Stat '{statId}' has multiple default overrides in primary instances. Overriding with the last one.";
                    _errorConfig.ReportError(Source, message);
                }
                defaultOverrideValue = payloadInstance.Primary;
                hasDefaultOverride = true; 
            }
        }
        foreach (var payloadInstance in state.SecondaryInstances)
        {
            var footprint = _footprintHandler.Of(payloadInstance.PayloadId);

            var payloadData = _payloadRegistry.Get(payloadInstance.PayloadId).PayloadData;
            if (!PassesContextCheck(payloadData)) continue;

            if (footprint.SetsDefault && payloadInstance.Secondary is { } secondaryOverride)
            {
                if (hasDefaultOverride)
                {
                    string statId = state.Id.ToString();
                    var message = $"Stat '{statId}' has multiple default overrides in secondary instances. Overriding with the last one.";
                    _errorConfig.ReportError(Source, message);
                }
                defaultOverrideValue = secondaryOverride;
                hasDefaultOverride = true; 
            }
        }

        state.DefaultOverrideValue = defaultOverrideValue;

        double baseValue = 0.0;
        bool hasPushedBases = false;
        foreach (var sourceValue in state.SourceValues)
        {
            if (sourceValue.IsBase && (sourceValue.Lane == lane || sourceValue.Lane == StatStateLane.Default))
            {
                if(hasPushedBases)
                {
                    string statId = state.Id.ToString();
                    var message = $"Stat '{statId}' has multiple base values. Overriding with the highest base value.";
                    _errorConfig.ReportError(Source, message);
                    baseValue = Math.Max(baseValue, sourceValue.Value);
                } 
                else baseValue = sourceValue.Value;
                hasPushedBases = true;
            }
        }

        double resolvedBase = hasPushedBases ? baseValue : state.OriginalBaseValue;
        double effectiveBase = state.DefaultOverrideValue ?? resolvedBase;

        return effectiveBase;
    }

    private double ResolveFlatValue(StatState state, StatStateLane lane)
    {
        double accumulatedFlatValue = 0.0;

        foreach (var sourceValue in state.SourceValues)
        {
            if (!sourceValue.IsBase && (sourceValue.Lane == lane || sourceValue.Lane == StatStateLane.Default))
            {
                accumulatedFlatValue += sourceValue.Value;
            }
        }

        foreach (var payloadInstance in state.PrimaryInstances)
        {
            var footprint = _footprintHandler.Of(payloadInstance.PayloadId);
            if (footprint.SetsDefault) continue;

            var payloadData = _payloadRegistry.Get(payloadInstance.PayloadId).PayloadData;
            if (!PassesContextCheck(payloadData)) continue;

            accumulatedFlatValue += payloadData switch
            {
                StatStaticPayload staticPayload when staticPayload.StatStaticType == StatStaticType.Flat => 
                    payloadInstance.Primary,

                StatContributionPayload contributionPayload when contributionPayload.StatStaticType == StatStaticType.Flat 
                    && contributionPayload.ContributionSourceType == ContributionSourceType.StaticStat => 
                        ResolveContributionValue(contributionPayload, payloadInstance, false),

                _ => 0.0
            };
        }

        foreach (var payloadInstance in state.SecondaryInstances)
        {
            if (payloadInstance.Secondary is { } secondaryValue)
            {
                var footprint = _footprintHandler.Of(payloadInstance.PayloadId);
                if (footprint.SetsDefault) continue;

                var payloadData = _payloadRegistry.Get(payloadInstance.PayloadId).PayloadData;
                if (!PassesContextCheck(payloadData)) continue;

                accumulatedFlatValue += payloadData switch
                {
                    StatStaticPayload staticPayload when staticPayload.StatStaticType == StatStaticType.Flat => 
                        secondaryValue,

                    StatContributionPayload contributionPayload when contributionPayload.StatStaticType == StatStaticType.Flat 
                        && contributionPayload.ContributionSourceType == ContributionSourceType.StaticStat=> 
                            ResolveContributionValue(contributionPayload, payloadInstance, true),

                    _ => 0.0
                };
            }
        }

        return accumulatedFlatValue;
    }

    private double? ResolveFlatOverrideValue(StatState state, StatStateLane lane)
    {
        double? flatOverrideValue = null;
        bool hasFlatOverride = false;

        foreach (var payloadInstance in state.PrimaryInstances)
        {
            var footprint = _footprintHandler.Of(payloadInstance.PayloadId);
            if (footprint.SetsDefault) continue;
            
            var payloadData = _payloadRegistry.Get(payloadInstance.PayloadId).PayloadData;

            if (!PassesContextCheck(payloadData)) continue;

            if (payloadData is StatOverridePayload overridePayload && overridePayload.StatOverrideType == StatOverrideType.FlatOverride)
            {
                if(hasFlatOverride)
                {
                    string statId = state.Id.ToString();
                    var message = $"Stat '{statId}' has multiple flat overrides in primary instances. Overriding with the last one.";
                    _errorConfig.ReportError(Source, message);
                }

                double resolvedValue = overridePayload.SourceStatId is { } sourceStatId
                    ? ResolveOverrideValue(overridePayload, sourceStatId, lane)
                    : payloadInstance.Primary;
                flatOverrideValue = resolvedValue;
                hasFlatOverride = true; 
            }
        }
        foreach (var payloadInstance in state.SecondaryInstances)
        {
            if (payloadInstance.Secondary is { } secondaryValue)
            {
                var footprint = _footprintHandler.Of(payloadInstance.PayloadId);

                var payloadData = _payloadRegistry.Get(payloadInstance.PayloadId).PayloadData;
                if (!PassesContextCheck(payloadData)) continue;

                if (payloadData is StatOverridePayload overridePayload && overridePayload.StatOverrideType == StatOverrideType.FlatOverride)
                {
                    if (hasFlatOverride)
                    {
                        string statId = state.Id.ToString();
                        var message = $"Stat '{statId}' has multiple flat overrides in secondary instances. Overriding with the last one.";
                        _errorConfig.ReportError(Source, message);
                    }

                    double resolvedValue = overridePayload.SourceStatId is { } sourceStatId
                        ? ResolveOverrideValue(overridePayload, sourceStatId, lane)
                        : secondaryValue;
                    flatOverrideValue = resolvedValue;
                    hasFlatOverride = true; 
                }
            }
        }

        return flatOverrideValue;
    }

    private double ResolveDerivedFlatValue(StatState state)
    {
        double accumulatedDerivedFlatValue = 0.0;

        foreach (var payloadInstance in state.PrimaryInstances)
        {
            var footprint = _footprintHandler.Of(payloadInstance.PayloadId);
            if (footprint.SetsDefault) continue;
            
            var payloadData = _payloadRegistry.Get(payloadInstance.PayloadId).PayloadData;

            if (!PassesContextCheck(payloadData)) continue;

            accumulatedDerivedFlatValue += payloadData switch
            {
                StatTranslationPayload translationPayload when translationPayload.StatTranslationType == StatTranslationType.DerivationPreMultipliers => 
                    ResolveDerivedValue(translationPayload, payloadInstance),

                StatMirrorPayload mirrorPayload when  mirrorPayload.StatStaticType == StatStaticType.Flat => 
                    ResolveMirrorValue(mirrorPayload, payloadInstance),

                _ => 0.0,
            };
        }

        return accumulatedDerivedFlatValue;
    }

    private double ResolveConversionFlatValue(StatState state)
    {
        double accumulatedConversionFlatValue = 0.0;

        foreach (var payloadInstance in state.PrimaryInstances)
        {
            var footprint = _footprintHandler.Of(payloadInstance.PayloadId);
            if (footprint.SetsDefault) continue;
            
            var payloadData = _payloadRegistry.Get(payloadInstance.PayloadId).PayloadData;

            if (!PassesContextCheck(payloadData)) continue;

            if (payloadData is StatTranslationPayload translationPayload &&  translationPayload.StatTranslationType == StatTranslationType.Conversion)
                accumulatedConversionFlatValue += ResolveDerivedValue(translationPayload, payloadInstance);
        }

        foreach (var payloadInstance in state.DrainInstances)
        {
            var footprint = _footprintHandler.Of(payloadInstance.PayloadId);
            if (footprint.SetsDefault) continue;
            
            var payloadData = _payloadRegistry.Get(payloadInstance.PayloadId).PayloadData;

            if (!PassesContextCheck(payloadData)) continue;

            if (payloadData is StatTranslationPayload translationPayload &&  translationPayload.StatTranslationType == StatTranslationType.Conversion)
                accumulatedConversionFlatValue -= ResolveDerivedValue(translationPayload, payloadInstance);
        }

        return accumulatedConversionFlatValue;
    }

    private (double additive, double multiplicative) ResolveMultiplierValue(StatState state)
    {
        double accumulatedAdditiveValue = 0.0;
        double accumulatedMultiplicativeValue = 1.0;

        foreach (var payloadInstance in state.PrimaryInstances)
        {
            var footprint = _footprintHandler.Of(payloadInstance.PayloadId);
            if (footprint.SetsDefault) continue;
            
            var payloadData = _payloadRegistry.Get(payloadInstance.PayloadId).PayloadData;

            if (!PassesContextCheck(payloadData)) continue;

            accumulatedAdditiveValue += payloadData switch
            {
                StatStaticPayload staticPayload when staticPayload.StatStaticType == StatStaticType.PercentAdditive =>
                    payloadInstance.Primary,
                
                StatMirrorPayload mirrorPayload when mirrorPayload.StatStaticType == StatStaticType.PercentAdditive =>
                    ResolveMirrorValue(mirrorPayload, payloadInstance),

                StatContributionPayload contributionPayload when contributionPayload.StatStaticType == StatStaticType.PercentAdditive 
                    && contributionPayload.ContributionSourceType == ContributionSourceType.StaticStat=>
                        ResolveContributionValue(contributionPayload, payloadInstance, false),

                _ => 0.0
            };

            accumulatedMultiplicativeValue *= payloadData switch
            {
                StatStaticPayload staticPayload when staticPayload.StatStaticType == StatStaticType.PercentMultiplicative =>
                    1.0 + payloadInstance.Primary,

                StatMirrorPayload mirrorPayload when mirrorPayload.StatStaticType == StatStaticType.PercentMultiplicative =>
                    1.0 + ResolveMirrorValue(mirrorPayload, payloadInstance),

                StatContributionPayload contributionPayload when contributionPayload.StatStaticType == StatStaticType.PercentMultiplicative 
                    && contributionPayload.ContributionSourceType == ContributionSourceType.StaticStat=>
                        1.0 + ResolveContributionValue(contributionPayload, payloadInstance, true),

                _ => 1.0
            };
        }

        foreach (var payloadInstance in state.SecondaryInstances)
        {
            if (payloadInstance.Secondary is { } secondaryValue)
            {
                var footprint = _footprintHandler.Of(payloadInstance.PayloadId);
                if (footprint.SetsDefault) continue;
            
                var payloadData = _payloadRegistry.Get(payloadInstance.PayloadId).PayloadData;

                if (!PassesContextCheck(payloadData)) continue;

                accumulatedAdditiveValue += payloadData switch
                {
                    StatStaticPayload staticPayload when staticPayload.StatStaticType == StatStaticType.PercentAdditive =>
                        secondaryValue,
                
                    StatMirrorPayload mirrorPayload when mirrorPayload.StatStaticType == StatStaticType.PercentAdditive =>
                        ResolveMirrorValue(mirrorPayload, payloadInstance),

                    StatContributionPayload contributionPayload when contributionPayload.StatStaticType == StatStaticType.PercentAdditive 
                        && contributionPayload.ContributionSourceType == ContributionSourceType.StaticStat =>
                            ResolveContributionValue(contributionPayload, payloadInstance, false),

                    _ => 0.0
                };

                accumulatedMultiplicativeValue += payloadData switch
                {
                    StatStaticPayload staticPayload when staticPayload.StatStaticType == StatStaticType.PercentMultiplicative =>
                        secondaryValue,

                    StatMirrorPayload mirrorPayload when mirrorPayload.StatStaticType == StatStaticType.PercentMultiplicative =>
                        ResolveMirrorValue(mirrorPayload, payloadInstance),

                    StatContributionPayload contributionPayload when contributionPayload.StatStaticType == StatStaticType.PercentMultiplicative 
                        && contributionPayload.ContributionSourceType == ContributionSourceType.StaticStat =>
                            ResolveContributionValue(contributionPayload, payloadInstance, true),

                    _ => 0.0
                };
            }
        }

        return (accumulatedAdditiveValue, accumulatedMultiplicativeValue);
    }

    private double ResolveDerivedPostMultiplierValue(StatState state)
    {
        double accumulatedDerivedPostMultiplierValue = 0.0;

        foreach (var payloadInstance in state.PrimaryInstances)
        {
            var footprint = _footprintHandler.Of(payloadInstance.PayloadId);
            if (footprint.SetsDefault) continue;
            
            var payloadData = _payloadRegistry.Get(payloadInstance.PayloadId).PayloadData;

            if (!PassesContextCheck(payloadData)) continue;

            if (payloadData is StatTranslationPayload translationPayload &&  translationPayload.StatTranslationType == StatTranslationType.DerivationPostMultipliers)
                accumulatedDerivedPostMultiplierValue += ResolveDerivedValue(translationPayload, payloadInstance);
        }

        return accumulatedDerivedPostMultiplierValue;
    }

    private double ResolveDerivedResolvedValue(StatState state)
    {
        double accumulatedDerivedFinalValue = 0.0;

        foreach (var payloadInstance in state.PrimaryInstances)
        {
            var footprint = _footprintHandler.Of(payloadInstance.PayloadId);
            if (footprint.SetsDefault) continue;
            
            var payloadData = _payloadRegistry.Get(payloadInstance.PayloadId).PayloadData;

            if (!PassesContextCheck(payloadData)) continue;

            if (payloadData is StatTranslationPayload translationPayload &&  translationPayload.StatTranslationType == StatTranslationType.DerivationLast)
                accumulatedDerivedFinalValue += ResolveDerivedValue(translationPayload, payloadInstance);
        }

        return accumulatedDerivedFinalValue;
    }

    private double? ResolveFinalOverrideValue(StatState state, StatStateLane lane)
    {
        double? finalOverrideValue = null;
        bool hasFinalOverride = false;

        foreach (var payloadInstance in state.PrimaryInstances)
        {
            var footprint = _footprintHandler.Of(payloadInstance.PayloadId);
            if (footprint.SetsDefault) continue;
            
            var payloadData = _payloadRegistry.Get(payloadInstance.PayloadId).PayloadData;

            if (!PassesContextCheck(payloadData)) continue;

            if (payloadData is StatOverridePayload overridePayload && overridePayload.StatOverrideType == StatOverrideType.FinalOverride)
            {
                if(hasFinalOverride)
                {
                    string statId = state.Id.ToString();
                    var message = $"Stat '{statId}' has multiple final overrides in primary instances. Overriding with the last one.";
                    _errorConfig.ReportError(Source, message);
                }

                double resolvedValue = overridePayload.SourceStatId is { } sourceStatId
                    ? ResolveOverrideValue(overridePayload, sourceStatId, lane)
                    : payloadInstance.Primary;
                finalOverrideValue = resolvedValue;
                hasFinalOverride = true; 
            }
        }
        foreach (var payloadInstance in state.SecondaryInstances)
        {
            if (payloadInstance.Secondary is { } secondaryValue)
            {
                var footprint = _footprintHandler.Of(payloadInstance.PayloadId);

                var payloadData = _payloadRegistry.Get(payloadInstance.PayloadId).PayloadData;
                if (!PassesContextCheck(payloadData)) continue;

                if (payloadData is StatOverridePayload overridePayload && overridePayload.StatOverrideType == StatOverrideType.FinalOverride)
                {
                    if (hasFinalOverride)
                    {
                        string statId = state.Id.ToString();
                        var message = $"Stat '{statId}' has multiple final overrides in secondary instances. Overriding with the last one.";
                        _errorConfig.ReportError(Source, message);
                    }

                    double resolvedValue = overridePayload.SourceStatId is { } sourceStatId
                        ? ResolveOverrideValue(overridePayload, sourceStatId, lane)
                        : secondaryValue;
                    finalOverrideValue = resolvedValue;
                    hasFinalOverride = true; 
                }
            }
        }

        return finalOverrideValue;
    }

    private double ResolveOverrideValue(StatOverridePayload overridePayload, StatId sourceStatId, StatStateLane lane)
    {
        var (laneA, laneB) = GetStatResultsAcrossLanes(sourceStatId);

        double GetValue(StatResult result) => overridePayload.StatOverrideType switch
        {
            StatOverrideType.FlatOverride => result.FlatTotalValue,
            StatOverrideType.FinalOverride => result.ResolvedValue,
            _ => result.FinalValue
        };

        return lane switch
        {
            StatStateLane.Mainhand => GetValue(laneA),
            StatStateLane.Offhand when laneB is { } b => GetValue(b),
            _ => GetValue(laneA)
        };
    }

    private double ResolveContributionValue(StatContributionPayload contributionPayload, StatPayloadInstance payloadInstance, bool isSecondary)
    {
        var (laneA, laneB) = GetStatResultsAcrossLanes(contributionPayload.SourceStatId);
        double sourceValue = laneA.FlatTotalValue + (laneB?.FlatTotalValue ?? 0.0);

        double coefficient = isSecondary ? (payloadInstance.Secondary ?? 0.0) : payloadInstance.Primary;

        return sourceValue * coefficient;
    }

    private double ResolveDerivedValue(StatTranslationPayload translationPayload, StatPayloadInstance payloadInstance)
    {
        var (laneA, laneB) = GetStatResultsAcrossLanes(translationPayload.SourceStatId);
        
        double sourceValue = translationPayload.StatTranslationType switch
        {
            StatTranslationType.Conversion => laneA.PreMultiplierValue + (laneB?.PreMultiplierValue ?? 0.0),
            
            StatTranslationType.DerivationPreMultipliers => laneA.FlatTotalValue + (laneB?.FlatTotalValue ?? 0.0),

            StatTranslationType.DerivationPostMultipliers => laneA.PostMultiplierValue + (laneB?.PostMultiplierValue ?? 0.0),

            StatTranslationType.DerivationLast => laneA.FinalValue + (laneB?.FinalValue ?? 0.0),

            _ => 0.0
        };

        return sourceValue * payloadInstance.Primary;
    }

    private double ResolveMirrorValue(StatMirrorPayload mirrorPayload, StatPayloadInstance payloadInstance)
    {
        var (laneA, laneB) = GetStatResultsAcrossLanes(mirrorPayload.SourceStatId);

        double sourceValue = mirrorPayload.StatStaticType switch
        {
            StatStaticType.Flat => laneA.AddedFlat + (laneB?.AddedFlat ?? 0.0),

            StatStaticType.PercentAdditive => laneA.AdditiveMultiplier + (laneB?.AdditiveMultiplier ?? 0.0),

            StatStaticType.PercentMultiplicative => laneA.MultiplicativeMultiplier + (laneB?.MultiplicativeMultiplier ?? 0.0),

            _ => 0.0
        };

        return sourceValue * payloadInstance.Primary;
    }

    private bool PassesContextCheck(IPayload payload)
    {
        if (payload is not IContextualPayload { ContextId: { IsDefault: false } contextualId }) return true; 
        
        //implement later
        return true;
    }
}