using Godot;
using lethal.stats.data;
using System;

namespace lethal.stats.logic;
public class StaticStatModifier : StatModifier
{
	public float Value { get; set; }

    public override float GetValue() => Value;

}
