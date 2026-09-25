using Godot;
using lethal.core.domain.stat_identity;
using lethal.gameplay.stats.components;
using System;

namespace lethal.gameplay.stats.modifiers;
public class ResourcePoolScaledModifier : StatModifier
{
    public StatId ResourcePoolMaxStat { get; set; }
    public float CoefficientPerPercent { get; set; }
    public required StatsComponent Owner { get; set; }

    public override float GetValue()
    {
        float? percentage = Owner.GetResourcePoolPercentage(ResourcePoolMaxStat);
        return (percentage ?? 0f) * CoefficientPerPercent;
    }
}
