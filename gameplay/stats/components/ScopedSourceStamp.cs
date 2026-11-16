// ScopedStamp.cs
using lethal.core.domain.payloads.enums;

namespace lethal.gameplay.stats.components;
public sealed partial class StatsComponent
{
    private struct ScopedSourceStamp
    {
        public double Additive;
        public double More;

        public static ScopedSourceStamp Identity => new() { Additive = 0.0, More = 1.0 };

        public void Apply(StatStaticType type, double amount)
        {
            if (type == StatStaticType.PercentAdditive) Additive += amount;
            else More *= 1.0 + amount;
        }
    }
}
