using Godot;
using lethal.gameplay.stats.components;
using lethal.gameplay.stats.modifiers;
using lethal.gameplay.stats.enums;
using lethal.core.persistence.generated;
using lethal.common.context.conditions;
using lethal.common.context.enums;
using lethal.core.bootstrap;
using System.Collections.Generic;
using lethal.core.domain.stat_identity;
using lethal.gameplay.stats.data;

public partial class StatsSmokeTest : Node
{
    public override async void _Ready()
    {
		await GameBootstrap.BootstrapReady.Task;
		
        var statsComponent = new StatsComponent();
        AddChild(statsComponent);
        statsComponent.Initialize("strengthplayer");

        // 1. Template inheritance + DefaultValue seeding
        GD.Print($"[Template] MaxHealth: {statsComponent.GetFinalStat(Stats.MaxHealth)}");
		GD.Print($"[Template] MaxFuel: {statsComponent.GetFinalStat(Stats.MaxFuel)}");    // expect 100 (inherited)
        GD.Print($"[Template] Strength: {statsComponent.GetFinalStat(Stats.Strength)}");
		GD.Print($"[Template] Intelligence: {statsComponent.GetFinalStat(Stats.Intelligence)}");     // expect 10 (strengthplayer's override)
        GD.Print($"[Template] PierceCount: {statsComponent.GetFinalStat(Stats.PierceCount)}"); // expect DefaultValue (0), untouched by either row

        // 2. Plain flat modifier — generic bucket
        var flatArmor = StatModifier.CreateSingleStaticModifier(
            ModifierType.Flat, 20f, Stats.Armour, new ModifierSource(ModifierSourceCategory.Passive));
        statsComponent.AddModifier(flatArmor.Source, flatArmor);
        GD.Print($"[Flat] Armour: {statsComponent.GetFinalStat(Stats.Armour)}"); // expect base + 20

        // 3. Slot-scoped gear modifier — slot bucket split
        var gearArmor = StatModifier.CreateSingleStaticModifier(
            ModifierType.Flat, 30f, Stats.Armour,
            new ModifierSource(ModifierSourceCategory.Equipment, "test_chest", EquipmentSlot.BodyArmour),
            slot: EquipmentSlot.BodyArmour, affixType: AffixType.Prefix);
        statsComponent.AddModifier(gearArmor.Source, gearArmor);
        GD.Print($"[Gear] Armour: {statsComponent.GetFinalStat(Stats.Armour)}"); // expect previous + 30

        // 4. Increased/More stacking
        var increasedArmor = StatModifier.CreateSingleStaticModifier(
            ModifierType.Increased, 0.50f, Stats.Armour, new ModifierSource(ModifierSourceCategory.Passive));
        statsComponent.AddModifier(increasedArmor.Source, increasedArmor);
        GD.Print($"[Increased] Armour: {statsComponent.GetFinalStat(Stats.Armour)}"); // expect (base+20+30) * 1.5

        // 5. Derived modifier (post-multiplier, e.g. gain X% of a resolved stat)
        var derivedEvasion = StatModifier.CreateSingleDerivedModifier(
            ModifierType.Flat, 0.30f, Stats.Armour, Stats.Evasion,
            ModifierExecutionPhase.PostMultipliers, new ModifierSource(ModifierSourceCategory.Passive));
        statsComponent.AddModifier(derivedEvasion.Source, derivedEvasion);
        GD.Print($"[Derived] Evasion: {statsComponent.GetFinalStat(Stats.Evasion)}"); // expect base evasion + 30% of final Armour

        // 6. EffectivenessModifier — DirectScale on the flat armor affix
        var effectivenessMod = new EffectivenessModifier
        {
            Slot = EquipmentSlot.BodyArmour,
            EffectivenessApplication = EffectivenessApplication.DirectScale,
            Type = ModifierType.Increased,
            Value = 0.20f,
            Source = new ModifierSource(ModifierSourceCategory.Passive)
        };
        statsComponent.AddModifier(effectivenessMod.Source, effectivenessMod);
        GD.Print($"[Effectiveness] Armour: {statsComponent.GetFinalStat(Stats.Armour)}"); // expect gear subtotal boosted 20% before summing in

        // 7. Persistent conditional modifier — should apply if condition true
        var conditionalMod = StatModifier.CreateSingleStaticModifier(
            ModifierType.Flat, 999f, Stats.Armour, new ModifierSource(ModifierSourceCategory.Passive));
        conditionalMod.Condition = new LeafCondition { ConditionType = "always_false_test" };
        conditionalMod.ConditionScope = ConditionScope.Persistent;
        statsComponent.AddModifier(conditionalMod.Source, conditionalMod);
        GD.Print($"[Conditional-False] Armour: {statsComponent.GetFinalStat(Stats.Armour)}"); // expect UNCHANGED from step 6 — condition should exclude it

		// 8. Persistent conditional modifier — should apply if condition true
		var conditionalTrueMod = StatModifier.CreateSingleStaticModifier(
            ModifierType.Increased, 10f, Stats.Armour, new ModifierSource(ModifierSourceCategory.Passive));
        	conditionalTrueMod.Condition = new LeafCondition { ConditionType = "is_full_life" };
        	conditionalTrueMod.ConditionScope = ConditionScope.Persistent;
        statsComponent.AddModifier(conditionalTrueMod.Source, conditionalTrueMod);
        GD.Print($"[Conditional-IsFullLife] Armour: {statsComponent.GetFinalStat(Stats.Armour)}"); // expect 644 from step 7 — condition should be true

		// 9. More multiplier — multiplicative scaling (e.g., 20% more armor)
        var moreArmor = StatModifier.CreateSingleStaticModifier(
            ModifierType.More, 0.20f, Stats.Armour, new ModifierSource(ModifierSourceCategory.Passive));
        statsComponent.AddModifier(moreArmor.Source, moreArmor);
        GD.Print($"[More] Armour: {statsComponent.GetFinalStat(Stats.Armour)}"); // expect previous subtotal * 1.20

		// 10. Stat Conversion — convert 50% of MaxHealth to MaxBarrier
        var healthToBarrierConversion = StatModifier.CreateSingleConversionModifier(
            ModifierType.Flat, 0.50f, Stats.MaxHealth, Stats.MaxBarrier, 
            new ModifierSource(ModifierSourceCategory.Passive));
        statsComponent.AddModifier(healthToBarrierConversion.Source, healthToBarrierConversion);
        GD.Print($"[Conversion] MaxBarrier: {statsComponent.GetFinalStat(Stats.MaxBarrier)}"); // expect 50% of MaxHealth
		GD.Print($"[Conversion] MaxHealth: {statsComponent.GetFinalStat(Stats.MaxHealth)}"); // expect 75 health

		// 11. Base Override — override base Evasion to 100
		var evasionBaseOverride = StatModifier.CreateSingleStaticModifier(
            ModifierType.BaseOverride, 100f, Stats.Evasion, new ModifierSource(ModifierSourceCategory.Passive));
        statsComponent.AddModifier(evasionBaseOverride.Source, evasionBaseOverride);
        GD.Print($"[BaseOverride] Evasion: {statsComponent.GetFinalStat(Stats.Evasion)}"); // expect 30% of armor + 100

		// 11. Final Override — override MaxFuel to 100
		var maxFuelFinalOverride = StatModifier.CreateSingleStaticModifier(
            ModifierType.FinalOverride, 250f, Stats.MaxFuel, new ModifierSource(ModifierSourceCategory.Passive));
        statsComponent.AddModifier(maxFuelFinalOverride.Source, maxFuelFinalOverride);
        GD.Print($"[FinalOverride] MaxFuel: {statsComponent.GetFinalStat(Stats.MaxFuel)}"); // expect 250

		// 12. Threshold resolution
        GD.Print($"[Threshold] StunThreshold: {statsComponent.GetFinalStat(Stats.StunThreshold)}"); // expect 50% of final MaxHealth

		// 13. Threshold Override — override StunThreshold to scale off MaxBarrier instead of MaxHealth
        var thresholdOverride = StatModifier.CreateThresholdOverrideDerivedStatModifier(
            ModifierType.Flat, 1.0f, 
            new Dictionary<StatId, float> { { Stats.MaxBarrier, 2.0f } }, 
            ThresholdType.Stun, 
            new ModifierSource(ModifierSourceCategory.Passive), 
            suppressDefault: true);
        statsComponent.AddModifier(thresholdOverride.Source, thresholdOverride);
        GD.Print($"[ThresholdOverride] StunThreshold: {statsComponent.GetFinalStat(Stats.StunThreshold)}"); // expect 200% of MaxBarrier (75)

        // 14. Attribute Override — override or scale attribute weights/yields
        var attributeOverride = StatModifier.CreateAttributeOverrideStatModifier(
            ModifierType.Flat, 
            new Dictionary<StatId, float> { { Stats.Strength, 1.0f } }, // Source attribute & weight
            targetYields: new List<AttributeYieldDefinition>
            {
                new AttributeYieldDefinition 
                { 
                    TargetStat = Stats.MaxBarrier, 
                    RatioPerPoint = 2.0f, 
                    Type = ModifierType.Flat 
                }
            },
            suppressDefaultYields: true,
            source: new ModifierSource(ModifierSourceCategory.Passive));
            
        statsComponent.AddModifier(attributeOverride.Source, attributeOverride);
        GD.Print($"[AttributeOverride] Strength-to-Barrier Check Complete");
		GD.Print($"[AttributeOverride] MaxBarrier: {statsComponent.GetFinalStat(Stats.MaxBarrier)}"); // expect 50 + 20 barrier
    }
}