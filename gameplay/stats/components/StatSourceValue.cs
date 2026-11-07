using lethal.gameplay.payloads.sources;

namespace lethal.gameplay.stats.components;
public sealed partial class StatsComponent
{
	private readonly record struct StatSourceValue(PayloadSourceHandle SourceHandle, double Value, bool IsBase = false, Lane Lane = Lane.Default);
}

