using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using lethal.core.config;
using Microsoft.Data.Sqlite;

namespace lethal.core.services;

public static class DataSyncService
{
    private const string MetaTable = "GameMetaData";

	//do not touch unless adding tables
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

    public static async Task SyncIfNeededAsync(DatabasePathConfig db)
    {
        int localVersion = await ReadLocalVersionAsync(db);
        string? metaJson = await SupabaseSyncService.FetchTableJsonAsync(MetaTable);
        int remoteVersion = ReadRemoteVersion(metaJson);

        GD.Print($"[DataSync]: Attempting version check -> Local: {localVersion}, Remote: {remoteVersion}");

        if (remoteVersion < 0)
        {
            if (localVersion == 0) throw new InvalidOperationException("No local data and the remote database is unreachable.");
            GD.PushWarning("[DataSync]: Remote version unavailable, continuing with local data.");
            return;
        }

        if (localVersion != 0 && remoteVersion <= localVersion)
        {
            GD.Print("[DataSync]: Local database is up to date. Skipping network sync.");
            return;
        }

        GD.Print("[DataSync]: New data version or empty local database. Syncing from Supabase...");

        var payloads = new Dictionary<string, string>();
        foreach (var table in CacheTables)
        {
            string? json = table == MetaTable ? metaJson : await SupabaseSyncService.FetchTableJsonAsync(table);
            if (!string.IsNullOrWhiteSpace(json)) payloads[table] = json;
        }

        var missing = CacheTables.Where(t => !payloads.ContainsKey(t)).ToList();
        if (missing.Count > 0)
        {
            if (localVersion == 0) throw new InvalidOperationException($"Sync incomplete and no local data. Missing: {string.Join(", ", missing)}");
            GD.PushWarning($"[DataSync]: Sync incomplete ({string.Join(", ", missing)}), continuing with local data.");
            return;
        }

        await new DatabaseSeedService(db.ConnectionString).SeedAllFromSupabaseAsync(payloads);
        GD.Print("[DataSync]: Supabase sync and seed complete.");
    }

    private static async Task<int> ReadLocalVersionAsync(DatabasePathConfig db)
    {
        await using var connection = new SqliteConnection(db.ConnectionString);
        await connection.OpenAsync();

        var exists = connection.CreateCommand();
        exists.CommandText = $"SELECT 1 FROM sqlite_master WHERE type='table' AND name='{MetaTable}';";
		//version is first column so we can just pull the first column
        if (await exists.ExecuteScalarAsync() is null) return 0;

        var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT payload_json FROM {MetaTable} LIMIT 1;";
        if (await cmd.ExecuteScalarAsync() is not string json || string.IsNullOrWhiteSpace(json)) return 0;

        using var doc = JsonDocument.Parse(json);
        return Math.Max(0, TryReadVersion(doc.RootElement));
    }

    private static int ReadRemoteVersion(string? metaJson)
    {
        if (string.IsNullOrWhiteSpace(metaJson)) return -1;
        using var doc = JsonDocument.Parse(metaJson);
        var root = doc.RootElement;
        return root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0 ? TryReadVersion(root[0]) : -1;
    }

    private static int TryReadVersion(JsonElement obj) => (obj.TryGetProperty("version", out var v) || obj.TryGetProperty("Version", out v)) ? v.GetInt32() : -1;
}