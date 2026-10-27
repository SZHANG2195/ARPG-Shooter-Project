using System.Linq;
using Godot;
using lethal.common.registries;
using lethal.common.registries.tags;
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

        // stat and item tag sets contain everything they declare
        Check(d.Stats.All.All(s => s.TagIds.All(t => d.Stats.TagsOf(s.Id).Contains(t))),
            "stat tag sets contain their declared tags");
        Check(d.Items.All.All(i => d.Items.TagsOf(i.Id).Contains(i.CategoryTagId)),
            "item tag sets contain their category tag");

        // the affix type buckets partition the table
        Check(d.Affixes.All.Select(a => a.AffixTypeTagId).Distinct().Sum(t => d.Affixes.OfType(t).Length) == d.Affixes.Count,
            "affix buckets add up to the affix count");

        // the faction index lists every character under each faction it declares
        Check(d.Characters.All.All(c => c.FactionTagIds.All(f => d.Characters.InFaction(f).Contains(c))),
            "every character appears under its factions");

        // code-owned tags and namespaces still exist in the data
        Check(d.Tags.Contains(StatusTags.Affliction) && d.Tags.Contains(StatusTags.Debuff), "status tags exist");
        Check(!d.Tags.Under(TagNamespaces.AffixKeyword).IsEmpty, "affix keyword namespace has tags");
    }

    private static void Check(bool ok, string what)
    {
        if (ok) GD.Print($"[SmokeTest] ok: {what}");
        else GD.PushError($"[SmokeTest] FAILED: {what}");
    }
}