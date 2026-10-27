using System.Collections.Generic;
using lethal.core.domain.items;
using lethal.core.domain.items.augments;

namespace lethal.common.registries.items.augments;
public sealed class AugmentRegistry
{
    private readonly Registry<ItemId, AugmentDefinition> _all;
    private AugmentRegistry(Registry<ItemId, AugmentDefinition> all) => _all = all;

    public static AugmentRegistry Build(IEnumerable<AugmentDefinition> defs) => new(Registry<ItemId, AugmentDefinition>.Build(defs, d => d.Id, "DefinitionAugments"));

    public bool Contains(ItemId id) => _all.Contains(id);
    public bool TryGet(ItemId id, out AugmentDefinition def) => _all.TryGet(id, out def);
    public AugmentDefinition Get(ItemId id) => _all.Get(id);
    public IReadOnlyList<AugmentDefinition> All => _all.All;
	public int Count => _all.Count;
}