using System.Collections.Generic;
using lethal.core.domain.items;

namespace lethal.common.registries.items;
public sealed class ItemProfileKeyRegistry
{
    private readonly Registry<ItemProfileKeyId, ItemProfileKeyDefinition> _all;
    private ItemProfileKeyRegistry(Registry<ItemProfileKeyId, ItemProfileKeyDefinition> all) => _all = all;

    public static ItemProfileKeyRegistry Build(IEnumerable<ItemProfileKeyDefinition> defs) => new(Registry<ItemProfileKeyId, ItemProfileKeyDefinition>.Build(defs, d => d.Id, "DefinitionItemProfileKeys"));

    public bool Contains(ItemProfileKeyId id) => _all.Contains(id);
    public ItemProfileKeyDefinition Get(ItemProfileKeyId id) => _all.Get(id);
    public int Count => _all.Count;
}