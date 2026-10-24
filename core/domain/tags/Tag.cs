using lethal.core.domain.localizations;

namespace lethal.core.domain.tags;
public sealed record TagDefinition
{
	public required TagId Id { get; init; }
	public required LocalizationKey NameLocalizationKey { get; init; }
	public required bool IsVisible { get; init; }
}
