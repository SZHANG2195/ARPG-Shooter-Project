using Godot;
using lethal.core.domain.characters;
using lethal.core.domain.stats;
using lethal.core.domain.tags;
using lethal.gameplay.payloads.instances;
using lethal.gameplay.payloads.sources;
using lethal.gameplay.stats.components;
using lethal.core.config;
using lethal.common.registries;
using lethal.gameplay.payloads.enums;
using lethal.core.domain.payloads;
using System;
using System.Diagnostics;

public static class StatsComponentSmokeTest
{
    internal static class SmokeIds
    {
        public static readonly CharacterId Player = new("player_strength_01");
        public static readonly StatId MaxHealth = new("max_health"), MovementSpeed = new("movement_speed"),
                                    ArmourRating = new("armour_rating"), StunThreshold = new("stun_threshold");
        public static readonly PayloadId MoveSpeedImplicit = new("implicit_increased_movement_speed_01"),
                                        BootsArmour = new("increased_armour_boots_01"),
                                        StunFromHealth = new("default_stun_threshold_from_health");

        public static void Verify(GameData d)
        {
            Check(d.Characters.Contains(Player), "smoke character exists");
            foreach (var stat in new[] { MaxHealth, MovementSpeed, ArmourRating, StunThreshold }) Check(d.Stats.Contains(stat), $"stat {stat}");
            foreach (var id in new[] { MoveSpeedImplicit, BootsArmour, StunFromHealth }) Check(d.Payloads.Contains(id), $"payload {id}");
        }
    }

