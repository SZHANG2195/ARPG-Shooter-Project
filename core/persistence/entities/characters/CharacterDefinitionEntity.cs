using System.Collections.Generic;
using lethal.gameplay.stats.enums;

namespace lethal.core.persistence.entities.characters;
public class CharacterDefinitionEntity
{
    public required string Id { get; set; } 
    public required string TemplateId { get; set; } 
    public required string LocalizationKey { get; set; }
    public List<CharacterBaseStatEntity> BaseStats { get; set; } = new();
    public List<CharacterStartingResourceEntity> StartingResources { get; set; } = new();
    public StatPipelineFlags PipelineFlags { get; set; } = StatPipelineFlags.DefaultPlayer;
}