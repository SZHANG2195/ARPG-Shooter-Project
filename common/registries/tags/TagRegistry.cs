using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using lethal.core.domain.tags;

namespace lethal.common.registries.tags;
public sealed class TagRegistry
{
    private readonly Registry<TagId, TagDefinition> _all;
    private readonly FrozenDictionary<TagId, ImmutableArray<TagId>> _ancestors;
    private readonly FrozenDictionary<string, ImmutableArray<TagId>> _byNamespace;

    private TagRegistry(Registry<TagId, TagDefinition> all, FrozenDictionary<TagId, ImmutableArray<TagId>> ancestors, FrozenDictionary<string, ImmutableArray<TagId>> byNamespace)
    {
        _all = all;
        _ancestors = ancestors;
        _byNamespace = byNamespace;
    }

    public static TagRegistry Build(IEnumerable<TagDefinition> definitions)
    {
        var all = Registry<TagId, TagDefinition>.Build(definitions, d => d.Id, "DefinitionTags");
        var ids = definitions.Select(d => d.Id.Value).ToHashSet(StringComparer.Ordinal);

        var ancestors = new Dictionary<TagId, ImmutableArray<TagId>>();
        var namespaces = new Dictionary<string, List<TagId>>(StringComparer.Ordinal); 
        foreach (var def in definitions)
        {
            var found = ImmutableArray.CreateBuilder<TagId>();
            var path = def.Id.Value;
            int dot;
            while ((dot = path.LastIndexOf('.')) > 0)
            {
                path = path[..dot];
                if (ids.Contains(path)) found.Add(new TagId(path));

                if (!namespaces.TryGetValue(path, out var bucket)) namespaces[path] = bucket = new List<TagId>();
                bucket.Add(def.Id); //not every prefix is an actual tag within the database
            }
            ancestors[def.Id] = found.ToImmutable();
        }
        return new TagRegistry(all, ancestors.ToFrozenDictionary(), namespaces.ToFrozenDictionary(kv => kv.Key, kv => kv.Value.ToImmutableArray()));
    }

    public bool Contains(TagId id) => _all.Contains(id);
    public TagSet Expand(IEnumerable<TagId> declared)
    {
        var set = new HashSet<TagId>();
        foreach (var tag in declared)
        {
            set.Add(tag);
            if (_ancestors.TryGetValue(tag, out var parents)) set.UnionWith(parents);
        }
        return TagSet.From(set);
    }
    public ImmutableArray<TagId> Under(TagNamespace ns) => ns.Prefix is { } prefix && _byNamespace.TryGetValue(prefix, out var tags) ? tags : ImmutableArray<TagId>.Empty;
    public bool IsUnder(TagId tag, TagNamespace ns) => ns.Contains(tag);
    public int Count => _all.Count;
}