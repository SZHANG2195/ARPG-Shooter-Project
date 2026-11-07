using System;
using System.Collections.Immutable;
using lethal.core.domain.context;
using lethal.core.domain.payloads.enums;
using lethal.core.domain.stats;
using lethal.core.domain.tags;

namespace lethal.core.domain.payloads;
[Obsolete("Archived for historical bookkeeping. Do not use for new implementations.", error: false)]
public sealed record StatScaledPayload : IPayload
{
    public required StatStaticType StatStaticType { get; init; }
    public ImmutableArray<PayloadId> DisabledDefaultIds { get; init; } = ImmutableArray<PayloadId>.Empty;
    public ContextId? ContextId { get; init; }
    public StatId? TargetStatId { get; init; }
    public TagFilter TargetTags { get; init; } = TagFilter.Empty;
    public ImmutableArray<TagId> ExcludedTags { get; init; } = ImmutableArray<TagId>.Empty;
    public bool TargetWithoutCounterpart { get; init; } = false;
    public required string PrimaryScalingFormulaMin { get; init; }
    public required string PrimaryScalingFormulaMax { get; init; }
    public string? SecondaryScalingFormulaMin { get; init; }
    public string? SecondaryScalingFormulaMax { get; init; }
    public ImmutableArray<ScalingVariable> ScalingVariables { get; init; } = ImmutableArray<ScalingVariable>.Empty;
}
