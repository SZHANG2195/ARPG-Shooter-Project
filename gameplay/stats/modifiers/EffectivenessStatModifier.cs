using lethal.gameplay.stats.enums;
using System.Linq;

namespace lethal.gameplay.stats.modifiers;
public class EffectivenessModifier : StatModifier
{
    public required EffectivenessApplication EffectivenessApplication { get; set; }
    public ModifierSourceCategory? TargetCategory { get; set; } = null;
    public AffixType? TargetAffixType { get; set; } = null;

    public float Value;
    public override float GetValue() => Value;

    public bool Matches(StatModifier target)
    {
        if (target is EffectivenessModifier || target is AttributeOverrideStatModifier) return false;

        if (Slot != EquipmentSlot.None && target.Source.OriginSlot != Slot) return false;

        if (TargetCategory.HasValue && target.Source.Category != TargetCategory.Value) return false;

        if (TargetAffixType.HasValue && target.AffixType != TargetAffixType.Value) return false;

        if (AffectedStats.Count > 0)
        {
            bool hasOverlap = false;
            foreach (var stat in target.AffectedStats)
            {
                if (AffectedStats.Contains(stat))
                {
                    hasOverlap = true;
                    break;
                }
            }
            if (!hasOverlap) return false;
        }

        return true;
    }
}
