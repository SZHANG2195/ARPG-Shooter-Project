using lethal.core.domain.stats;
using lethal.core.domain.tags;
using lethal.gameplay.payloads.instances;
using System.Collections.Generic;

namespace lethal.gameplay.stats.components;
public sealed partial class StatsComponent
{
	private sealed class StatSourceEntry
	{
    	public readonly List<StatPayloadInstance> Instances = new();
    	public readonly HashSet<(StatId Stat, Lane Lane)> PushedKeys = new();
		public readonly TagSet Tags;

		public StatSourceEntry(TagSet tags) => Tags = tags;
	}
}