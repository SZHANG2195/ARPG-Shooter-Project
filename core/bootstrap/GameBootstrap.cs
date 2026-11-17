using System;
using System.Threading.Tasks;
using Godot;
using lethal.core.config;
using lethal.core.services;
using lethal.common.registries;
using lethal.core.domain.localizations;
using lethal.core.tools;
using System.Diagnostics;

namespace lethal.core.bootstrap;
public sealed record BootstrapResult(GameData Data, ILocalizationKeys LocalizationKeys);
public partial class GameBootstrap : Node
{
    public static TaskCompletionSource<BootstrapResult> BootstrapReady { get; } = new();

    private const string Source = nameof(GameBootstrap);

    public override async void _Ready()
    {
        GD.Print($"[{Source}]: Initializing...");

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var globalConfig = new ErrorConfig(
                OnError: message => { if (OS.IsDebugBuild()) throw new InvalidOperationException(message); GD.PushError(message); },
                OnWarning: message => GD.PrintRich($"[color=yellow]{message}[/color]"),
                StrictValidation: OS.IsDebugBuild()
            );

            var db = DatabasePathConfig.Resolve();
            await DataSyncService.SyncIfNeededAsync(db);
            var keys = LocalizationLoader.LoadLocalizationsIntoGodot(db.ConnectionString);
            var data = GameDataLoader.Load(db.ConnectionString, globalConfig);

            GD.Print($"[GameData]: {data.Summary()}");

            if (OS.IsDebugBuild()) {
                PrintDivider("GameDataSmokeTest");
                GameDataSmokeTest.Run(data);

                PrintDivider("StatsComponentSmokeTest :: Run 1 (cold — JIT + pool warmup)");
                StatsComponentSmokeTest.Run(data, globalConfig);

                PrintDivider("StatsComponentSmokeTest :: Run 2 (warm — steady state)");
                StatsComponentSmokeTest.Run(data, globalConfig);

                PrintDivider("Smoke tests complete");
            }
            stopwatch.Stop();

            BootstrapReady.TrySetResult(new BootstrapResult(data, keys));
            GD.Print($"[{Source}]: Loading complete in {stopwatch.ElapsedMilliseconds} ms ({stopwatch.Elapsed.TotalSeconds:F2}s).");
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            GD.PrintErr($"[{Source}]: Bootstrap failed after {stopwatch.ElapsedMilliseconds} ms: {exception}");
            BootstrapReady.TrySetException(exception);
        }
    }

    private static void PrintDivider(string label, char glyph = '─', int width = 60)
    {
        var line = new string(glyph, width);
        GD.Print(line);
        GD.Print($"{glyph} {label} {new string(glyph, Math.Max(0, width - label.Length - 3))}");
        GD.Print(line);
    }
}