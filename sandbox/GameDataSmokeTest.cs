using System.Linq;
using Godot;
using lethal.common.registries;
using lethal.core.domain.payloads;
using lethal.core.domain.payloads.enums;
using lethal.core.domain.tags;

public static class GameDataSmokeTest
{
    public static void Run(GameData d)
    {
        // Tag expansion: real ancestors included, namespace-only prefixes excluded
        var head = new TagId("item.category.equipment.armour.head");
        var set = d.Tags.Expand(new[] { head });
        Check(set.Contains(new TagId("item.category.equipment.armour")), "head expands to armour");
        Check(set.Contains(new TagId("item.category.equipment")), "head expands to equipment");
        Check(!set.Contains(new TagId("item.category")), "namespace-only prefix not in set");

        // The converter chose the right record and read the nested ranges
        var p = d.Payloads.GetData<StatStaticPayload>(new PayloadId("added_physical_damage_agility_01"));
        Check(p.PrimaryMinValue == 2 && p.SecondaryMaxValue == 4, "static payload ranges read correctly");

        // Group index covers everything
        Check(d.Payloads.InGroup(PayloadBehaviorGroup.Stat).Length
            + d.Payloads.InGroup(PayloadBehaviorGroup.Effectiveness).Length == 63, "every payload is in a group");

        // Status tags (7 of your 11 effects carry status.affliction)
        Check(d.StatusEffects.All.Count(s => d.StatusEffects.IsAffliction(s.Id)) == 7, "affliction count");
    }

    private static void Check(bool ok, string what)
    {
        if (ok) GD.Print($"[SmokeTest] ok: {what}");
        else GD.PushError($"[SmokeTest] FAILED: {what}");
    }
}