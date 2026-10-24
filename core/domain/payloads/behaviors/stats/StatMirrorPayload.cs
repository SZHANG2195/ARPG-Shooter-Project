using System.Collections.Immutable;
using lethal.core.domain.context;
using lethal.core.domain.payloads.enums;
using lethal.core.domain.stats;
using lethal.core.domain.tags;

namespace lethal.core.domain.payloads;
public sealed record StatMirrorPayload : IPayload
{
    public required StatStaticType StatStaticType { get; init; }
    public ContextId? ContextId { get; init; }
    public StatId? TargetStatId { get; init; }
	public StatId? SourceStatId { get; init; }
	public TagFilter TargetTags { get; init; } = TagFilter.Empty;
    public ImmutableArray<TagId> ExcludedTags { get; init; } = ImmutableArray<TagId>.Empty;
    public required double MirroringMinRatio { get; init; }
    public required double MirroringMaxRatio { get; init; }
}