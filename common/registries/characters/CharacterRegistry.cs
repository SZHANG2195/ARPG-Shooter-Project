using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using lethal.core.domain.characters;
using lethal.core.domain.tags;

namespace lethal.common.registries.characters;
public sealed class CharacterRegistry
{
    private readonly Registry<CharacterId, CharacterDefinition> _all;
    private readonly FrozenDictionary<TagId, ImmutableArray<CharacterDefinition>> _byFaction;

    private CharacterRegistry(Registry<CharacterId, CharacterDefinition> all, FrozenDictionary<TagId, ImmutableArray<CharacterDefinition>> byFaction)
    {
        _all = all;
        _byFaction = byFaction;
    }

    public static CharacterRegistry Build(IEnumerable<CharacterDefinition> definitions)
    {
        var all = Registry<CharacterId, CharacterDefinition>.Build(definitions.ToList(), d => d.Id, "DefinitionCharacters");
        var byFaction = all.All
            .SelectMany(c => c.FactionTagIds.Distinct().Select(f => (Faction: f, Character: c)))
            .GroupBy(x => x.Faction, x => x.Character)
            .ToFrozenDictionary(g => g.Key, g => g.ToImmutableArray());
        return new CharacterRegistry(all, byFaction);
    }

    public bool Contains(CharacterId id) => _all.Contains(id);
    public CharacterDefinition Get(CharacterId id) => _all.Get(id);
    public IReadOnlyCollection<CharacterDefinition> All => _all.All;
    public ImmutableArray<CharacterDefinition> InFaction(TagId faction) => _byFaction.TryGetValue(faction, out var list) ? list : ImmutableArray<CharacterDefinition>.Empty;
    public int Count => _all.Count;
}