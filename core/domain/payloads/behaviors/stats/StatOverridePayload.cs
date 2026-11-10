using lethal.core.domain.context;
using lethal.core.domain.payloads.enums;
using lethal.core.domain.stats;

namespace lethal.core.domain.payloads;
public sealed record StatOverridePayload : IPayload, IContextualPayload
{
	public required StatOverrideType StatOverrideType { get; init; }
    public required EffectScope EffectScope { get; init; }
    public ContextId? ContextId { get; init; }
    public StatId? TargetStatId { get; init; }
	public StatId? SourceStatId { get; init; }
    public bool TargetWithoutCounterpart { get; init; } = false;
    public required double PrimaryMinValue { get; init; }
    public required double PrimaryMaxValue { get; init; }
    public double? SecondaryMinValue { get; init; }
    public double? SecondaryMaxValue { get; init; }
}