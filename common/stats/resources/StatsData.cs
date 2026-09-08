using Godot;
using System;
using Godot.Collections;
using lethal.stats.data;

namespace lethal.stats.resources;
public partial class StatsData : Resource
{
	[Export]
	public Dictionary<StatType, float> BaseStats { get; set; } = new();

	[Export]
	public Array<StatType> StartingResources { get; set; } = new();
}
