using Godot;
using Godot.Collections;
using lethal.gameplay.stats.enums;

namespace lethal.gameplay.stats.data;
public partial class StatsData : Resource
{
	[Export]
	public Dictionary<StatType, float> BaseStats { get; set; } = new();

	[Export]
	public Array<StatType> StartingResources { get; set; } = new();
}
