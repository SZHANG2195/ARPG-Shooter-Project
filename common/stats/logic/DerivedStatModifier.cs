using Godot;
using lethal.stats.components;
using lethal.stats.data;
using lethal.stats.logic;
using System;

namespace lethal.stats.logic;
public class DerivedStatModifier : StatModifier
{
	public StatType SourceStat { get; set; }
	public float Ratio { get; set; }
	public ModifierExecutionPhase Phase { get; set; }

    public override float GetValue() => 0.0f;

}
