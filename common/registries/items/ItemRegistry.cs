using System.Collections.Frozen;
using System.Collections.Generic;
using lethal.common.registries.tags;
using lethal.core.domain.items;
using lethal.core.domain.tags;

namespace lethal.common.registries.items;
public sealed class ItemRegistry
{
    private readonly Registry<ItemId, ItemDefinition> _all;
    private readonly FrozenDictionary<ItemId, TagSet> _tagSets;

    private ItemRegistry(Registry<ItemId, ItemDefinition> all, FrozenDictionary<ItemId, TagSet> tagSets)
    {
        _all = all;
        _tagSets = tagSets;
    }


	private static IEnumerable<TagId> DeclaredTags(ItemDefinition d)
	{
    	yield return d.CategoryTagId;
    	if (d.AttributeAffinityTagId is { } affinity) yield return affinity;
    	if (d.RarityTypeTagId is { } rarity) yield return rarity;
	}

    public static ItemRegistry Build(IEnumerable<ItemDefinition> definitions, TagRegistry tags)
	{
    	var all = Registry<ItemId, ItemDefinition>.Build(definitions, d => d.Id, "DefinitionItems");
    	var tagSets = all.All.ToFrozenDictionary(d => d.Id, d => tags.Expand(DeclaredTags(d)));
    	return new ItemRegistry(all, tagSets);
	}

    public bool Contains(ItemId id) => _all.Contains(id);
    public ItemDefinition Get(ItemId id) => _all.Get(id);
    public TagSet TagsOf(ItemId id) => _tagSets[id];
    public IReadOnlyCollection<ItemDefinition> All => _all.All;
    public int Count => _all.Count;
}