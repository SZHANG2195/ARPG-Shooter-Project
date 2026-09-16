using Godot;
using lethal.core.domain.character_identity;
using lethal.core.domain.stat_identity;
using lethal.core.persistence;
using lethal.core.persistence.entities.characters;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace lethal.common.registry;

public static class CharacterDefinitionRegistry
{
    private static readonly Dictionary<string, CharacterDefinition> _definitions = new();

    public static void LoadAll(GameDbContext db)
    {
        var entities = db.Set<CharacterDefinitionEntity>()
            .Include(c => c.BaseStats)
            .Include(c => c.StartingResources)
            .ToList();

        foreach (var entity in entities)
        {
            var baseStats = new Dictionary<StatId, float>();
            foreach (var stat in entity.BaseStats)
            {
                baseStats[new StatId(stat.StatId)] = stat.Value;
            }

            var startingResources = new List<StatId>();
            foreach (var resource in entity.StartingResources)
            {
                startingResources.Add(new StatId(resource.StatId));
            }

            _definitions[entity.Id] = new CharacterDefinition
            {
                Id = entity.Id,
                DisplayName = entity.DisplayName,
                BaseStats = baseStats,
                StartingResources = startingResources
            };
        }

        GD.Print($"[CharacterDefinitionRegistry] Loaded {_definitions.Count} character definitions.");
    }

    public static CharacterDefinition? Get(string characterId)
    {
        if (_definitions.TryGetValue(characterId, out var definition))
        {
            return definition;
        }

        string errorMsg = $"[CharacterDefinitionRegistry] Error: Character '{characterId}' was requested but does not exist!";
        #if DEBUG
        throw new KeyNotFoundException(errorMsg);
        #else
        GD.PrintErr(errorMsg);
        return null;
        #endif
    }
}