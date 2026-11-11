using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using lethal.core.config;
using lethal.core.domain.characters;
using lethal.core.domain.tags;

namespace lethal.common.registries.characters;
public sealed class CharacterRegistry
{
    private readonly Registry<CharacterId, CharacterDefinition> _all;
    private readonly FrozenDictionary<TagId, ImmutableArray<CharacterDefinition>> _byFaction;
    private readonly ErrorConfig _errorConfig;

    private const string Source = nameof(CharacterRegistry);

    private CharacterRegistry(Registry<CharacterId, CharacterDefinition> all, FrozenDictionary<TagId, ImmutableArray<CharacterDefinition>> byFaction, ErrorConfig errorConfig)
    {
        _all = all;
        _byFaction = byFaction;
        _errorConfig = errorConfig; 
    }

    public static CharacterRegistry Build(IEnumerable<CharacterDefinition> definitions, ErrorConfig errorConfig)
    {
        var raw = Registry<CharacterId, CharacterDefinition>.Build(definitions.ToList(), definition => definition.Id, "DefinitionCharacters", errorConfig);

        var resolvedCharacters = new Dictionary<CharacterId, CharacterDefinition>();
        var charactersCurrentlyResolving = new HashSet<CharacterId>();

        CharacterDefinition Resolve(CharacterDefinition character)
        {
            if (resolvedCharacters.TryGetValue(character.Id, out var done)) return done;
            if (character.TemplateId is not { } templateId) return resolvedCharacters[character.Id] = character;

            if (!charactersCurrentlyResolving.Add(character.Id)) throw new InvalidOperationException($"[{Source}]: Character template cycle involving '{character.Id}'.");
            if (!raw.TryGet(templateId, out var template)) throw new InvalidOperationException($"[{Source}]: Character '{character.Id}' uses unknown template '{templateId}'.");

            var parent = Resolve(template);
            charactersCurrentlyResolving.Remove(character.Id);

            return resolvedCharacters[character.Id] = character with
            {
                BaseStats = parent.BaseStats.SetItems(character.BaseStats),
                StartingResourceIds = character.StartingResourceIds.IsEmpty
                    ? parent.StartingResourceIds
                    : character.StartingResourceIds,
            };
        }

        var all = Registry<CharacterId, CharacterDefinition>.Build(raw.All.Select(Resolve), definition => definition.Id, "DefinitionCharacters", errorConfig);

        var byFaction = all.All
            .SelectMany(character => character.FactionTagIds.Distinct().Select(f => (Faction: f, Character: character)))
            .GroupBy(factionPair => factionPair.Faction, x => x.Character)
            .ToFrozenDictionary(factionGroup => factionGroup.Key, factionGroup => factionGroup.ToImmutableArray());

        return new CharacterRegistry(all, byFaction, errorConfig);
    }

    public bool Contains(CharacterId id) => _all.Contains(id);
    public CharacterDefinition Get(CharacterId id) => _all.Get(id);
    public IReadOnlyCollection<CharacterDefinition> All => _all.All;
    public ImmutableArray<CharacterDefinition> InFaction(TagId faction) => _byFaction.TryGetValue(faction, out var list) ? list : ImmutableArray<CharacterDefinition>.Empty;
    public int Count => _all.Count;
}