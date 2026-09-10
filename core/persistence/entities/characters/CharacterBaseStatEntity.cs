namespace lethal.core.persistence.entities.characters;
public class CharacterBaseStatEntity
{
    public required string CharacterId { get; set; }
    public required string StatId { get; set; }
    public float Value { get; set; }
}
