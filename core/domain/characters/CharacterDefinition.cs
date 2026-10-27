using System.Collections.Immutable;
using lethal.core.domain.characters.enums;
using lethal.core.domain.localizations;
using lethal.core.domain.stats;
using lethal.core.domain.tags;

namespace lethal.core.domain.characters;
public sealed record CharacterDefinition
{
	public required CharacterId Id { get; init; }
	public required LocalizationKey NameLocalizationKey { get; init; }
	public required LocalizationKey DescriptionLocalizationKey { get; init; }
	public CharacterId? TemplateId { get; init; }
	public required TagId CharacterTypeTagId { get; init; }
	public required TagId CharacterCategoryTagId { get; init; }
	public required StatPipelineFlags StatPipelineFlags { get; init; }
	public ImmutableArray<TagId> FactionTagIds { get; init; } = ImmutableArray<TagId>.Empty;
	public ImmutableDictionary<StatId, double> BaseStats { get; init; } = ImmutableDictionary<StatId, double>.Empty;
	public ImmutableArray<StatId> StartingResourceIds { get; init; } = ImmutableArray<StatId>.Empty;
}
