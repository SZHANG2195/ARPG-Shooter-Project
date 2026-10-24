using System.Collections.Immutable;
using lethal.core.domain.localizations;
using lethal.core.domain.tags;

namespace lethal.core.domain.status_effects;
public sealed record StatusEffectDefinition
{
	public required StatusEffectId Id { get; init; }
	public required LocalizationKey NameLocalizationKey { get; init; }
	public required bool IsVisible { get; init; }
	public required bool IsPermanent { get; init; }
	public required double BaseDuration { get; init; }
	public required int MaxStacks { get; init; }
	public required double TickInterval { get; init; }
	public ImmutableArray<TagId> TagIds { get; init; } = ImmutableArray<TagId>.Empty;
}
