using System.Collections.Generic;
using lethal.core.config;
using lethal.core.domain.items;

namespace lethal.common.registries.items;
public sealed class ItemProfileKeyRegistry
{
    private readonly Registry<ItemProfileKeyId, ItemProfileKeyDefinition> _all;
    private readonly ErrorConfig _errorConfig;

    private const string Source = nameof(ItemProfileKeyRegistry);

    private ItemProfileKeyRegistry(Registry<ItemProfileKeyId, ItemProfileKeyDefinition> all, ErrorConfig errorConfig) 
    {
        _all = all;
        _errorConfig = errorConfig;
    }

    public static ItemProfileKeyRegistry Build(IEnumerable<ItemProfileKeyDefinition> defs, ErrorConfig errorConfig) => 
        new(Registry<ItemProfileKeyId, ItemProfileKeyDefinition>.Build(defs, d => d.Id, "DefinitionItemProfileKeys", errorConfig), errorConfig);

    public bool Contains(ItemProfileKeyId id) => _all.Contains(id);
    public ItemProfileKeyDefinition Get(ItemProfileKeyId id) => _all.Get(id);
    public int Count => _all.Count;
}