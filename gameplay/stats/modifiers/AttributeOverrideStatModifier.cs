using System.Collections.Generic;
using lethal.gameplay.stats.data;
using lethal.gameplay.stats.enums;

namespace lethal.gameplay.stats.modifiers;
public class AttributeOverrideStatModifier : StatModifier
{
	public Dictionary<StatType, float> SourceAttributeWeights { get; set; } = new();
	public List<AttributeYieldDefinition> TargetYields { get; set; } = new();

	public bool SuppressDefaultYields { get; set; } = true;

    public override float GetValue() => 0.0f;

}
