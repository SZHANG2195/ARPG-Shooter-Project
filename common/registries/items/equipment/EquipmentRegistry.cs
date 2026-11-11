using System.Collections.Generic;
using lethal.core.config;
using lethal.core.domain.items;
using lethal.core.domain.items.equipment;

namespace lethal.common.registries.items.equipment;
public sealed class EquipmentRegistry
{
    private readonly Registry<ItemId, EquipmentDefinition> _all;
    private readonly ErrorConfig _errorConfig;

    private const string Source = nameof(EquipmentRegistry);

    private EquipmentRegistry(Registry<ItemId, EquipmentDefinition> all, ErrorConfig errorConfig) 
    {
        _all = all;
        _errorConfig = errorConfig;
    }

    public static EquipmentRegistry Build(IEnumerable<EquipmentDefinition> definitions, ErrorConfig errorConfig) => 
        new(Registry<ItemId, EquipmentDefinition>.Build(definitions, definition => definition.Id, "DefinitionEquipment", errorConfig), errorConfig);

    public bool Contains(ItemId id) => _all.Contains(id);
    public bool TryGet(ItemId id, out EquipmentDefinition def) => _all.TryGet(id, out def);
    public EquipmentDefinition Get(ItemId id) => _all.Get(id);
    public IReadOnlyList<EquipmentDefinition> All => _all.All;
    public int Count => _all.Count;
}