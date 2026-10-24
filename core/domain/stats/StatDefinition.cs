using System.Collections.Immutable;
using lethal.core.domain.localizations;
using lethal.core.domain.tags;

namespace lethal.core.domain.stats;
public sealed record StatDefinition
{
	public required StatId Id { get; init; }
	public required LocalizationKey NameLocalizationKey { get; init; }
	public required bool IsRangePaired { get; init; }
	public required bool IsResourcePoolValid { get; init; }
	public StatId? PairedCounterpartId { get; init; }
	public required float DefaultValue { get; init; }
	public ImmutableArray<TagId> TagIds { get; init; } = ImmutableArray<TagId>.Empty;

}
