using lethal.core.domain.stat_identity;
using lethal.gameplay.stats.enums;
using System.Collections.Generic;

namespace lethal.gameplay.stats.modifiers;
public class ThresholdOverrideDerivedStatModifier : StatModifier
{
	public Dictionary<StatId, float> SourceStatRatios { get; set; } = new();
    public ThresholdType TargetThreshold { get; set; }
    public bool SuppressDefault { get; set; } = true;

	public override float GetValue() => 0.0f;
}
