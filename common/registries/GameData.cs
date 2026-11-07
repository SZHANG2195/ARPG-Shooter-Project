using lethal.common.registries.affixes;
using lethal.common.registries.characters;
using lethal.common.registries.factions;
using lethal.common.registries.items;
using lethal.common.registries.items.augments;
using lethal.common.registries.items.equipment;
using lethal.common.registries.payloads;
using lethal.common.registries.status_effect;
using lethal.common.registries.tags;
using lethal.core.config;

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

    public static GameData Build(GameDefinitions d, ErrorConfig errorConfig)
    {
        //order matters here do not touch
        var tags = TagRegistry.Build(d.Tags, errorConfig);
        var stats = StatRegistry.Build(d.Stats, tags, errorConfig);
        var statusEffects = StatusEffectRegistry.Build(d.StatusEffects, tags, errorConfig);
        var items = ItemRegistry.Build(d.Items, tags, errorConfig);
        var factions = FactionRegistry.Build(d.Factions, errorConfig);
        var characters = CharacterRegistry.Build(d.Characters, errorConfig);
        var itemProfileKeys = ItemProfileKeyRegistry.Build(d.ItemProfileKeys, errorConfig);
        var equipment = EquipmentRegistry.Build(d.Equipment, errorConfig);
        var augments = AugmentRegistry.Build(d.Augments, errorConfig);
        var affixes = AffixRegistry.Build(d.Affixes, errorConfig);
        var payloads = PayloadRegistry.Build(d.Payloads, errorConfig);


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