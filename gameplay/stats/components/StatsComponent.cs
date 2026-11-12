using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using lethal.common.registries.payloads;
using lethal.common.registries.tags;
using lethal.core.config;
using lethal.core.domain.characters;
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
    private readonly Dictionary<PayloadId, int> _disabledDefaultIdCounts = new();
    private readonly StatGraph _statGraph;
    private readonly ErrorConfig _errorConfig;
    private readonly PayloadFootprintHandler _footprintHandler;
    private readonly PayloadRegistry _payloadRegistry;
    private readonly HashSet<StatId> _scratch = new();
    private readonly Stack<List<ValidPayloadContext>> _validPayloadPool = new();

    private const string Source = nameof(StatsComponent);

    private StatsComponent(
        Dictionary<StatId, double> resolvedBaseStats, 
        StatGraph statGraph, 
        ErrorConfig errorConfig, 
        PayloadFootprintHandler footprintHandler, 
        PayloadRegistry payloadRegistry)
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
        foreach (var instance in payloadInstances) _footprintHandler.Of(instance.PayloadId);

        RemoveSource(sourceHandle);

        var sourceEntry = new StatSourceEntry(sourceTags);
        _activeSources[sourceHandle] = sourceEntry;

        foreach (var payloadInstance in payloadInstances)
        {
            if (InstallPayload(sourceEntry, payloadInstance)) continue;
            RemoveSource(sourceHandle);
            return false;
        }

        foreach (var payloadInstance in payloadInstances)
        {
            var definition = _payloadRegistry.Get(payloadInstance.PayloadId);
            if (definition.PayloadData is IDefaultDisablingPayload disablingPayload && !disablingPayload.DisabledDefaultIds.IsDefaultOrEmpty)
            {
                foreach (var disabledId in disablingPayload.DisabledDefaultIds)
                {
                    sourceEntry.AppliedDisables.Add(disabledId);
                    Disable(disabledId);
                }
            }
        }

        foreach (var sourceValue in sourceValues)
        {
            InstallSourceValue(sourceEntry, sourceValue);
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

    public bool RemoveSource(PayloadSourceHandle sourceHandle)
    {
        if (!_activeSources.Remove(sourceHandle, out var sourceEntry)) return false;

        foreach (var payloadInstance in sourceEntry.Instances)
        {
            var footprint = _footprintHandler.Of(payloadInstance.PayloadId);
            _statGraph.Remove(payloadInstance.PayloadId, footprint);

            var definition = _payloadRegistry.Get(payloadInstance.PayloadId);
            if (definition.PayloadData is IDefaultDisablingPayload disablingPayload && !disablingPayload.DisabledDefaultIds.IsDefaultOrEmpty)
            {
                foreach (var disabledId in sourceEntry.AppliedDisables)
                {
                    Enable(disabledId); 
                }
            }

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

    private void Disable(PayloadId id)
    {
        _disabledDefaultIdCounts[id] = _disabledDefaultIdCounts.GetValueOrDefault(id) + 1;
        if (_disabledDefaultIdCounts[id] == 1) DirtyWrites(id);
    }

    private void Enable(PayloadId id)
    {
        if (!_disabledDefaultIdCounts.TryGetValue(id, out var count)) return;
        if (count > 1) 
        { 
            _disabledDefaultIdCounts[id] = count - 1;
            return; 
        }
        _disabledDefaultIdCounts.Remove(id);
        DirtyWrites(id);
    }

    private bool IsDisabled(StatPayloadInstance payloadInstance) => _disabledDefaultIdCounts.Count > 0 && _disabledDefaultIdCounts.ContainsKey(payloadInstance.PayloadId);

    private void DirtyWrites(PayloadId id)
    {
        var footprint = _footprintHandler.Of(id);
        foreach (var stat in footprint.PrimaryWrites)
        {
            MarkDirty(stat);
        }
        foreach (var stat in footprint.SecondaryWrites) 
        {
            MarkDirty(stat);
        }
        foreach (var stat in footprint.Drains)
        {
            MarkDirty(stat);
        }
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
            EvaluateStat(lane, state, ref laneResult);
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

    private void GetValidPayloads(StatState state, List<ValidPayloadContext> results,bool checkSetsDefault = true, bool drainsOnly = false)
    {
        if (drainsOnly)
        {
            foreach (var instance in state.DrainInstances)
            {
                if (IsDisabled(instance)) continue;

                var footprint = _footprintHandler.Of(instance.PayloadId);
                if (footprint.SetsDefault) continue;

                var payloadData = _payloadRegistry.Get(instance.PayloadId).PayloadData;
                if (!PassesContextCheck(payloadData)) continue;

                results.Add(new ValidPayloadContext 
                { 
                    Instance = instance, 
                    PayloadData = payloadData, 
                    Value = instance.Primary, 
                    IsSecondary = false 
                });
            }
            return;
        }
        foreach (var instance in state.PrimaryInstances)
        {
            if (IsDisabled(instance)) continue;

            var footprint = _footprintHandler.Of(instance.PayloadId);
            if (checkSetsDefault && footprint.SetsDefault) continue;

            var payloadData = _payloadRegistry.Get(instance.PayloadId).PayloadData;
            if (!PassesContextCheck(payloadData)) continue;

            results.Add(new ValidPayloadContext 
            { 
                Instance = instance, 
                PayloadData = payloadData, 
                Value = instance.Primary, 
                IsSecondary = false 
            });
        }
        foreach (var instance in state.SecondaryInstances)
        {
            if (IsDisabled(instance)) continue;
            var footprint = _footprintHandler.Of(instance.PayloadId);
            if (checkSetsDefault && footprint.SetsDefault) continue;
            var payloadData = _payloadRegistry.Get(instance.PayloadId).PayloadData;
            if (!PassesContextCheck(payloadData)) continue;

            double value = payloadData is StatStaticPayload or StatOverridePayload or StatContributionPayload
                ? (instance.Secondary ?? instance.Primary)
                : instance.Primary;

            results.Add(new ValidPayloadContext 
            { 
                Instance = instance, 
                PayloadData = payloadData, 
                Value = value, 
                IsSecondary = true 
            });
        }
    }

    private StatResult EvaluateStat(StatStateLane lane, StatState state, ref StatLaneResult laneResult)
    {
        using var validPayloads = new PooledPayloadList(this);
        GetValidPayloads(state, validPayloads.List);
        ReadOnlySpan<ValidPayloadContext> standardPayloads = CollectionsMarshal.AsSpan(validPayloads.List);

        double baseValue = ResolveBaseValue(state, lane, standardPayloads);
        laneResult.Result = StatResult.Seed(baseValue);

        double accumulatedFlatValue = ResolveFlatValue(state, lane, standardPayloads);
        laneResult.Result = laneResult.Result.ApplyFlatValue(accumulatedFlatValue);

        double? flatOverrideValue = ResolveFlatOverrideValue(state, standardPayloads);
        if (flatOverrideValue is { } resolvedFlatOverrideValue) laneResult.Result = laneResult.Result.ApplyFlatOverrideValue(resolvedFlatOverrideValue);

        double accumulatedDerivedFlatValue = ResolveDerivedFlatValue(state, standardPayloads);
        laneResult.Result = laneResult.Result.ApplyDerivedPreConversionValue(accumulatedDerivedFlatValue);

        double accumulatedConversionFlatValue = ResolveConversionFlatValue(state, laneResult.Result, standardPayloads);
        laneResult.Result = laneResult.Result.ApplyConvertedValue(accumulatedConversionFlatValue);

        var (accumulatedRawAdditiveValue, accumulatedRawMultiplicativeValue) = ResolveMultiplierValue(state, standardPayloads);
        laneResult.Result = laneResult.Result.ApplyRawMultiplierValue(accumulatedRawAdditiveValue, accumulatedRawMultiplicativeValue);

        var (accumulatedMirroredAdditiveValue, accumulatedMirroredMultiplicativeValue) = ResolveMirroredMultiplierValue(state, standardPayloads);
        laneResult.Result = laneResult.Result.ApplyMirroredMultiplierValue(accumulatedMirroredAdditiveValue, accumulatedMirroredMultiplicativeValue);

        double accumulatedDerivedPostMultiplierValue = ResolveDerivedPostMultiplierValue(state, laneResult.Result, standardPayloads);
        laneResult.Result = laneResult.Result.ApplyDerivedPostMultiplierValue(accumulatedDerivedPostMultiplierValue);

        double accumulatedDerivedFinalValue = ResolveDerivedResolvedValue(state, laneResult.Result, standardPayloads);
        laneResult.Result = laneResult.Result.ApplyDerivedResolvedValue(accumulatedDerivedFinalValue);

        double? finalOverrideValue = ResolveFinalOverrideValue(state, standardPayloads);
        if (finalOverrideValue is { } resolvedFinalOverrideValue) laneResult.Result = laneResult.Result.ApplyFinalOverrideValue(resolvedFinalOverrideValue);

        return laneResult.Result;
    }

    private double ResolveBaseValue(StatState state, StatStateLane lane, ReadOnlySpan<ValidPayloadContext> standardPayloads)
    {
        double? largestPushedBaseValue = null;
        int pushedBaseCount = 0;

        foreach (var sourceValue in state.SourceValues)
        {
            if (sourceValue.IsBase && (sourceValue.Lane == lane || sourceValue.Lane == StatStateLane.Default))
            {
                pushedBaseCount++;
                if (largestPushedBaseValue is null || sourceValue.Value > largestPushedBaseValue.Value) largestPushedBaseValue = sourceValue.Value;
            }
        }

        if (pushedBaseCount > 1)
        {
            string statId = state.Id.ToString();
            var message = $"Stat '{statId}' has {pushedBaseCount} base value sources. Applying the largest value ({largestPushedBaseValue}).";
            _errorConfig.ReportError(Source, message);
        }

        double resolvedBase = largestPushedBaseValue ?? state.OriginalBaseValue;

        double? largestDefaultOverrideValue = null;
        int defaultOverrideCount = 0;

        foreach (var context in standardPayloads)
        {
            var footprint = _footprintHandler.Of(context.Instance.PayloadId);
            if (footprint.SetsDefault)
            {
                defaultOverrideCount++;
                if (largestDefaultOverrideValue is null || context.Value > largestDefaultOverrideValue.Value) largestDefaultOverrideValue = context.Value;
            }
        }

        if (defaultOverrideCount > 1)
        {
            string statId = state.Id.ToString();
            var message = $"Stat '{statId}' has {defaultOverrideCount} default override instances. Applying the largest value ({largestDefaultOverrideValue}).";
            _errorConfig.ReportError(Source, message);
        }

        state.DefaultOverrideValue = largestDefaultOverrideValue;

        double effectiveBase = state.DefaultOverrideValue ?? resolvedBase;

        return effectiveBase;
    }

    private double ResolveFlatValue(StatState state, StatStateLane lane, ReadOnlySpan<ValidPayloadContext> standardPayloads)
    {
        double accumulatedFlatValue = 0.0;

        foreach (var sourceValue in state.SourceValues)
        {
            if (!sourceValue.IsBase && (sourceValue.Lane == lane || sourceValue.Lane == StatStateLane.Default))
            {
                accumulatedFlatValue += sourceValue.Value;
            }
        }

        foreach (var context in standardPayloads)
        {
            accumulatedFlatValue += context.PayloadData switch
            {
                StatStaticPayload staticPayload when staticPayload.StatStaticType == StatStaticType.Flat => 
                    context.Value,

                StatContributionPayload contributionPayload when contributionPayload.StatStaticType == StatStaticType.Flat 
                    && contributionPayload.ContributionSourceType == ContributionSourceType.StaticStat => 
                        ResolveContributionSourceValue(contributionPayload) * context.Value,

                _ => 0.0
            };
        }

        return accumulatedFlatValue;
    }

    private double? ResolveFlatOverrideValue(StatState state, ReadOnlySpan<ValidPayloadContext> standardPayloads)
    {
        double? largestFlatOverrideValue = null;
        int flatOverrideCount = 0;

        foreach (var context in standardPayloads)
        {
            if (context.PayloadData is StatOverridePayload overridePayload && overridePayload.StatOverrideType == StatOverrideType.FlatOverride)
            {
                flatOverrideCount++;
                double resolvedValue = overridePayload.SourceStatId is { } sourceStatId
                    ? ResolveOverrideSourceValue(overridePayload, sourceStatId) * context.Value
                    : context.Value;

                if (largestFlatOverrideValue is null || resolvedValue > largestFlatOverrideValue.Value) largestFlatOverrideValue = resolvedValue;
            }
        }

        if (flatOverrideCount > 1)
        {
            string statId = state.Id.ToString();
            var message = $"Stat '{statId}' has {flatOverrideCount} flat override instances. Applying the largest value ({largestFlatOverrideValue}).";
            _errorConfig.ReportError(Source, message);
        }

        return largestFlatOverrideValue;
    }

    private double ResolveDerivedFlatValue(StatState state, ReadOnlySpan<ValidPayloadContext> standardPayloads)
    {
        double accumulatedDerivedFlatValue = 0.0;

        foreach (var context in standardPayloads)
        {
            accumulatedDerivedFlatValue += context.PayloadData switch
            {
                StatTranslationPayload translationPayload when translationPayload.StatTranslationType == StatTranslationType.DerivationPreMultipliers => 
                    ResolveDerivedSourceValue(translationPayload) * context.Value,

                StatMirrorPayload mirrorPayload when  mirrorPayload.StatStaticType == StatStaticType.Flat => 
                    ResolveMirrorSourceValue(mirrorPayload) * context.Value,

                _ => 0.0,
            };
        }

        return accumulatedDerivedFlatValue;
    }

    private double ResolveConversionFlatValue(StatState state, StatResult currentStatResult, ReadOnlySpan<ValidPayloadContext> standardPayloads)
    {
        double accumulatedConversionFlatValue = 0.0;

        foreach (var context in standardPayloads)
        {
            if (context.PayloadData is StatTranslationPayload translationPayload &&  translationPayload.StatTranslationType == StatTranslationType.Conversion)
                accumulatedConversionFlatValue += ResolveDerivedSourceValue(translationPayload) * context.Value;
        }

        using var validPayloads = new PooledPayloadList(this);
        GetValidPayloads(state, validPayloads.List, drainsOnly: true);
        ReadOnlySpan<ValidPayloadContext> drainInstancePayloads = CollectionsMarshal.AsSpan(validPayloads.List);

        foreach (var context in drainInstancePayloads)
        {
            if (context.PayloadData is StatTranslationPayload translationPayload && translationPayload.StatTranslationType == StatTranslationType.Conversion)
                accumulatedConversionFlatValue -= currentStatResult.DerivedPreConversionValue * context.Value;
        }

        return accumulatedConversionFlatValue;
    }

    private (double additive, double multiplicative) ResolveMultiplierValue(StatState state, ReadOnlySpan<ValidPayloadContext> standardPayloads)
    {
        double accumulatedRawAdditiveValue = 0.0;
        double accumulatedRawMultiplicativeValue = 1.0;

        foreach (var context in standardPayloads)
        {
            accumulatedRawAdditiveValue += context.PayloadData switch
            {
                StatStaticPayload staticPayload when staticPayload.StatStaticType == StatStaticType.PercentAdditive =>
                    context.Value,

                StatContributionPayload contributionPayload when contributionPayload.StatStaticType == StatStaticType.PercentAdditive 
                    && contributionPayload.ContributionSourceType == ContributionSourceType.StaticStat =>
                        ResolveContributionSourceValue(contributionPayload) * context.Value,

                _ => 0.0
            };

            accumulatedRawMultiplicativeValue *= 1.0 + context.PayloadData switch
            {
                StatStaticPayload staticPayload when staticPayload.StatStaticType == StatStaticType.PercentMultiplicative =>
                    context.Value,

                StatContributionPayload contributionPayload when contributionPayload.StatStaticType == StatStaticType.PercentMultiplicative 
                    && contributionPayload.ContributionSourceType == ContributionSourceType.StaticStat =>
                        ResolveContributionSourceValue(contributionPayload) * context.Value,

                _ => 0.0
            };
        }

        return (accumulatedRawAdditiveValue, accumulatedRawMultiplicativeValue - 1.0);
    }

    private (double additive, double multiplicative) ResolveMirroredMultiplierValue(StatState state, ReadOnlySpan<ValidPayloadContext> standardPayloads)
    {
        double accumulatedMirroredAdditiveValue = 0.0;
        double accumulatedMirroredMultiplicativeValue = 1.0;

        foreach (var context in standardPayloads)
        {
            if (context.PayloadData is StatMirrorPayload mirrorAdditivePayload &&  mirrorAdditivePayload.StatStaticType == StatStaticType.PercentAdditive)
                accumulatedMirroredAdditiveValue += ResolveMirrorSourceValue(mirrorAdditivePayload) * context.Value;

            if (context.PayloadData is StatMirrorPayload mirrorMultiplicativePayload &&  mirrorMultiplicativePayload.StatStaticType == StatStaticType.PercentMultiplicative)
                accumulatedMirroredMultiplicativeValue *= 1.0 + ResolveMirrorSourceValue(mirrorMultiplicativePayload) * context.Value;
        }

        return (accumulatedMirroredAdditiveValue, accumulatedMirroredMultiplicativeValue - 1.0);
    }

    private double ResolveDerivedPostMultiplierValue(StatState state, StatResult currentStatResult, ReadOnlySpan<ValidPayloadContext> standardPayloads)
    {
        double accumulatedDerivedPostMultiplierValue = 0.0;

        foreach (var context in standardPayloads)
        {
            if (context.PayloadData is StatTranslationPayload translationPayload &&  translationPayload.StatTranslationType == StatTranslationType.DerivationPostMultipliers)
                accumulatedDerivedPostMultiplierValue += ResolveDerivedSourceValue(translationPayload) * context.Value;
        }

        accumulatedDerivedPostMultiplierValue *= (1.0 + currentStatResult.MirroredAdditiveMultiplier) * (1.0 + currentStatResult.MirroredMultiplicativeMultiplier);

        return accumulatedDerivedPostMultiplierValue;
    }

    private double ResolveDerivedResolvedValue(StatState state, StatResult currentStatResult, ReadOnlySpan<ValidPayloadContext> standardPayloads)
    {
        double accumulatedDerivedFinalValue = 0.0;

        foreach (var context in standardPayloads)
        {
            if (context.PayloadData is StatTranslationPayload translationPayload &&  translationPayload.StatTranslationType == StatTranslationType.DerivationLast)
                accumulatedDerivedFinalValue += ResolveDerivedSourceValue(translationPayload) * context.Value;
        }

        accumulatedDerivedFinalValue *= (1.0 + currentStatResult.MirroredAdditiveMultiplier) * (1.0 + currentStatResult.MirroredMultiplicativeMultiplier);

        return accumulatedDerivedFinalValue;
    }

    private double? ResolveFinalOverrideValue(StatState state, ReadOnlySpan<ValidPayloadContext> standardPayloads)
    {
        double? largestFinalOverrideValue = null;
        int finalOverrideCount = 0;

        foreach (var context in standardPayloads)
        {
            if (context.PayloadData is StatOverridePayload overridePayload && overridePayload.StatOverrideType == StatOverrideType.FinalOverride)
            {
                finalOverrideCount++;
                double resolvedValue = overridePayload.SourceStatId is { } sourceStatId
                    ? ResolveOverrideSourceValue(overridePayload, sourceStatId) * context.Value
                    : context.Value;

                if (largestFinalOverrideValue is null || resolvedValue > largestFinalOverrideValue.Value) largestFinalOverrideValue = resolvedValue;
            }
        }

        if (finalOverrideCount > 1)
        {
            string statId = state.Id.ToString();
            var message = $"Stat '{statId}' has {finalOverrideCount} final override instances. Applying the largest value ({largestFinalOverrideValue}).";
            _errorConfig.ReportError(Source, message);
        }

        return largestFinalOverrideValue;
    }

    private double ResolveOverrideSourceValue(StatOverridePayload overridePayload, StatId sourceStatId)
    {
        var (laneA, laneB) = GetStatResultsAcrossLanes(sourceStatId);

        return overridePayload.StatOverrideType switch
        {
            StatOverrideType.FlatOverride => AverageLanes(laneA.FlatTotalValue, laneB?.FlatTotalValue),
            StatOverrideType.FinalOverride => AverageLanes(laneA.ResolvedValue, laneB?.ResolvedValue),
            _ => AverageLanes(laneA.FinalValue, laneB?.FinalValue)
        };
    }

    private double ResolveContributionSourceValue(StatContributionPayload contributionPayload)
    {
        var (laneA, laneB) = GetStatResultsAcrossLanes(contributionPayload.SourceStatId);

        double sourceValue = AverageLanes(laneA.FlatTotalValue, laneB?.FlatTotalValue);

        return sourceValue;
    }

    private double ResolveDerivedSourceValue(StatTranslationPayload translationPayload)
    {
        var (laneA, laneB) = GetStatResultsAcrossLanes(translationPayload.SourceStatId);
        
        double sourceValue = translationPayload.StatTranslationType switch
        {
            StatTranslationType.Conversion => AverageLanes(laneA.DerivedPreConversionValue, laneB?.DerivedPreConversionValue),
            
            StatTranslationType.DerivationPreMultipliers => AverageLanes(laneA.FlatTotalValue, laneB?.FlatTotalValue),

            StatTranslationType.DerivationPostMultipliers => AverageLanes(laneA.PostMultiplierValue, laneB?.PostMultiplierValue),

            StatTranslationType.DerivationLast => AverageLanes(laneA.FinalValue, laneB?.FinalValue),

            _ => 0.0
        };

        return sourceValue;
    }

    private double ResolveMirrorSourceValue(StatMirrorPayload mirrorPayload)
    {
        var (laneA, laneB) = GetStatResultsAcrossLanes(mirrorPayload.SourceStatId);

        double sourceValue = mirrorPayload.StatStaticType switch
        {
            StatStaticType.Flat => AverageLanes(laneA.RawAddedFlat, laneB?.RawAddedFlat),

            StatStaticType.PercentAdditive => AverageLanes(laneA.RawAdditiveMultiplier, laneB?.RawAdditiveMultiplier),

            StatStaticType.PercentMultiplicative => AverageLanes(laneA.RawMultiplicativeMultiplier, laneB?.RawMultiplicativeMultiplier),

            _ => 0.0
        };

        return sourceValue;
    }

    private bool PassesContextCheck(IPayload payload)
    {
        if (payload is not IContextualPayload { ContextId: { IsDefault: false } contextualId }) return true; 
        
        //implement later
        return true;
    }

    private double AverageLanes(double valueA, double? valueB) => valueB is { } b ? (valueA + b) / 2.0 : valueA;
}