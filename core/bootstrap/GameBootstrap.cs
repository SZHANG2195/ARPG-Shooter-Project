using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Microsoft.Data.Sqlite;
using lethal.core.config;
using lethal.core.services;

namespace lethal.core.bootstrap;

public partial class GameBootstrap : Node
{
    // Needed so the game knows when it can move on from bootstrap
    public static TaskCompletionSource BootstrapReady { get; } = new();

    private static readonly string[] CacheTables =
    [
        "GameMetaData",
        "Localizations",
        "DefinitionTags",
        "DefinitionStats",
        "DefinitionStatusEffects",
        "DefinitionFactions",
        "DefinitionCharacters",
        "DefinitionItemProfileKeys",
        "DefinitionItems",
        "DefinitionEquipment",
        "DefinitionAugments",
        "DefinitionAffixes",
        "DefinitionPayloads"
    ];

    public override async void _Ready()
    {
        GD.Print("[GameBootstrap]: Initializing database cache...");

        try
        {
            var dbPath = ProjectSettings.GlobalizePath("res://data/game_data.db");
            
            // Ensure directory exists
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(dbPath)!);

            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = dbPath,
                Password = DatabaseSecurityConfig.GetEncryptionKey()
            }.ToString();

            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();

            // Metadata check allows us to skip network calls if the local database is already up to date
            int localVersion = 0;
            var checkTableCmd = connection.CreateCommand();
            checkTableCmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='GameMetaData';";
            var tableExists = await checkTableCmd.ExecuteScalarAsync() != null;

            if (tableExists)
            {
                var getVersionCmd = connection.CreateCommand();
                getVersionCmd.CommandText = "SELECT payload_json FROM GameMetaData LIMIT 1;";
                using var reader = await getVersionCmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    string json = reader.GetString(0);
                    using var doc = System.Text.Json.JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("version", out var vProp) || doc.RootElement.TryGetProperty("Version", out vProp))
                    {
                        localVersion = vProp.GetInt32();
                    }
                }
            }

            int remoteVersion = -1;
            string? metaJson = await SupabaseSyncService.FetchTableJsonAsync("GameMetaData");

            if (!string.IsNullOrWhiteSpace(metaJson))
            {
                using var doc = System.Text.Json.JsonDocument.Parse(metaJson);
                if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array && doc.RootElement.GetArrayLength() > 0)
                {
                    var firstElem = doc.RootElement[0];
                    if (firstElem.TryGetProperty("version", out var vProp) || firstElem.TryGetProperty("Version", out vProp))
                    {
                        remoteVersion = vProp.GetInt32();
                    }
                }
            }

            GD.Print($"[GameBootstrap]: Version Check -> Local: {localVersion}, Remote: {remoteVersion}");

            if (remoteVersion > localVersion || localVersion == 0 || !tableExists)
            {
                GD.Print("[GameBootstrap]: New data version detected or local DB empty. Syncing from Supabase...");

                var tablePayloads = new Dictionary<string, string>();
                foreach (var tableName in CacheTables)
                {
                    string? json = (tableName == "GameMetaData" && metaJson != null) 
                        ? metaJson 
                        : await SupabaseSyncService.FetchTableJsonAsync(tableName);

                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        tablePayloads[tableName] = json;
                    }
                }

                var seeder = new DatabaseSeedService();
                await seeder.SeedAllFromSupabaseAsync(tablePayloads);
                GD.Print("[GameBootstrap]: Supabase sync and seed complete.");
            }
            else
            {
                GD.Print("[GameBootstrap]: Local database is up to date. Skipping network sync.");
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[GameBootstrap] Error: Database initialization or seeding failed: {ex.Message}");
        }

        GD.Print("[GameBootstrap]: Loading Complete.");
        BootstrapReady.SetResult();
    }
}