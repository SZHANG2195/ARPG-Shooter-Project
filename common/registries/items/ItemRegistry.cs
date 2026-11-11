using System.Collections.Frozen;
using System.Collections.Generic;
using lethal.common.registries.tags;
using lethal.core.config;
using lethal.core.domain.items;
using lethal.core.domain.tags;

namespace lethal.common.registries.items;
public sealed class ItemRegistry
{
    private readonly Registry<ItemId, ItemDefinition> _all;
    private readonly FrozenDictionary<ItemId, TagSet> _tagSets;
    private readonly ErrorConfig _errorConfig;

    private const string Source = nameof(ItemRegistry);

    private ItemRegistry(Registry<ItemId, ItemDefinition> all, FrozenDictionary<ItemId, TagSet> tagSets, ErrorConfig errorConfig)
    {
        _all = all;
        _tagSets = tagSets;
        _errorConfig = errorConfig;
    }


	private static IEnumerable<TagId> DeclaredTags(ItemDefinition definition)
	{
    	yield return definition.CategoryTagId;
    	if (definition.AttributeAffinityTagId is { } affinity) yield return affinity;
    	if (definition.RarityTypeTagId is { } rarity) yield return rarity;
	}

    public static ItemRegistry Build(IEnumerable<ItemDefinition> definitions, TagRegistry tags, ErrorConfig errorConfig)
	{
    	var all = Registry<ItemId, ItemDefinition>.Build(definitions, definition => definition.Id, "DefinitionItems", errorConfig);
    	var tagSets = all.All.ToFrozenDictionary(item => item.Id, item => tags.Expand(DeclaredTags(item)));
    	return new ItemRegistry(all, tagSets, errorConfig);
	}

    public bool Contains(ItemId id) => _all.Contains(id);
    public ItemDefinition Get(ItemId id) => _all.Get(id);
    public TagSet TagsOf(ItemId id) => _tagSets[id];
    public IReadOnlyCollection<ItemDefinition> All => _all.All;
    public int Count => _all.Count;
}