    public static void Run(GameData data, ErrorConfig errorConfig)
    {
        GD.Print("[StatsSmokeTest] Starting component architecture verification...");

        // 1. Setup a test character definition and initialize the component
        var characterDef = data.Characters.Get(new CharacterId("player_strength_01"));
        var stats = StatsComponent.Create(data.Characters.Get(SmokeIds.Player), data.Stats, data.Payloads, errorConfig, data.Footprints);
        var handle = new PayloadSourceHandle(SourceKind.Equipment, 1);

        // Verify base stats initialized properly from character or registry defaults
        // (Assuming you have a way to inspect or query the initial state/default lane base value)
        GD.Print("[StatsSmokeTest] StatsComponent successfully created.");

        var noModifierStopWatch = Stopwatch.StartNew();
        Check(stats.GetStatResult(SmokeIds.MaxHealth).FinalValue == 100, "max_health inherited from player_base");
        noModifierStopWatch.Stop();

        double totalMsClean = noModifierStopWatch.Elapsed.TotalMicroseconds; 
        double totalTicksClean = noModifierStopWatch.ElapsedTicks;
        GD.Print($"[Benchmark] Clean Evaluation time: {totalMsClean:F3} microseconds. Total ticks: {totalTicksClean}");

        var dirtyStopWatch = Stopwatch.StartNew();
        double bonus = data.Payloads.GetData<StatStaticPayload>(SmokeIds.MoveSpeedImplicit).PrimaryMaxValue;
        var instance = new StatPayloadInstance(SmokeIds.MoveSpeedImplicit, bonus, null, handle, TagSet.Empty);
        stats.InstallSource(handle, TagSet.Empty, [instance], []);
        Check(Approximately(stats.GetStatResult(SmokeIds.MovementSpeed).FinalValue, 1.0 * (1 + bonus)), "increased movement speed applies");
        dirtyStopWatch.Stop();

        double totalMsDirty = dirtyStopWatch.Elapsed.TotalMicroseconds; 
        double totalTicksDirty = dirtyStopWatch.ElapsedTicks;
        GD.Print($"[Benchmark] Dirty Evaluation time: {totalMsDirty:F3} microseconds. Total ticks: {totalTicksDirty}");

        stats.GetStatResult(SmokeIds.MovementSpeed).PrintDebug("Movement Speed");
        stats.RemoveSource(handle);
        Check(Approximately(stats.GetStatResult(SmokeIds.MovementSpeed).FinalValue, 1.0), "removing the source restores the default");

        double ratio = data.Payloads.GetData<StatTranslationPayload>(SmokeIds.StunFromHealth).TranslationMinRatio;
        var defaultHandle = new PayloadSourceHandle(SourceKind.Character, 2);
        var defaultInstance = new StatPayloadInstance(SmokeIds.StunFromHealth, ratio, null, defaultHandle, TagSet.Empty);

        var pureInstallStopWatch = Stopwatch.StartNew();
        stats.InstallSource(defaultHandle, TagSet.Empty, [defaultInstance], []);
        pureInstallStopWatch.Stop();

        double totalMsPureInstall = pureInstallStopWatch.Elapsed.TotalMicroseconds; 
        double totalTicksPureInstall = pureInstallStopWatch.ElapsedTicks;

        GD.Print($"[Benchmark] Pure Install time: {totalMsPureInstall:F3} microseconds. Total ticks: {totalTicksPureInstall}");

        var pureRecalcStopWatch = Stopwatch.StartNew();
        Check(Approximately(stats.GetStatResult(SmokeIds.StunThreshold).FinalValue, 100.0), "default stun threshold applies");
        pureRecalcStopWatch.Stop();

        double totalMsPure = pureRecalcStopWatch.Elapsed.TotalMicroseconds; 
        double totalTicksPure = pureRecalcStopWatch.ElapsedTicks;

        GD.Print($"[Benchmark] Pure Evaluation time: {totalMsPure:F3} microseconds. Total ticks: {totalTicksPure}");


        stats.GetStatResult(SmokeIds.StunThreshold).PrintDebug("Stun Threshold");
        
        var removeStopWatch = Stopwatch.StartNew();
        stats.RemoveSource(defaultHandle);
        removeStopWatch.Stop();

        double totalMsRemove = removeStopWatch.Elapsed.Microseconds; 
        double totalTicksRemove = removeStopWatch.ElapsedTicks;

        GD.Print($"[Benchmark] Removal time: {totalMsRemove:F3} microseconds. Total ticks: {totalTicksRemove}");

        Check(Approximately(stats.GetStatResult(SmokeIds.StunThreshold).FinalValue, 0.0), "removing the source restores the default");

        var boots = data.Tags.Expand([new TagId("item.category.equipment.armour.boots")]);
        var bootsHandle = new PayloadSourceHandle(SourceKind.Equipment, 3);
        var bootsStatSourceValue = new StatSourceValue(bootsHandle, SmokeIds.ArmourRating, 200, false, StatStateLane.Default, boots);

        var pureInstallStopWatch2 = Stopwatch.StartNew();
        stats.InstallSource(bootsHandle, boots, [], [bootsStatSourceValue]);
        pureInstallStopWatch2.Stop();

        double totalMsPureInstall2 = pureInstallStopWatch2.Elapsed.Microseconds; 
        double totalTicksPureInstall2 = pureInstallStopWatch2.ElapsedTicks;

        GD.Print($"[Benchmark] Pure Install time # 2: {totalMsPureInstall2:F3} microseconds. Total ticks: {totalTicksPureInstall2}");

        var pureRecalcStopWatch2 = Stopwatch.StartNew();
        Check(Approximately(stats.GetStatResult(SmokeIds.ArmourRating).FinalValue, 200.0), "boots armour rating applies");
        pureRecalcStopWatch2.Stop();

        double totalMsPure2 = pureRecalcStopWatch2.Elapsed.TotalMicroseconds; 
        double totalTicksPure2 = pureRecalcStopWatch2.ElapsedTicks;

        GD.Print($"[Benchmark] Pure Evaluation time: {totalMsPure2:F3} microseconds. Total ticks: {totalTicksPure2}");


        stats.GetStatResult(SmokeIds.ArmourRating).PrintDebug("Armour Rating");

        double bootsBonus = data.Payloads.GetData<StatStaticPayload>(SmokeIds.BootsArmour).PrimaryMaxValue;
        var increasedBootsHandle = new PayloadSourceHandle(SourceKind.PassiveNode, 4);
        var bootsInstance = new StatPayloadInstance(SmokeIds.BootsArmour, bootsBonus, null, increasedBootsHandle, TagSet.Empty);
        stats.InstallSource(increasedBootsHandle, TagSet.Empty, [bootsInstance], []);
        Check(Approximately(stats.GetStatResult(SmokeIds.ArmourRating).FinalValue, 280.0), "scoped armour rating increase applies");
        stats.GetStatResult(SmokeIds.ArmourRating).PrintDebug("Armour Rating");

        var helmet = data.Tags.Expand([new TagId("item.category.equipment.armour.head")]);
        var helmetHandle = new PayloadSourceHandle(SourceKind.Equipment, 5);
        var helmetStatSourceValue = new StatSourceValue(helmetHandle, SmokeIds.ArmourRating, 100, false, StatStateLane.Default, helmet);
        stats.InstallSource(helmetHandle, TagSet.Empty, [], [helmetStatSourceValue]);
        Check(Approximately(stats.GetStatResult(SmokeIds.ArmourRating).FinalValue, 380.0), "helmet armour rating applies");
        stats.GetStatResult(SmokeIds.ArmourRating).PrintDebug("Armour Rating");


        stats.RemoveSource(bootsHandle);
        stats.RemoveSource(helmetHandle);
        stats.RemoveSource(increasedBootsHandle);
        Check(Approximately(stats.GetStatResult(SmokeIds.ArmourRating).FinalValue, 0.0), "removing the source restores the default");

        var stopwatch = Stopwatch.StartNew();
        int iterations = 1000;
        for (int i = 0; i < iterations; i++)
        {
            var result = stats.GetStatResult(SmokeIds.MovementSpeed);
        }
        stopwatch.Stop();

        double avgMicroseconds = stopwatch.Elapsed.TotalMilliseconds * 1000.0 / iterations;

        GD.Print($"[Benchmark] {iterations} stat evaluations took {stopwatch.Elapsed.TotalMilliseconds:F4} ms total.");
        GD.Print($"[Benchmark] Average time per evaluation: {avgMicroseconds:F3} microseconds.");

        GD.Print("[StatsSmokeTest] All smoke test assertions completed.");
    }

    private static void Check(bool ok, string what)
    {
        if (ok) GD.Print($"[StatsSmokeTest] ok: {what}");
        else GD.PushError($"[StatsSmokeTest] FAILED: {what}");
    }

    public static bool Approximately(double a, double b, double tolerance = 1e-6)
    {
        return Math.Abs(a - b) <= tolerance;
    }
}