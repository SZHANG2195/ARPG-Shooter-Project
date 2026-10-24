using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace lethal.core.domain.tags;

public sealed class TagSet
{
    public static readonly TagSet Empty = new(FrozenSet<TagId>.Empty);

    private readonly FrozenSet<TagId> _tags;

    private TagSet(FrozenSet<TagId> tags) => _tags = tags;

    public static TagSet From(IEnumerable<TagId> expandedTags)
    {
        var frozen = expandedTags.ToFrozenSet();
        return frozen.Count == 0 ? Empty : new TagSet(frozen);
    }

    public int Count => _tags.Count;
    public IEnumerable<TagId> Tags => _tags;

    public bool Contains(TagId tag) => _tags.Contains(tag);

    public bool ContainsAll(ImmutableArray<TagId> required)
    {
        if (required.IsDefaultOrEmpty) return true;
        foreach (var tag in required)
            if (!_tags.Contains(tag)) return false;
        return true;
    }

    public bool ContainsAny(ImmutableArray<TagId> candidates)
    {
        if (candidates.IsDefaultOrEmpty) return false;
        foreach (var tag in candidates)
            if (_tags.Contains(tag)) return true;
        return false;
    }

    public bool ContainsNone(ImmutableArray<TagId> excluded) => !ContainsAny(excluded);

    public bool Satisfies(TagFilter filter) =>
        ContainsAll(filter.All) && (filter.Any.IsDefaultOrEmpty || ContainsAny(filter.Any));

    public bool Matches(TagFilter filter, ImmutableArray<TagId> excluded) =>
        Satisfies(filter) && ContainsNone(excluded);
}