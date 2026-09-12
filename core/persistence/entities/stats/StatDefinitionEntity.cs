using System.Collections.Generic;

namespace lethal.core.persistence.entities.stats;
public class StatDefinitionEntity
{
    public required string Id { get; set; }
    public required string CodeName { get; set; }
    public required string LocalizationKey { get; set; }
    public bool IsRangePaired { get; set; }
    public string? PairedCounterpartId { get; set; }
    public List<StatTagEntity> Tags { get; set; } = new();
}
