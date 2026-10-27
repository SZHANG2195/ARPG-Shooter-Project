using System.Collections.Generic;
using lethal.core.domain.affixes;
using lethal.core.domain.characters;
using lethal.core.domain.factions;
using lethal.core.domain.items;
using lethal.core.domain.items.augments;
using lethal.core.domain.items.equipment;
using lethal.core.domain.payloads;
using lethal.core.domain.stats;
using lethal.core.domain.status_effects;
using lethal.core.domain.tags;

namespace lethal.common.registries;
public sealed class GameDefinitions
{
    public required IReadOnlyList<TagDefinition> Tags { get; init; }
    public required IReadOnlyList<StatDefinition> Stats { get; init; }
    public required IReadOnlyList<StatusEffectDefinition> StatusEffects { get; init; }
    public required IReadOnlyList<FactionDefinition> Factions { get; init; }
	public required IReadOnlyList<CharacterDefinition> Characters { get; init; }
	public required IReadOnlyList<ItemProfileKeyDefinition> ItemProfileKeys { get; init; }
	public required IReadOnlyList<ItemDefinition> Items { get; init; }
	public required IReadOnlyList<EquipmentDefinition> Equipment { get; init; }
	public required IReadOnlyList<AugmentDefinition> Augments { get; init; }
	public required IReadOnlyList<AffixDefinition> Affixes { get; init; }
    public required IReadOnlyList<PayloadDefinition> Payloads { get; init; }
}
