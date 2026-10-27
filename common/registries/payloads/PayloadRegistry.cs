using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using lethal.core.domain.payloads;
using lethal.core.domain.payloads.enums;

namespace lethal.common.registries.payloads;
public sealed class PayloadRegistry
{
	private readonly Registry<PayloadId, PayloadDefinition> _all;
	private readonly FrozenDictionary<PayloadBehaviorGroup, ImmutableArray<PayloadDefinition>> _byBehaviorType;

	private PayloadRegistry(Registry<PayloadId, PayloadDefinition> all, FrozenDictionary<PayloadBehaviorGroup, ImmutableArray<PayloadDefinition>> byBehaviorType)
    {
        _all = all;
        _byBehaviorType = byBehaviorType;
    }

	public static PayloadRegistry Build(IEnumerable<PayloadDefinition> definitions)
	{
    	var all = Registry<PayloadId, PayloadDefinition>.Build(definitions, d => d.Id, "DefinitionPayloads");
    	var byBehaviorType = all.All
			.GroupBy(p => p.PayloadBehaviorGroup)
			.ToFrozenDictionary(g => g.Key, g => g.ToImmutableArray());
    	return new PayloadRegistry(all, byBehaviorType);
	}

	public bool Contains(PayloadId id) => _all.Contains(id);
    public PayloadDefinition Get(PayloadId id) => _all.Get(id);
    public T GetData<T>(PayloadId id) where T : class, IPayload => _all.Get(id).PayloadData as T ?? throw new InvalidOperationException($"{id} is not a {typeof(T).Name}.");
    public ImmutableArray<PayloadDefinition> InGroup(PayloadBehaviorGroup group) => _byBehaviorType.TryGetValue(group, out var list) ? list : ImmutableArray<PayloadDefinition>.Empty;
    public int Count => _all.Count;
}
