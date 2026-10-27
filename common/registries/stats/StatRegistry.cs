using System.Collections.Frozen;
using System.Collections.Generic;
using lethal.core.domain.stats;
using lethal.core.domain.tags;

namespace lethal.common.registries.tags;
public sealed class StatRegistry
{
    private readonly Registry<StatId, StatDefinition> _all;
    private readonly FrozenDictionary<StatId, TagSet> _tagSets;

    private StatRegistry(Registry<StatId, StatDefinition> all, FrozenDictionary<StatId, TagSet> tagSets)
    {
        _all = all;
        _tagSets = tagSets;
    }


    public static StatRegistry Build(IEnumerable<StatDefinition> definitions, TagRegistry tags)
    {
        var all = Registry<StatId, StatDefinition>.Build(definitions, d => d.Id, "DefinitionStats");
    	var tagSets = all.All.ToFrozenDictionary(d => d.Id, d => tags.Expand(d.TagIds));
        return new StatRegistry(all, tagSets);
    }

    public bool Contains(StatId id) => _all.Contains(id);
    public StatDefinition Get(StatId id) => _all.Get(id);
    public TagSet TagsOf(StatId id) => _tagSets[id];
    public IReadOnlyCollection<StatDefinition> All => _all.All;
    public int Count => _all.Count;
}