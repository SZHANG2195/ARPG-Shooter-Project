using System.Collections.Immutable;

namespace lethal.core.domain.tags;
public sealed record TagFilter
{
    public static readonly TagFilter Empty = new();

    public ImmutableArray<TagId> All { get; init; } = ImmutableArray<TagId>.Empty;
    public ImmutableArray<TagId> Any { get; init; } = ImmutableArray<TagId>.Empty;

	public bool IsEmpty => All.IsEmpty && Any.IsEmpty;
}
