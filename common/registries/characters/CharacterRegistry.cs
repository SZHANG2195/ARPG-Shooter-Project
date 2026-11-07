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
        var raw = Registry<CharacterId, CharacterDefinition>.Build(definitions.ToList(), d => d.Id, "DefinitionCharacters", errorConfig);

        var resolved = new Dictionary<CharacterId, CharacterDefinition>();
        var resolving = new HashSet<CharacterId>();

        CharacterDefinition Resolve(CharacterDefinition c)
        {
            if (resolved.TryGetValue(c.Id, out var done)) return done;
            if (c.TemplateId is not { } templateId) return resolved[c.Id] = c;

            if (!resolving.Add(c.Id)) throw new InvalidOperationException($"CharacterRegistry]: Character template cycle involving '{c.Id}'.");
            if (!raw.TryGet(templateId, out var template))
            {
                var message = $"CharacterRegistry]: Character '{c.Id}' uses unknown template '{templateId}'.";
                errorConfig.ReportError(Source, message);
            }

            var parent = Resolve(template);
            resolving.Remove(c.Id);

            return resolved[c.Id] = c with
            {
                BaseStats = parent.BaseStats.SetItems(c.BaseStats),
                StartingResourceIds = c.StartingResourceIds.IsEmpty
                    ? parent.StartingResourceIds
                    : c.StartingResourceIds,
            };
        }

        var all = Registry<CharacterId, CharacterDefinition>.Build(raw.All.Select(Resolve), d => d.Id, "DefinitionCharacters", errorConfig);

        var byFaction = all.All
            .SelectMany(c => c.FactionTagIds.Distinct().Select(f => (Faction: f, Character: c)))
            .GroupBy(x => x.Faction, x => x.Character)
            .ToFrozenDictionary(g => g.Key, g => g.ToImmutableArray());

        return new CharacterRegistry(all, byFaction, errorConfig);
    }

    public bool Contains(CharacterId id) => _all.Contains(id);
    public CharacterDefinition Get(CharacterId id) => _all.Get(id);
    public IReadOnlyCollection<CharacterDefinition> All => _all.All;
    public ImmutableArray<CharacterDefinition> InFaction(TagId faction) => _byFaction.TryGetValue(faction, out var list) ? list : ImmutableArray<CharacterDefinition>.Empty;
    public int Count => _all.Count;
}