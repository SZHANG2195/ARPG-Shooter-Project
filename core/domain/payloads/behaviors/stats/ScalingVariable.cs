using System;
using lethal.core.domain.stats;


namespace lethal.core.domain.payloads;
[Obsolete("Archived for historical bookkeeping. Do not use for new implementations.", error: false)]
public sealed record ScalingVariable
{
    public required string VariableName { get; init; }
    public double? ConstantValue { get; init; }
    public StatId? StatId { get; init; }
}
