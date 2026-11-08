using lethal.core.domain.stats;
using lethal.gameplay.payloads.sources;

namespace lethal.gameplay.stats.components;
public readonly record struct StatSourceValue(PayloadSourceHandle SourceHandle, StatId Stat, double Value, bool IsBase = false, Lane Lane = Lane.Default);

