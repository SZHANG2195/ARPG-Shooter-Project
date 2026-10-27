using System.Collections.Generic;
using lethal.core.domain.items;
using lethal.core.domain.items.equipment;

namespace lethal.common.registries.items.equipment;
public sealed class EquipmentRegistry
{
    private readonly Registry<ItemId, EquipmentDefinition> _all;
    private EquipmentRegistry(Registry<ItemId, EquipmentDefinition> all) => _all = all;

    public static EquipmentRegistry Build(IEnumerable<EquipmentDefinition> defs) => new(Registry<ItemId, EquipmentDefinition>.Build(defs, d => d.Id, "DefinitionEquipment"));

    public bool Contains(ItemId id) => _all.Contains(id);
    public bool TryGet(ItemId id, out EquipmentDefinition def) => _all.TryGet(id, out def);
    public EquipmentDefinition Get(ItemId id) => _all.Get(id);
    public IReadOnlyList<EquipmentDefinition> All => _all.All;
    public int Count => _all.Count;
}