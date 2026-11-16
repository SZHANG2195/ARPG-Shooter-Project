using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

    public bool InstallSource(PayloadSourceHandle sourceHandle, TagSet sourceTags, IReadOnlyList<StatPayloadInstance> payloadInstances, IReadOnlyList<StatSourceValue> sourceValues)
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
            if (definition.PayloadData is IDefaultDisablingPayload { DisabledDefaultIds.IsDefaultOrEmpty : false }disablingPayload)
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
            InstallSourceValue(sourceEntry, sourceValue with { SourceHandle = sourceHandle, SourceTags = sourceTags });
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

        if (sourceValue.Lane != StatStateLane.Default) state.LanePushCount++;
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
            int removed = _states[stat].SourceValues.RemoveAll(sourceValue => sourceValue.SourceHandle == sourceHandle && sourceValue.Lane == lane);
            if (lane != StatStateLane.Default) _states[stat].LanePushCount -= removed;
            if (lane == StatStateLane.Default) MarkDirty(stat);
            else MarkLaneDirty(stat, lane);
        }

        foreach (var disabledId in sourceEntry.AppliedDisables) Enable(disabledId);

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

        if (lane != StatStateLane.Default && state.LanePushCount == 0) lane = StatStateLane.Default;

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

        return ref state.EnsureLanes()[(int)lane - 1];
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

    private (double laneA, double? laneB) GetValidSourceValueSum(StatState sourceState, TagFilter requirementFilter, ImmutableArray<TagId> excludedTags)
    {
        double laneASourceValue = 0.0;
        double? laneBSourceValue = null;

        bool isDualWielding = false;
        if (isDualWielding)
        {
            laneBSourceValue = 0.0;
            foreach (var sourceValue in sourceState.SourceValues)
            {
                if (sourceValue.SourceTags is { IsEmpty: false } tags && tags.Matches(requirementFilter, excludedTags))
                {
                    if (sourceValue.Lane == StatStateLane.Mainhand) laneASourceValue += sourceValue.Value;
                    else if (sourceValue.Lane == StatStateLane.Offhand) laneBSourceValue += sourceValue.Value;
                    else 
                    { 
                        laneASourceValue += sourceValue.Value; 
                        laneBSourceValue += sourceValue.Value; 
                    }
                }
            }
        }
        else
        {
            foreach (var sourceValue in sourceState.SourceValues)
            {
                if (sourceValue.SourceTags is { IsEmpty: false } tags && tags.Matches(requirementFilter, excludedTags))
                {
                    if (sourceValue.Lane == StatStateLane.Default)
                    {
                        laneASourceValue += sourceValue.Value;
                    }
                }
            }
        }

        return (laneASourceValue, laneBSourceValue);
    }



    private void GetValidPayloads(
        StatState state, 
        List<ValidPayloadContext> standard, 
        List<ValidPayloadContext>? scoped, 
        bool drainsOnly = false)
    {
        void ProcessInstance(StatPayloadInstance instance, List<ValidPayloadContext> standard, List<ValidPayloadContext>? scoped, bool isSecondary)
        {
            if (IsDisabled(instance)) return;

            var footprint = _footprintHandler.Of(instance.PayloadId);

            var payloadData = _payloadRegistry.Get(instance.PayloadId).PayloadData;
            if (!PassesContextCheck(payloadData)) return;

            bool hasRequirements = HasAnyRequirements(payloadData);

            double value = isSecondary && (payloadData is StatStaticPayload or StatOverridePayload or StatContributionPayload)
                ? (instance.Secondary ?? instance.Primary)
                : instance.Primary;
            
            var context = new ValidPayloadContext 
            { 
                Instance = instance, 
                PayloadData = payloadData, 
                Value = value, 
                IsSecondary = isSecondary,
                SetsDefault = footprint.SetsDefault 
            };

            if (hasRequirements) scoped?.Add(context); 
            else standard.Add(context);
        }

        bool HasAnyRequirements(IPayload payloadData)
        {
            if (payloadData is ITargetRequirementPayload { TargetRequirementTags.IsEmpty: false } targetRequirement) return true;
            if (payloadData is ISourceRequirementPayload { SourceRequirementTags.IsEmpty: false } sourceRequirement) return true;
            return false;
        }

        if (drainsOnly)
        {
            foreach (var instance in state.DrainInstances)
            {
                ProcessInstance(instance, standard, scoped, isSecondary: false);
            }
            return;
        }
        foreach (var instance in state.PrimaryInstances)
        {
            ProcessInstance(instance, standard, scoped, isSecondary: false);
        }
        foreach (var instance in state.SecondaryInstances)
        {
            ProcessInstance(instance, standard, scoped, isSecondary: true);
        }
    }

    private StatResult EvaluateStat(StatStateLane lane, StatState state, ref StatLaneResult laneResult)
    {
        if (state.HasNoModifiers)
        {
            laneResult.Result = StatResult.Seed(state.OriginalBaseValue);
            return laneResult.Result;
        }

        if (state.HasOnlySourceValues)
        {
            ReadOnlySpan<ValidPayloadContext> emptyPayloads = ReadOnlySpan<ValidPayloadContext>.Empty;
        
            double sourceBaseValue = ResolveBaseValue(state, lane, emptyPayloads);
            double flatValue = ResolveFlatValue(state, lane, emptyPayloads);

            laneResult.Result = StatResult.Seed(sourceBaseValue).ApplyFlatValue(flatValue);
            return laneResult.Result;
        }

        using var standardList = PooledPayloadList.Rent();
        using var scopedList = PooledPayloadList.Rent();
        GetValidPayloads(state, standardList.List, scopedList.List);
        ReadOnlySpan<ValidPayloadContext> standardPayloads = CollectionsMarshal.AsSpan(standardList.List);
        ReadOnlySpan<ValidPayloadContext> scopedPayloads = CollectionsMarshal.AsSpan(scopedList.List);

        double baseValue = ResolveBaseValue(state, lane, standardPayloads);
        laneResult.Result = StatResult.Seed(baseValue);

        double accumulatedFlatValue = ResolveFlatValue(state, lane, standardPayloads);
        double accumulatedScopedFlatValue = ResolveScopedFlatValue(scopedPayloads);
        laneResult.Result = laneResult.Result.ApplyFlatValue(accumulatedFlatValue + accumulatedScopedFlatValue);

        double? flatOverrideValue = ResolveFlatOverrideValue(state, standardPayloads);
        if (flatOverrideValue is { } resolvedFlatOverrideValue) laneResult.Result = laneResult.Result.ApplyFlatOverrideValue(resolvedFlatOverrideValue);

        double accumulatedDerivedFlatValue = ResolveDerivedFlatValue(standardPayloads);
        laneResult.Result = laneResult.Result.ApplyDerivedPreConversionValue(accumulatedDerivedFlatValue);

        var (accumulatedConversionSubtractedFlatValue, totalConversionDrainRatio, conversionNormalizationRatio) = ResolveConversionSubtractedFlatValue(state, laneResult.Result, standardPayloads);
        laneResult.Result = laneResult.Result.ApplyConversionSubtractedValue(accumulatedConversionSubtractedFlatValue, totalConversionDrainRatio, conversionNormalizationRatio);

        double accumulatedConversionGainedFlatValue = ResolveConversionGainedFlatValue(standardPayloads);
        laneResult.Result = laneResult.Result.ApplyConversionGainedValue(accumulatedConversionGainedFlatValue);

        var (accumulatedRawAdditiveValue, accumulatedRawMultiplicativeValue) = ResolveMultiplierValue(standardPayloads);
        var (accumulatedScopedAdditiveValue, accumulatedScopedMultiplicativeValue) = ResolveScopedMultiplierValue(scopedPayloads);
        laneResult.Result = laneResult.Result.ApplyRawMultiplierValue(accumulatedRawAdditiveValue + accumulatedScopedAdditiveValue, accumulatedRawMultiplicativeValue, accumulatedScopedMultiplicativeValue);

        var (accumulatedMirroredAdditiveValue, accumulatedMirroredMultiplicativeValue) = ResolveMirroredMultiplierValue(standardPayloads);
        laneResult.Result = laneResult.Result.ApplyMirroredMultiplierValue(accumulatedMirroredAdditiveValue, accumulatedMirroredMultiplicativeValue);

        double accumulatedScopedDeltaValue = ResolveTargetScopedValue(state, lane, laneResult.Result, scopedPayloads);
        laneResult.Result = laneResult.Result.ApplyTargetScopedValue(accumulatedScopedDeltaValue);

        double accumulatedDerivedPostMultiplierValue = ResolveDerivedPostMultiplierValue(laneResult.Result, standardPayloads);
        laneResult.Result = laneResult.Result.ApplyDerivedPostMultiplierValue(accumulatedDerivedPostMultiplierValue);

        double accumulatedDerivedFinalValue = ResolveDerivedResolvedValue(laneResult.Result, standardPayloads);
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
            if (context.SetsDefault)
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
                StatStaticPayload { StatStaticType: StatStaticType.Flat } => 
                    context.Value,

                StatContributionPayload { StatStaticType: StatStaticType.Flat, ContributionSourceType: ContributionSourceType.StaticStat } contributionPayload => 
                    ResolveContributionSourceValue(contributionPayload) * context.Value,

                _ => 0.0
            };
        }

        return accumulatedFlatValue;
    }

    private double ResolveScopedFlatValue(ReadOnlySpan<ValidPayloadContext> scopedPayloads)
    {
        double accumulatedScopedFlatValue = 0.0;

        foreach (var context in scopedPayloads)
        {
            accumulatedScopedFlatValue += context.PayloadData switch
            {
                StatStaticPayload { StatStaticType: StatStaticType.Flat } => 
                    context.Value,

                StatContributionPayload { StatStaticType: StatStaticType.Flat, ContributionSourceType: ContributionSourceType.StaticStat } contributionPayload =>
                    ResolveScopedContributionSourceValue(contributionPayload, contributionPayload.SourceRequirementTags) * context.Value,

                _ => 0.0
            };
        }

        return accumulatedScopedFlatValue;
    }

    private double? ResolveFlatOverrideValue(StatState state, ReadOnlySpan<ValidPayloadContext> standardPayloads)
    {
        double? largestFlatOverrideValue = null;
        int flatOverrideCount = 0;

        foreach (var context in standardPayloads)
        {
            if (context.PayloadData is StatOverridePayload { StatOverrideType: StatOverrideType.FlatOverride } overridePayload)
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

        state.FlatOverrideValue = largestFlatOverrideValue;

        return largestFlatOverrideValue;
    }

    private double ResolveDerivedFlatValue(ReadOnlySpan<ValidPayloadContext> standardPayloads)
    {
        double accumulatedDerivedFlatValue = 0.0;

        foreach (var context in standardPayloads)
        {
            accumulatedDerivedFlatValue += context.PayloadData switch
            {
                StatTranslationPayload { StatTranslationType: StatTranslationType.DerivationPreMultipliers } translationPayload => 
                    ResolveDerivedSourceValue(translationPayload) * context.Value,

                StatMirrorPayload { StatStaticType: StatStaticType.Flat } mirrorPayload => 
                    ResolveMirrorSourceValue(mirrorPayload) * context.Value,

                _ => 0.0,
            };
        }

        return accumulatedDerivedFlatValue;
    }

    private (double subtracted, double drain, double normalization) ResolveConversionSubtractedFlatValue(StatState state, StatResult currentStatResult, ReadOnlySpan<ValidPayloadContext> standardPayloads)
    {
        double accumulatedConversionSubtractedFlatValue = 0.0;
        double totalConversionDrainRatio = 0.0;
        double conversionNormalizationRatio = 1.0;

        using var validPayloads = PooledPayloadList.Rent();
        GetValidPayloads(state, validPayloads.List, null, drainsOnly: true);
        ReadOnlySpan<ValidPayloadContext> drainInstancePayloads = CollectionsMarshal.AsSpan(validPayloads.List);

        foreach (var context in drainInstancePayloads)
        {
            if (context.PayloadData is StatTranslationPayload { StatTranslationType: StatTranslationType.Conversion } translationPayload)
                totalConversionDrainRatio += context.Value;
        }

        if (totalConversionDrainRatio > 1.0) conversionNormalizationRatio /= totalConversionDrainRatio;

        foreach (var context in drainInstancePayloads)
        {
            if (context.PayloadData is StatTranslationPayload { StatTranslationType: StatTranslationType.Conversion } translationPayload)
                accumulatedConversionSubtractedFlatValue -= currentStatResult.DerivedPreConversionValue * context.Value * conversionNormalizationRatio;
        }

        return (accumulatedConversionSubtractedFlatValue, totalConversionDrainRatio, conversionNormalizationRatio);
    }

    private double ResolveConversionGainedFlatValue(ReadOnlySpan<ValidPayloadContext> standardPayloads)
    {
        double accumulatedConversionGainedFlatValue = 0.0;

        foreach (var context in standardPayloads)
        {
            if (context.PayloadData is StatTranslationPayload { StatTranslationType: StatTranslationType.Conversion } translationPayload)
                accumulatedConversionGainedFlatValue += ResolveConversionSourceValue(translationPayload) * context.Value;
        }

        return accumulatedConversionGainedFlatValue;
    }

    private (double additive, double multiplicative) ResolveMultiplierValue(ReadOnlySpan<ValidPayloadContext> standardPayloads)
    {
        double accumulatedRawAdditiveValue = 0.0;
        double accumulatedRawMultiplicativeValue = 1.0;

        foreach (var context in standardPayloads)
        {
            accumulatedRawAdditiveValue += context.PayloadData switch
            {
                StatStaticPayload { StatStaticType: StatStaticType.PercentAdditive } =>
                    context.Value,

                StatContributionPayload { StatStaticType: StatStaticType.PercentAdditive, ContributionSourceType: ContributionSourceType.StaticStat } contributionPayload =>
                    ResolveContributionSourceValue(contributionPayload) * context.Value,

                _ => 0.0
            };

            accumulatedRawMultiplicativeValue *= 1.0 + context.PayloadData switch
            {
                StatStaticPayload { StatStaticType: StatStaticType.PercentMultiplicative } =>
                    context.Value,

                StatContributionPayload { StatStaticType: StatStaticType.PercentMultiplicative, ContributionSourceType: ContributionSourceType.StaticStat } contributionPayload  =>
                    ResolveContributionSourceValue(contributionPayload) * context.Value,

                _ => 0.0
            };
        }

        return (accumulatedRawAdditiveValue, accumulatedRawMultiplicativeValue - 1.0);
    }

    private (double additive, double multiplicative) ResolveScopedMultiplierValue(ReadOnlySpan<ValidPayloadContext> scopedPayloads)
    {
        double accumulatedScopedAdditiveValue = 0.0;
        double accumulatedScopedMultiplicativeValue = 1.0;

        foreach (var context in scopedPayloads)
        {
            if (context.PayloadData is ITargetRequirementPayload { TargetRequirementTags.IsEmpty: false }) continue;

            if (context.PayloadData is StatContributionPayload { StatStaticType: StatStaticType.PercentAdditive } contributionAdditivePayload)
                accumulatedScopedAdditiveValue += ResolveScopedContributionSourceValue(contributionAdditivePayload, contributionAdditivePayload.SourceRequirementTags) * context.Value;
            
            if (context.PayloadData is StatContributionPayload { StatStaticType: StatStaticType.PercentMultiplicative } contributionMultiplicativePayload)
                accumulatedScopedMultiplicativeValue *= 1.0 + ResolveScopedContributionSourceValue(contributionMultiplicativePayload, contributionMultiplicativePayload.SourceRequirementTags) * context.Value;
        }

        return (accumulatedScopedAdditiveValue, accumulatedScopedMultiplicativeValue - 1.0);
    }

    private (double additive, double multiplicative) ResolveMirroredMultiplierValue(ReadOnlySpan<ValidPayloadContext> standardPayloads)
    {
        double accumulatedMirroredAdditiveValue = 0.0;
        double accumulatedMirroredMultiplicativeValue = 1.0;

        foreach (var context in standardPayloads)
        {
            if (context.PayloadData is StatMirrorPayload { StatStaticType: StatStaticType.PercentAdditive } mirrorAdditivePayload)
                accumulatedMirroredAdditiveValue += ResolveMirrorSourceValue(mirrorAdditivePayload) * context.Value;

            if (context.PayloadData is StatMirrorPayload { StatStaticType: StatStaticType.PercentMultiplicative } mirrorMultiplicativePayload)
                accumulatedMirroredMultiplicativeValue *= 1.0 + ResolveMirrorSourceValue(mirrorMultiplicativePayload) * context.Value;
        }

        return (accumulatedMirroredAdditiveValue, accumulatedMirroredMultiplicativeValue - 1.0);
    }

    private double ResolveTargetScopedValue(StatState state, StatStateLane lane, StatResult currentStatResult, ReadOnlySpan<ValidPayloadContext> scopedPayloads)
    {
        double ResolveTargetContributionQuantity(StatContributionPayload contributionPayload) => contributionPayload.SourceRequirementTags.IsEmpty
            ? ResolveContributionSourceValue(contributionPayload)
            : ResolveScopedContributionSourceValue(contributionPayload, contributionPayload.SourceRequirementTags);  

        if (state.FlatOverrideValue.HasValue) return 0.0;

        var sourceValues = state.SourceValues;
        int sourceCount = sourceValues.Count;
        if (sourceCount == 0) return 0.0;

        Span<ScopedSourceStamp> sourceStamps = sourceCount <= 16 ? stackalloc ScopedSourceStamp[16] : new ScopedSourceStamp[sourceCount];
        sourceStamps.Fill(ScopedSourceStamp.Identity);

        bool hasStamp = false;

        foreach(var context in scopedPayloads)
        {
            if (context.PayloadData is not ITargetRequirementPayload { TargetRequirementTags.IsEmpty: false } targetRequirementPayload) continue;

            var (statStaticType, value, excludedTags) = context.PayloadData switch
            {
                StatStaticPayload staticPayload => (staticPayload.StatStaticType, context.Value, staticPayload.ExcludedTags),

                StatContributionPayload { ContributionSourceType: ContributionSourceType.StaticStat } contributionPayload => 
                    (contributionPayload.StatStaticType, ResolveTargetContributionQuantity(contributionPayload) * context.Value, contributionPayload.ExcludedTags),

                _ => (StatStaticType.Flat, 0.0, ImmutableArray<TagId>.Empty)
            };
            if (statStaticType == StatStaticType.Flat) continue;

            for (int i = 0; i < sourceCount; i++)
            {
                var sourceValue = sourceValues[i];

                if (state.DefaultOverrideValue.HasValue && sourceValue.IsBase) continue;

                if (!(sourceValue.Lane == StatStateLane.Default || sourceValue.Lane == lane)) continue;

                if (sourceValue.SourceTags is not { IsEmpty: false } tags || !tags.Matches(targetRequirementPayload.TargetRequirementTags, excludedTags)) continue;

                hasStamp = true;

                sourceStamps[i].Apply(statStaticType, value);
            }
        }

        if (!hasStamp) return 0.0;

        double remainderAfterConversion = 1.0 - (currentStatResult.TotalConversionDrainRatio * currentStatResult.ConversionNormalizationRatio);

        double resolvedTotal = 0.0;

        for (int i = 0; i < sourceCount; i++) 
        {
            var sourceValue = sourceValues[i];

            if (!(sourceValue.Lane == StatStateLane.Default || sourceValue.Lane == lane)) continue;

            var sourceStamp = sourceStamps[i];

            if (sourceStamp.Additive == 0.0 && Math.Abs(sourceStamp.More - 1.0) < 1e-9) continue;

            double baselineValue = sourceValue.Value 
                * (1.0 + currentStatResult.TotalAdditiveMultiplier) 
                * (1.0 + currentStatResult.TotalMultiplicativeMultiplier) 
                * remainderAfterConversion;
            
            double totalValue = sourceValue.Value 
                * (1.0 + currentStatResult.TotalAdditiveMultiplier + sourceStamp.Additive) 
                * (1.0 + currentStatResult.TotalMultiplicativeMultiplier) 
                * sourceStamp.More 
                * remainderAfterConversion;

            resolvedTotal += totalValue - baselineValue;

        }

        return resolvedTotal;
    }

    private double ResolveDerivedPostMultiplierValue(StatResult currentStatResult, ReadOnlySpan<ValidPayloadContext> standardPayloads)
    {
        double accumulatedDerivedPostMultiplierValue = 0.0;

        foreach (var context in standardPayloads)
        {
            if (context.PayloadData is StatTranslationPayload { StatTranslationType: StatTranslationType.DerivationPostMultipliers } translationPayload)
                accumulatedDerivedPostMultiplierValue += ResolveDerivedSourceValue(translationPayload) * context.Value;
        }

        accumulatedDerivedPostMultiplierValue *= (1.0 + currentStatResult.TotalAdditiveMultiplier) * (1.0 + currentStatResult.TotalMultiplicativeMultiplier);

        return accumulatedDerivedPostMultiplierValue;
    }

    private double ResolveDerivedResolvedValue(StatResult currentStatResult, ReadOnlySpan<ValidPayloadContext> standardPayloads)
    {
        double accumulatedDerivedFinalValue = 0.0;

        foreach (var context in standardPayloads)
        {
            if (context.PayloadData is StatTranslationPayload { StatTranslationType: StatTranslationType.DerivationLast } translationPayload)
                accumulatedDerivedFinalValue += ResolveDerivedSourceValue(translationPayload) * context.Value;
        }

        accumulatedDerivedFinalValue *= (1.0 + currentStatResult.TotalAdditiveMultiplier) * (1.0 + currentStatResult.TotalMultiplicativeMultiplier);

        return accumulatedDerivedFinalValue;
    }

    private double? ResolveFinalOverrideValue(StatState state, ReadOnlySpan<ValidPayloadContext> standardPayloads)
    {
        double? largestFinalOverrideValue = null;
        int finalOverrideCount = 0;

        foreach (var context in standardPayloads)
        {
            if (context.PayloadData is StatOverridePayload { StatOverrideType: StatOverrideType.FinalOverride } overridePayload)
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

        state.FinalOverrideValue = largestFinalOverrideValue;

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

    private double ResolveScopedContributionSourceValue(StatContributionPayload contributionPayload, TagFilter requirementFilter)
    {
        var (laneA, laneB) = GetValidSourceValueSum(_states[contributionPayload.SourceStatId], requirementFilter, contributionPayload.ExcludedTags);

        double sourceValue = AverageLanes(laneA, laneB);

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

    private double ResolveConversionSourceValue(StatTranslationPayload conversionPayload)
    {
        var (laneA, laneB) = GetStatResultsAcrossLanes(conversionPayload.SourceStatId);

        double conversionNormalizationRatio = AverageLanes(laneA.ConversionNormalizationRatio, laneB?.ConversionNormalizationRatio);

        double sourceValue = AverageLanes(laneA.DerivedPreConversionValue, laneB?.DerivedPreConversionValue);

        double normalizedSourceValue = sourceValue * conversionNormalizationRatio;

        return normalizedSourceValue;

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