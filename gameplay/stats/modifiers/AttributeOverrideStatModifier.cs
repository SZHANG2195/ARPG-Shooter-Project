using System.Collections.Generic;
using lethal.core.domain.stat_identity;
using lethal.gameplay.stats.data;

namespace lethal.gameplay.stats.modifiers;
public class AttributeOverrideStatModifier : StatModifier
{
	public Dictionary<StatId, float> SourceAttributeWeights { get; set; } = new();
	public List<AttributeYieldDefinition> TargetYields { get; set; } = new();

	public bool SuppressDefaultYields { get; set; } = true;

    public override float GetValue() => 0.0f;

}
