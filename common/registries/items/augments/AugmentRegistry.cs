using System.Collections.Generic;
using lethal.core.config;
using lethal.core.domain.items;
using lethal.core.domain.items.augments;

namespace lethal.common.registries.items.augments;
public sealed class AugmentRegistry
{
    private readonly Registry<ItemId, AugmentDefinition> _all;
    private readonly ErrorConfig _errorConfig;

    private const string Source = nameof(AugmentRegistry);

    private AugmentRegistry(Registry<ItemId, AugmentDefinition> all, ErrorConfig errorConfig) 
    {
        _all = all;
        _errorConfig = errorConfig;
    }

    public static AugmentRegistry Build(IEnumerable<AugmentDefinition> defs, ErrorConfig errorConfig) => 
        new(Registry<ItemId, AugmentDefinition>.Build(defs, d => d.Id, "DefinitionAugments", errorConfig), errorConfig);

    public bool Contains(ItemId id) => _all.Contains(id);
    public bool TryGet(ItemId id, out AugmentDefinition def) => _all.TryGet(id, out def);
    public AugmentDefinition Get(ItemId id) => _all.Get(id);
    public IReadOnlyList<AugmentDefinition> All => _all.All;
	public int Count => _all.Count;
}