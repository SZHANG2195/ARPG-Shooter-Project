using System.Collections.Immutable;
using lethal.core.domain.localizations;
using lethal.core.domain.payloads;
using lethal.core.domain.tags;

namespace lethal.core.domain.affixes;
public sealed record AffixDefinition
{
	public required AffixId Id { get; init; }
	public required LocalizationKey NameLocalizationKey { get; init; }
	public required int Tier { get; init; }
	public required int Weight { get; init; }
	public required int MinLevel { get; init; }
	public required int MaxLevel { get; init; }
	public required int CreditValue { get; init; }
	public required bool IsAlterable { get; init; }
	public required TagId AffixTypeTagId { get; init; }
	public ImmutableArray<TagId> ExclusivityTagIds { get; init; } = ImmutableArray<TagId>.Empty;
	public ImmutableArray<TagId> RequirementTagIds { get; init; } = ImmutableArray<TagId>.Empty;
	public ImmutableArray<PayloadId> PayloadIds { get; init; } = ImmutableArray<PayloadId>.Empty;
	public ImmutableArray<TagId> TagIds { get; init; } = ImmutableArray<TagId>.Empty;
}
