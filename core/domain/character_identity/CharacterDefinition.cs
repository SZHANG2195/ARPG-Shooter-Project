using System.Collections.Generic;
using lethal.core.domain.stat_identity;
using lethal.gameplay.stats.enums;

namespace lethal.core.domain.character_identity;
public class CharacterDefinition
{
    public required string Id { get; init; }
    public string? DisplayName { get; init; }
    public Dictionary<StatId, float> BaseStats { get; init; } = new();
    public List<StatId> StartingResources { get; init; } = new();
    public StatPipelineFlags PipelineFlags { get; init; }
}
