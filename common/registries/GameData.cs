using lethal.common.registries.affixes;
using lethal.common.registries.characters;
using lethal.common.registries.factions;
using lethal.common.registries.items;
using lethal.common.registries.items.augments;
using lethal.common.registries.items.equipment;
using lethal.common.registries.payloads;
using lethal.common.registries.status_effect;
using lethal.common.registries.tags;

namespace lethal.common.registries;
public sealed class GameData
{
    public required TagRegistry Tags { get; init; }
    public required StatRegistry Stats { get; init; }
    public required StatusEffectRegistry StatusEffects { get; init; }
    public required FactionRegistry Factions { get; init; }
    public required PayloadRegistry Payloads { get; init; }
    public required AffixRegistry Affixes { get; init; }
    public required ItemRegistry Items { get; init; }
    public required ItemProfileKeyRegistry ItemProfiles { get; init; }
    public required EquipmentRegistry Equipment { get; init; }
    public required AugmentRegistry Augments { get; init; }
    public required CharacterRegistry Characters { get; init; }

    public static GameData Build(GameDefinitions d)
    {
        //order matters here do not touch
        var tags = TagRegistry.Build(d.Tags);
        var stats = StatRegistry.Build(d.Stats, tags);
        var statusEffects = StatusEffectRegistry.Build(d.StatusEffects, tags);
        var items = ItemRegistry.Build(d.Items, tags);
        var factions = FactionRegistry.Build(d.Factions);
        var characters = CharacterRegistry.Build(d.Characters);
        var itemProfileKeys = ItemProfileKeyRegistry.Build(d.ItemProfileKeys);
        var equipment = EquipmentRegistry.Build(d.Equipment);
        var augments = AugmentRegistry.Build(d.Augments);
        var affixes = AffixRegistry.Build(d.Affixes);
        var payloads = PayloadRegistry.Build(d.Payloads);


        return new GameData
        {
            Tags = tags,
            Stats = stats,
            StatusEffects = statusEffects,
            Factions = factions,
            Characters = characters,
            ItemProfiles = itemProfileKeys,
            Items = items,
            Equipment = equipment,
            Augments = augments,
            Affixes = affixes,
            Payloads = payloads,
        };
    }

    public string Summary() =>
        $"tags {Tags.Count}, stats {Stats.Count}, statuses {StatusEffects.Count}, factions {Factions.Count}, " +
        $"characters {Characters.Count}, profiles {ItemProfiles.Count}, items {Items.Count}, equipment {Equipment.Count}, " +
        $"augments {Augments.Count}, affixes {Affixes.Count}, payloads {Payloads.Count}";
}