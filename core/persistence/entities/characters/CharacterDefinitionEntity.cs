using System.Collections.Generic;

namespace lethal.core.persistence.entities.characters;
public class CharacterDefinitionEntity
{
    public required string Id { get; set; } 
    public string? DisplayName { get; set; }
    public List<CharacterBaseStatEntity> BaseStats { get; set; } = new();
    public List<CharacterStartingResourceEntity> StartingResources { get; set; } = new();
}