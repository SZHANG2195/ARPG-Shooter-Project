using System.Collections.Frozen;
using System.Collections.Generic;
using lethal.common.registries.tags;
using lethal.core.config;
using lethal.core.domain.status_effects;
using lethal.core.domain.tags;

namespace lethal.common.registries.status_effect;
public sealed class StatusEffectRegistry
{
    private readonly Registry<StatusEffectId, StatusEffectDefinition> _all;
    private readonly FrozenDictionary<StatusEffectId, TagSet> _tagSets;
    private readonly ErrorConfig _errorConfig;

    private const string Source = nameof(StatusEffectRegistry);

    private StatusEffectRegistry(Registry<StatusEffectId, StatusEffectDefinition> all, FrozenDictionary<StatusEffectId, TagSet> tagSets, ErrorConfig errorConfig)
    {
        _all = all;
        _tagSets = tagSets;
        _errorConfig = errorConfig;
    }


    public static StatusEffectRegistry Build(IEnumerable<StatusEffectDefinition> definitions, TagRegistry tags, ErrorConfig errorConfig)
    {
        var all = Registry<StatusEffectId, StatusEffectDefinition>.Build(definitions, d => d.Id, "DefinitionStatusEffects", errorConfig);
    	var tagSets = all.All.ToFrozenDictionary(d => d.Id, d => tags.Expand(d.TagIds));
        return new StatusEffectRegistry(all, tagSets, errorConfig);
    }

    public bool Contains(StatusEffectId id) => _all.Contains(id);
    public StatusEffectDefinition Get(StatusEffectId id) => _all.Get(id);
    public TagSet TagsOf(StatusEffectId id) => _tagSets[id];
	public bool IsAffliction(StatusEffectId id) => TagsOf(id).Contains(StatusTags.Affliction);
    public IReadOnlyCollection<StatusEffectDefinition> All => _all.All;
    public int Count => _all.Count;
}