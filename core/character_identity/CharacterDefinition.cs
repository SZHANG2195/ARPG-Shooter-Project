using lethal.core.persistence.stat_identity;
using System.Collections.Generic;

public class CharacterDefinition
{
    public required string Id { get; init; }
    public string? DisplayName { get; init; }
    public Dictionary<StatId, float> BaseStats { get; init; } = new();
    public List<StatId> StartingResources { get; init; } = new();
}
