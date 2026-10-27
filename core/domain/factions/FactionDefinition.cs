using System.Text.Json.Serialization;
using lethal.core.domain.localizations;
using lethal.core.domain.tags;

namespace lethal.core.domain.factions;
public sealed record FactionDefinition
{
	[JsonPropertyName("FactionTagId")]
	public required TagId Id { get; init; }
	public required LocalizationKey NameLocalizationKey { get; init; }
	public required LocalizationKey DescriptionLocalizationKey { get; init; }
	public required LocalizationKey FlavourTextLocalizationKey { get; init; }
}
