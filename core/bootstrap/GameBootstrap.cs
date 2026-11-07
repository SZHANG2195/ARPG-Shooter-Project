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

    private const string Source = nameof(GameBootstrap);

    public override async void _Ready()
    {
        GD.Print($"[{Source}]: Initializing...");
        try
        {
            var globalConfig = new ErrorConfig(
                OnError: msg => { if (OS.IsDebugBuild()) throw new InvalidOperationException(msg); GD.PushError(msg); },
                OnWarning: msg => GD.PrintRich($"[color=yellow]{msg}[/color]"),
                StrictValidation: OS.IsDebugBuild()
            );

            var db = DatabasePathConfig.Resolve();
            await DataSyncService.SyncIfNeededAsync(db);
            var keys = LocalizationLoader.LoadLocalizationsIntoGodot(db.ConnectionString);
            var data = GameDataLoader.Load(db.ConnectionString, globalConfig);

            GD.Print($"[GameData]: {data.Summary()}");

            if (OS.IsDebugBuild()) GameDataSmokeTest.Run(data);

            BootstrapReady.TrySetResult(new BootstrapResult(data, keys));
            GD.Print($"[{Source}]: Loading complete.");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[{Source}]: Bootstrap failed: {ex}");
            BootstrapReady.TrySetException(ex);
        }
    }
}