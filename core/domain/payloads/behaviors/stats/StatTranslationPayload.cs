using System.Collections.Immutable;
using lethal.core.domain.context;
using lethal.core.domain.payloads.enums;
using lethal.core.domain.stats;

namespace lethal.core.domain.payloads;
public sealed record StatTranslationPayload : IPayload
{
	public required StatTranslationType StatTranslationType { get; init; }
    public required PayloadResolveOn ResolveOn { get; init; }
	public ImmutableArray<PayloadId> DisabledDefaultIds { get; init; } = ImmutableArray<PayloadId>.Empty;
    public ContextId? ContextId { get; init; }
    public StatId? TargetStatId { get; init; }
	public StatId? SourceStatId { get; init; }
    public required double TranslationMinRatio { get; init; }
    public required double TranslationMaxRatio { get; init; }
}