using Godot;
using lethal.core.domain.characters;
using lethal.core.domain.stats;
using lethal.core.domain.tags;
using lethal.gameplay.payloads.instances;
using lethal.gameplay.payloads.sources;
using lethal.gameplay.stats.components;
using lethal.core.config;
using lethal.common.registries;
using lethal.gameplay.stats.components.stat_dependencies;
using lethal.gameplay.payloads.enums;

public static class StatsComponentSmokeTest
{
    public static void Run(GameData d, ErrorConfig errorConfig)
    {
        GD.Print("[StatsSmokeTest] Starting component architecture verification...");

        // 1. Setup a test character definition and initialize the component
        var characterDef = d.Characters.Get(new CharacterId("player_strength_01"));
        var statsComponent = StatsComponent.Create(characterDef, d.Stats, d.Payloads, errorConfig, d.Footprints);

        // Verify base stats initialized properly from character or registry defaults
        // (Assuming you have a way to inspect or query the initial state/default lane base value)
        GD.Print("[StatsSmokeTest] StatsComponent successfully created.");

        // 2. Define a mock source handle (e.g., equipping an iron sword)
        var swordHandle = new PayloadSourceHandle(SourceKind.Equipment, 1001);
        var swordTags = TagSet.From([new TagId("item.weapon.sword")]);

        // Create a mock StatSourceValue (e.g., adding flat physical damage to Mainhand)
        var sourceValues = new[]
        {
            new StatSourceValue(
                SourceHandle: swordHandle,
                Stat: new StatId("physical_damage_min"),
                Value: 25.0,
                IsBase: true,
                Lane: StatStateLane.Mainhand
            )
        };

        // Empty payload list for this simple test, or add actual payload instances if available
        var payloadInstances = System.Array.Empty<StatPayloadInstance>();

        // 3. Test Installation
        bool installSuccess = statsComponent.InstallSource(swordHandle, swordTags, payloadInstances, sourceValues);
        Check(installSuccess, "Sword source installed successfully");

        // 4. Test Idempotent Re-installation / Overwrite
        bool reinstallSuccess = statsComponent.InstallSource(swordHandle, swordTags, payloadInstances, sourceValues);
        Check(reinstallSuccess, "Sword source reinstalled/overwritten cleanly");

        // 5. Test Removal & Teardown
        bool removeSuccess = statsComponent.RemoveSource(swordHandle);
        Check(removeSuccess, "Sword source removed successfully");

        // Test removing a non-existent handle returns false
        bool invalidRemove = statsComponent.RemoveSource(new PayloadSourceHandle(SourceKind.Equipment, 9999));
        Check(!invalidRemove, "Removing uninstalled source returns false");

        GD.Print("[StatsSmokeTest] All smoke test assertions completed.");
    }

    private static void Check(bool ok, string what)
    {
        if (ok) GD.Print($"[StatsSmokeTest] ok: {what}");
        else GD.PushError($"[StatsSmokeTest] FAILED: {what}");
    }
}