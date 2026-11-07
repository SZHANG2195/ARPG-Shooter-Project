namespace lethal.gameplay.stats.components;
public sealed partial class StatsComponent
{
	public readonly record struct StatResult(double FinalValue, double FlatTotal, double PostMultiplierValue)
	{
    	public static StatResult Seed(double value) => new(value, value, value);
	}
}