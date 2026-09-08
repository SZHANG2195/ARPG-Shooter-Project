using Godot;
using lethal.stats.data;
using lethal.stats.logic;
using System;


namespace lethal.stats.resources;
public class ScalingPool
{
	public float Increased { get; set; } = 0.0f;
	public float More { get; set; } = 1.0f;

	public void AddModifier(StatModifier modifier)
	{
		switch (modifier.Type)
		{
			case ModifierType.Increased:
				Increased += modifier.GetValue();
				break;
			case ModifierType.More:
				More *= modifier.GetValue();
				break;
			default:
				GD.PrintErr($"[ScalingPool] Warning: Invalid modifier type {modifier.Type} passed to ScalingPool!");
				break;
		}
	}

	public float GetFinalMultiplier() => (1.0f + Increased) * More;
}
