using Godot;
using lethal.core.domain.character_identity;
using lethal.core.domain.stat_identity;
using lethal.core.persistence;
using lethal.core.persistence.entities.characters;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace lethal.common.registry;

public static class CharacterDefinitionRegistry
{
    private static readonly Dictionary<string, CharacterDefinition> _definitions = new();

    public static void LoadAll(GameDbContext db)
    {
        _definitions.Clear();
        var entities = db.Set<CharacterDefinitionEntity>()
            .Include(c => c.BaseStats)
            .Include(c => c.StartingResources)
            .ToList();
        
        var entitiesById = entities.ToDictionary(e => e.Id);

        foreach (var entity in entities)
        {
            var baseStats = new Dictionary<StatId, float>();
            var startingResources = new List<StatId>();

            if (!string.IsNullOrEmpty(entity.TemplateId))
            {
                if (entitiesById.TryGetValue(entity.TemplateId, out var template))
                {
                    bool isValidTemplate = true;

                    if (!string.IsNullOrEmpty(template.TemplateId))
                    {
                        string errorMsg = $"[CharacterDefinitionRegistry] Chain inheritance error: Character '{entity.Id}' " +
                                            $"references template '{template.Id}', which itself uses template '{template.TemplateId}'. " +
                                            $"Multi-level inheritance is not supported.";
                        #if DEBUG
                        throw new InvalidOperationException(errorMsg);
                        #else
                        GD.PrintErr(errorMsg);
                        isValidTemplate = false;
                        #endif
                    }

                    if (isValidTemplate)
                    {
                        foreach (var stat in template.BaseStats)
                        {
                            baseStats[new StatId(stat.StatId)] = stat.Value;
                        }
                        foreach (var resource in template.StartingResources)
                        {
                            startingResources.Add(new StatId(resource.StatId));
                        }
                    }
                }
                else
                {
                    GD.PrintErr($"[CharacterDefinitionRegistry] Warning: Character '{entity.Id}' references missing TemplateId '{entity.TemplateId}'.");
                }
            }

            foreach (var stat in entity.BaseStats)
            {
                baseStats[new StatId(stat.StatId)] = stat.Value;
            }

            foreach (var resource in entity.StartingResources)
            {
                var statId = new StatId(resource.StatId);
                if (!startingResources.Contains(statId)) startingResources.Add(statId);
            }

            _definitions[entity.Id] = new CharacterDefinition
            {
                Id = entity.Id,
                TemplateId = entity.TemplateId,
                LocalizationKey = entity.LocalizationKey,
                BaseStats = baseStats,
                StartingResources = startingResources,
                PipelineFlags = entity.PipelineFlags
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