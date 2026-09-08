using Godot;
using lethal.stats.data;
using System;

namespace lethal.stats.data;
public class AttributeYieldDefinition
{
    public StatType TargetStat { get; set; }
    public float RatioPerPoint { get; set; }
    public ModifierType Type { get; set; } = ModifierType.Flat;
}
