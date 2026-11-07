using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using lethal.common.registries.payloads;
using lethal.common.registries.tags;
using lethal.core.config;
using lethal.core.domain.payloads;
using lethal.core.domain.payloads.enums;
using lethal.core.domain.stats;
using lethal.core.domain.tags;
using lethal.gameplay.stats.components.stat_dependencies.enums;

namespace lethal.gameplay.stats.components.stat_dependencies;
public sealed class PayloadFootprintHandler
{
	private readonly PayloadRegistry _payloads;
    private readonly StatRegistry _stats;
    private readonly Dictionary<PayloadId, PayloadFootprint> _cache = new();

	private const string Source = nameof(PayloadFootprintHandler);

    public PayloadFootprintHandler(PayloadRegistry payloads, StatRegistry stats)
    {
        _payloads = payloads;
        _stats = stats;
    }

    public PayloadFootprint Of(PayloadId id)
	{
    	if (_cache.TryGetValue(id, out var footprint)) return footprint;

    	footprint = Describe(id, _payloads.Get(id).PayloadData);

    	if (footprint.PrimaryWrites.IsEmpty) throw new InvalidOperationException($"[{Source}]: Payload '{id}' doesn't resolve to any stat (check its target stat and target tags).");

    	foreach (var (read, _) in footprint.Reads)
		{
        	if (!_stats.Contains(read)) throw new InvalidOperationException($"[{Source}]: Payload '{id}' reads unknown stat '{read}'.");
		}

    	return _cache[id] = footprint;
	}

    private PayloadFootprint Describe(PayloadId id, IPayload payload)
	{
    	static IEnumerable<(StatId, EdgeKind)> From(StatId? stat, EdgeKind kind) => stat is { } s ? [(s, kind)] : [];
    	//static IEnumerable<(StatId, EdgeKind)> FromMany(IEnumerable<StatId?> stats, EdgeKind kind) => stats.Where(s => s.HasValue).Select(s => (s!.Value, kind));

    	return payload switch
    	{
        	StatStaticPayload footprint => Make(id, footprint.TargetStatId, footprint.TargetTags, footprint.ExcludedTags, footprint.TargetWithoutCounterpart, footprint.SecondaryMinValue.HasValue, []),

        	//StatScaledPayload footprint => Make(id, footprint.TargetStatId, footprint.TargetTags, footprint.ExcludedTags, footprint.TargetWithoutCounterpart, footprint.SecondaryScalingFormulaMin is not null,
            //	[.. FromMany(footprint.ScalingVariables.Select(v => v.StatId), EdgeKind.Evaluation)]),

        	StatOverridePayload footprint => Make(id, footprint.TargetStatId, TagFilter.Empty, [], footprint.TargetWithoutCounterpart,footprint.SecondaryMinValue.HasValue,
            	[.. From(footprint.SourceStatId, EdgeKind.Evaluation)]) with 
				{ 
					SetsDefault = footprint.StatOverrideType == StatOverrideType.DefaultOverride 
				},

        	StatTranslationPayload footprint => Make(id, footprint.TargetStatId, TagFilter.Empty, [], footprint.TargetWithoutCounterpart,false,
            	[.. From(footprint.SourceStatId, EdgeKind.Evaluation)]) with 
				{ 
					Drains = footprint.StatTranslationType == StatTranslationType.Conversion
    					? [footprint.SourceStatId ?? throw new InvalidOperationException($"[{Source}]: Payload '{id}' is a conversion but has no source stat.")]
    					: ImmutableArray<StatId>.Empty
				},

        	StatContributionPayload footprint => Make(id, footprint.TargetStatId, footprint.TargetTags, footprint.ExcludedTags, footprint.TargetWithoutCounterpart, footprint.SecondaryMinValue.HasValue,
            	[.. footprint.ContributionSourceType == ContributionSourceType.StaticStat
                	? From(footprint.SourceStatId, EdgeKind.Evaluation)
                	: From(footprint.SourceStatId, EdgeKind.Evaluation).Concat(From(footprint.SourceStatId, EdgeKind.Resource))]),

        	StatMirrorPayload footprint => Make(id, footprint.TargetStatId, footprint.TargetTags, footprint.ExcludedTags, footprint.TargetWithoutCounterpart, false,
            	[.. From(footprint.SourceStatId, EdgeKind.Mirror)]),

        	_ => throw new NotSupportedException($"No graph rules for {payload.GetType().Name}.")
    	};
	}

    private ImmutableArray<StatId> Resolve(PayloadId id, StatId? stat, TagFilter tags, ImmutableArray<TagId> excluded)
	{
		var matched = ImmutableArray.CreateBuilder<StatId>();

		if (stat is {} explicitTarget)
		{
			if (!_stats.Contains(explicitTarget))  throw new InvalidOperationException($"[{Source}]: Payload '{id}' targets unknown stat '{explicitTarget}'.");
			matched.Add(explicitTarget);
		}

		if (!tags.All.IsDefaultOrEmpty || !tags.Any.IsDefaultOrEmpty)
    	{
        	foreach (var statEntry in _stats.All)
        	{
            	if (_stats.TagsOf(statEntry.Id).Matches(tags, excluded))
            	{
                	if (stat is null || statEntry.Id != stat.Value)
                	{
                    	matched.Add(statEntry.Id);
                	}
            	}
        	}
    	}

    	return matched.ToImmutable();
	}


	private PayloadFootprint Make(
		PayloadId id, 
		StatId? stat, 
		TagFilter tags, 
		ImmutableArray<TagId> excluded,
    	bool withoutCounterpart, 
		bool hasSecondary, 
		ImmutableArray<(StatId Stat, EdgeKind Kind)> reads)
	{
    	var writes = Resolve(id, stat, tags, excluded);
    	bool noTags = tags.All.IsDefaultOrEmpty && tags.Any.IsDefaultOrEmpty;

    	if (hasSecondary && (withoutCounterpart || stat is null || !noTags)) throw new InvalidOperationException(
            $"[{Source}]: Payload '{id}': secondary values need one explicit stat, no tags, and no 'without counterpart' flag.");

    	var secondary = ImmutableArray.CreateBuilder<StatId>();
    	if (!withoutCounterpart) 
		{
        	foreach (var write in writes) 
			{
            	if (_stats.Get(write).PairedCounterpartId is { } counterpart
                	&& !writes.Contains(counterpart) && !secondary.Contains(counterpart))
                	secondary.Add(counterpart);
			}
		}

    	if (hasSecondary && secondary.Count == 0) throw new InvalidOperationException($"[{Source}]: Payload '{id}' has secondary values but its target isn't range-paired.");

    	return new PayloadFootprint(writes, reads) { SecondaryWrites = secondary.ToImmutable() };
	}

	public void Warm()
	{
    	foreach (var definition in _payloads.InGroup(PayloadBehaviorGroup.Stat)) Of(definition.Id);
	}

	private void HandleError(string message, ErrorConfig errorConfig)
	{
    	if (errorConfig.StrictValidation) throw new InvalidOperationException(message);
    	else errorConfig.OnWarning(message);
	}
}

