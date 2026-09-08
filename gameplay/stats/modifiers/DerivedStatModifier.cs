using lethal.gameplay.stats.enums;

namespace lethal.gameplay.stats.modifiers;
public class DerivedStatModifier : StatModifier
{
	public StatType SourceStat { get; set; }
	public float Ratio { get; set; }
	public ModifierExecutionPhase Phase { get; set; }

    public override float GetValue() => 0.0f;

}
