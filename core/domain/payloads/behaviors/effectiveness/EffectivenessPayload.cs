using System.Collections.Immutable;
using lethal.core.domain.payloads.enums;
using lethal.core.domain.tags;

namespace lethal.core.domain.payloads;
public sealed record EffectivenessPayload : IPayload
{
	public required EffectScope EffectScope { get; init; }
	public required PayloadTargetDomain TargetDomain { get; init; }
    public TagFilter RequirementTags { get; init; } = TagFilter.Empty;
	public ImmutableArray<TagId> ExcludedTags { get; init; } = ImmutableArray<TagId>.Empty;
    public required double EffectivenessMinValue { get; init; }
    public required double EffectivenessMaxValue { get; init; }
}