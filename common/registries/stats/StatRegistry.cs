using System.Collections.Frozen;
using System.Collections.Generic;
using lethal.core.config;
using lethal.core.domain.stats;
using lethal.core.domain.tags;

namespace lethal.common.registries.tags;
public sealed class StatRegistry
{
    private readonly Registry<StatId, StatDefinition> _all;
    private readonly FrozenDictionary<StatId, TagSet> _tagSets;
    private readonly ErrorConfig _errorConfig;

    private const string Source = nameof(StatRegistry);

    private StatRegistry(Registry<StatId, StatDefinition> all, FrozenDictionary<StatId, TagSet> tagSets, ErrorConfig errorConfig)
    {
        _all = all;
        _tagSets = tagSets;
        _errorConfig = errorConfig;
    }


    public static StatRegistry Build(IEnumerable<StatDefinition> definitions, TagRegistry tags, ErrorConfig errorConfig)
    {
        var all = Registry<StatId, StatDefinition>.Build(definitions, definition => definition.Id, "DefinitionStats", errorConfig);
    	var tagSets = all.All.ToFrozenDictionary(stat => stat.Id, stat => tags.Expand(stat.TagIds));
        return new StatRegistry(all, tagSets, errorConfig);
    }

    public bool Contains(StatId id) => _all.Contains(id);
    public StatDefinition Get(StatId id) => _all.Get(id);
    public TagSet TagsOf(StatId id) => _tagSets[id];
    public IReadOnlyCollection<StatDefinition> All => _all.All;
    public int Count => _all.Count;
}