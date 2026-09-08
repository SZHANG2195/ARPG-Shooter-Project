using Godot;
using lethal.stats.data;
using lethal.stats.logic;
using System;
using System.Collections.Generic;

namespace lethal.stats.logic;
public class AttributeOverrideStatModifier : StatModifier
{
	public Dictionary<StatType, float> SourceAttributeWeights { get; set; } = new();
	public List<AttributeYieldDefinition> TargetYields { get; set; } = new();

	public bool SuppressDefaultYields { get; set; } = true;

    public override float GetValue() => 0.0f;

}
