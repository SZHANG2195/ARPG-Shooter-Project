using lethal.common.registries;
using lethal.core.config;
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
using Microsoft.Data.Sqlite;

namespace lethal.core.tools;

public static class GameDataLoader
{
    public static GameData Load(string connectionString)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        var options = DefinitionJsonConfig.Options;

        var defs = new GameDefinitions
        {
            Tags          		= DefinitionReader.ReadTable<TagDefinition>(connection, "DefinitionTags", options),
            Stats         		= DefinitionReader.ReadTable<StatDefinition>(connection, "DefinitionStats", options),
            StatusEffects 		= DefinitionReader.ReadTable<StatusEffectDefinition>(connection, "DefinitionStatusEffects", options),
            Factions      		= DefinitionReader.ReadTable<FactionDefinition>(connection, "DefinitionFactions", options),
            Characters    		= DefinitionReader.ReadTable<CharacterDefinition>(connection, "DefinitionCharacters", options),
            ItemProfileKeys  	= DefinitionReader.ReadTable<ItemProfileKeyDefinition>(connection, "DefinitionItemProfileKeys", options),
            Items         		= DefinitionReader.ReadTable<ItemDefinition>(connection, "DefinitionItems", options),
            Equipment     		= DefinitionReader.ReadTable<EquipmentDefinition>(connection, "DefinitionEquipment", options),
            Augments      		= DefinitionReader.ReadTable<AugmentDefinition>(connection, "DefinitionAugments", options),
            Affixes       		= DefinitionReader.ReadTable<AffixDefinition>(connection, "DefinitionAffixes", options),
            Payloads      		= DefinitionReader.ReadTable<PayloadDefinition>(connection, "DefinitionPayloads", options),
        };

        return GameData.Build(defs);
    }
}