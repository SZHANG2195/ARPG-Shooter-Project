namespace lethal.core.domain.items;
public sealed record ItemProfileKeyDefinition
{
	public required ItemProfileKeyId Id { get; init; }
	public required int GridWidth { get; init; }
	public required int GridHeight { get; init; }
	public required int MaxSocketCount { get; init; }
	public required int BaseAugmentSockets { get; init; }
	public required int BaseChipSockets { get; init; }
	public required int MaxPrefixes { get; init; }
	public required int MaxSuffixes { get; init; }
	public required int MaxQuality { get; init; }
}
