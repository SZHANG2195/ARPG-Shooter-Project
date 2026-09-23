using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using lethal.core.config;
using lethal.core.persistence;
using lethal.core.services;
using lethal.core.tools;
using lethal.common.registry;
using lethal.common.conditions;

namespace lethal.core.bootstrap;
public partial class GameBootstrap : Node
{

	public static TaskCompletionSource BootstrapReady { get; } = new();
    public override async void _Ready()
    {
        GD.Print("[GameBootstrap]: Initializing database...");

        var resolvedPaths = new Dictionary<string, string>();

        foreach (var kvp in GameDataConfig.SheetUrls)
        {
            string fileName = kvp.Key;
            string url = kvp.Value;

            string resolvedPath = await CsvSyncService.ResolveCsvPathAsync(fileName, url);
            resolvedPaths.Add(fileName, resolvedPath);
        }

        try
        {
            using var db = new GameDbContext();
            await db.Database.EnsureCreatedAsync();
            GD.Print("[GameBootstrap]: Database verified/created successfully.");

            var seeder = new DatabaseSeedService(db);
            await seeder.ClearDatabaseAsync();

            var orderedSeedMap = new (string FileName, Func<string, Task> SeedAction)[]
            {
                ("TagEntity.csv", path => seeder.SeedTagEntitiesAsync(path)),
                ("StatDefinitions.csv", path => seeder.SeedStatDefinitionsAsync(path)),
                ("CharacterDefinitions.csv", path => seeder.SeedCharacterDefinitionsAsync(path)),
                ("CharacterBaseStats.csv", path => seeder.SeedCharacterBaseStatsAsync(path)),
                ("CharacterStartingResources.csv", path => seeder.SeedCharacterStartingResourcesAsync(path)),
            };

            foreach (var (fileName, seedAction) in orderedSeedMap)
            {
                if (resolvedPaths.TryGetValue(fileName, out var filePath) && filePath != null)
                {
                    await seedAction(filePath);
                }
            }

            StatDefinitionRegistry.LoadAll(db);
            CharacterDefinitionRegistry.LoadAll(db);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[GameBootstrap] Error: Database initialization or seeding failed: {ex.Message}");
        }

        ConditionEvaluatorBootstrap.RegisterAll();

        DatabaseCodeGeneratorRunner.RunGenerationPipeline();

        GD.Print("[GameBootstrap]: Loading Complete.");
		BootstrapReady.SetResult();
    }
}