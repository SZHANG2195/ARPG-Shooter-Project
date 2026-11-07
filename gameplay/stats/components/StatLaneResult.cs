namespace lethal.gameplay.stats.components;
public sealed partial class StatsComponent
{
	private struct StatLaneResult
	{
    	public double BaseValue;
    	public StatResult Result;
    	public bool IsDirty;
	}
}
