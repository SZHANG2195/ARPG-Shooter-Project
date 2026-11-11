using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using lethal.core.config;
using lethal.core.domain.payloads;
using lethal.core.domain.payloads.enums;
using lethal.core.domain.tags;

namespace lethal.common.registries.payloads;
public sealed class PayloadRegistry
{
	private readonly Registry<PayloadId, PayloadDefinition> _all;
	private readonly FrozenDictionary<PayloadBehaviorGroup, ImmutableArray<PayloadDefinition>> _byBehaviorType;
	private readonly FrozenDictionary<TagId, ImmutableArray<PayloadDefinition>> _defaultsByTag;
	private readonly ErrorConfig _errorConfig;

	private const string Source = nameof(PayloadRegistry);

	private PayloadRegistry(
		Registry<PayloadId, PayloadDefinition> all, 
		FrozenDictionary<PayloadBehaviorGroup, ImmutableArray<PayloadDefinition>> byBehaviorType, 
		FrozenDictionary<TagId, ImmutableArray<PayloadDefinition>> defaultsByTag, 
		ErrorConfig errorConfig)
    {
        _all = all;
        _byBehaviorType = byBehaviorType;
		_defaultsByTag = defaultsByTag;
        _errorConfig = errorConfig;
    }

	public static PayloadRegistry Build(IEnumerable<PayloadDefinition> definitions, ErrorConfig errorConfig)
	{
    	var all = Registry<PayloadId, PayloadDefinition>.Build(definitions, definition => definition.Id, "DefinitionPayloads", errorConfig);
    	var byBehaviorType = all.All
			.GroupBy(payload => payload.PayloadBehaviorGroup)
			.ToFrozenDictionary(behaviorGroup => behaviorGroup.Key, behaviorGroup => behaviorGroup.ToImmutableArray());
		var defaultsByTag = all.All
        	.Where(payload => !payload.DefaultsForTagIds.IsDefaultOrEmpty)
        	.SelectMany(payload => payload.DefaultsForTagIds.Select(tag => (Tag: tag, Payload: payload)))
        	.GroupBy(tagPair => tagPair.Tag, tagPair => tagPair.Payload)
        	.ToFrozenDictionary(tagGroup => tagGroup.Key, tagGroup => tagGroup.ToImmutableArray());
    	return new PayloadRegistry(all, byBehaviorType, defaultsByTag, errorConfig);
	}

	public bool Contains(PayloadId id) => _all.Contains(id);
    public PayloadDefinition Get(PayloadId id) => _all.Get(id);
    public T GetData<T>(PayloadId id) where T : class, IPayload => _all.Get(id).PayloadData as T ?? throw new InvalidOperationException($"[{Source}]: {id} is not a {typeof(T).Name}.");
    public ImmutableArray<PayloadDefinition> InGroup(PayloadBehaviorGroup group) => _byBehaviorType.TryGetValue(group, out var list) ? list : ImmutableArray<PayloadDefinition>.Empty;
	public IEnumerable<PayloadDefinition> DefaultsFor(TagSet characterTags)
	{
    	var seenPayloads = new HashSet<PayloadId>();
    	foreach (var tag in characterTags.Tags)
		{
			if (_defaultsByTag.TryGetValue(tag, out var list))
            	foreach (var definition in list)
				{
					if (seenPayloads.Add(definition.Id)) yield return definition;
				}
		}
	}
    public int Count => _all.Count;
}
