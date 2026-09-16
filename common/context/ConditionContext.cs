using lethal.common.context.enums;
using lethal.gameplay.stats.components;

namespace lethal.common.context;
public class ConditionContext
{
    public required StatsComponent Source { get; init; }
    public StatsComponent? Target { get; init; }
    public ConditionScope Scope { get; init; }
}
