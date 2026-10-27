using System;
using System.Threading.Tasks;
using Godot;
using lethal.core.config;
using lethal.core.services;
using lethal.common.registries;
using lethal.core.domain.localizations;
using lethal.core.tools;

namespace lethal.core.bootstrap;
public sealed record BootstrapResult(GameData Data, ILocalizationKeys LocalizationKeys);
public partial class GameBootstrap : Node
{
    public static TaskCompletionSource<BootstrapResult> BootstrapReady { get; } = new();

    public override async void _Ready()
    {
        GD.Print("[GameBootstrap]: Initializing...");
        try
        {
            var db = DatabasePathConfig.Resolve();
            await DataSyncService.SyncIfNeededAsync(db);
            var keys = LocalizationLoader.LoadLocalizationsIntoGodot(db.ConnectionString);
            var data = GameDataLoader.Load(db.ConnectionString);

            GD.Print($"[GameData]: {data.Summary()}");

            if (OS.IsDebugBuild()) GameDataSmokeTest.Run(data);

            BootstrapReady.TrySetResult(new BootstrapResult(data, keys));
            GD.Print("[GameBootstrap]: Loading complete.");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[GameBootstrap] Bootstrap failed: {ex}");
            BootstrapReady.TrySetException(ex);
        }
    }
}