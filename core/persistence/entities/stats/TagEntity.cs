namespace lethal.core.persistence.entities.stats;
public class TagEntity
{
    public required string Name { get; set; }
    public bool IsPlayerVisible { get; set; }
    public required string LocalizationKey { get; set; }
}