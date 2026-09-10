using lethal.core.persistence.stat_identity;
using lethal.gameplay.stats.enums;

namespace lethal.gameplay.stats.data;
public class AttributeYieldDefinition
{
    public StatId TargetStat { get; set; }
    public float RatioPerPoint { get; set; }
    public ModifierType Type { get; set; } = ModifierType.Flat;
}
