using lethal.core.domain.stats;
using lethal.gameplay.stats.components.stat_dependencies.enums;
using System.Collections.Immutable;

namespace lethal.gameplay.stats.components.stat_dependencies;
public sealed record PayloadFootprint(ImmutableArray<StatId> PrimaryWrites, ImmutableArray<(StatId Stat, EdgeKind Kind)> Reads)
{
    public ImmutableArray<StatId> SecondaryWrites { get; init; } = ImmutableArray<StatId>.Empty;
    public ImmutableArray<StatId> Drains { get; init; } = ImmutableArray<StatId>.Empty;
    public bool SetsDefault { get; init; } 
}
