using lethal.gameplay.stats.enums;
using System.Linq;

namespace lethal.gameplay.stats.modifiers;
public class EffectivenessModifier : StatModifier
{
    public ModifierSourceCategory? TargetCategory { get; set; } = null;

    public float Value;
    public override float GetValue() => Value;

    public bool Matches(StatModifier target)
    {
        if (target is EffectivenessModifier) return false;

        if (Slot != EquipmentSlot.None && target.Source.OriginSlot != Slot)
            return false;

        if (TargetCategory.HasValue && target.Source.Category != TargetCategory.Value)
            return false;

        if (AffectedStats.Count > 0 &&
            !target.AffectedStats.Any(stat => AffectedStats.Contains(stat)))
            return false;

        return true;
    }
}
