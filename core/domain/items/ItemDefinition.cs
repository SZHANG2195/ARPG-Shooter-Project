using lethal.core.domain.localizations;
using lethal.core.domain.tags;

namespace lethal.core.domain.items;
public sealed record ItemDefinition
{
	public required ItemId Id { get; init; }
	public required LocalizationKey NameLocalizationKey { get; init; }
	public required LocalizationKey FlavourTextLocalizationKey { get; init; }
	public required ItemProfileKeyId ItemProfileKeyId { get; init; }
	public required TagId CategoryTagId { get; init; }
	public TagId? AttributeAffinityTagId { get; init; }
	public TagId? RarityTypeTagId { get; init; }
	public required int CreditValue { get; init; }
	public required int MinItemLevel { get; init; }
	public required int RequiredStrength { get; init; }
	public required int RequiredAgility { get; init; }
	public required int RequiredIntelligence { get; init; }
}
