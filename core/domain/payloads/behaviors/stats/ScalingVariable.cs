using lethal.core.domain.stats;


namespace lethal.core.domain.payloads;
public sealed record ScalingVariable
{
    public required string VariableName { get; init; }
    public double? ConstantValue { get; init; }
    public StatId? StatId { get; init; }
}
