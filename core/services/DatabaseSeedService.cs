using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Microsoft.Data.Sqlite;
using lethal.core.config;
using System.Linq;

namespace lethal.core.services;

public class DatabaseSeedService
{
    private readonly string _connectionString;

    public DatabaseSeedService()
    {
        var dbPath = ProjectSettings.GlobalizePath("res://data/game_data.db");
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Password = DatabaseSecurityConfig.GetEncryptionKey()
        }.ToString();
    }

    public async Task SeedAllFromSupabaseAsync(Dictionary<string, string> tableJsonPayloads)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        // Use a transaction for fast, atomic bulk-inserting
        await using var transaction = connection.BeginTransaction();

        try
        {
            foreach (var kvp in tableJsonPayloads)
            {
                string tableName = kvp.Key;
                string jsonPayload = kvp.Value;

                if (string.IsNullOrWhiteSpace(jsonPayload)) continue;

                // No guarantee that the table exists, so create it if it doesn't
                var createTableCmd = connection.CreateCommand();
                createTableCmd.CommandText = $@"
                    CREATE TABLE IF NOT EXISTS {tableName} (
                        id TEXT PRIMARY KEY,
                        payload_json TEXT NOT NULL
                    );
                    DELETE FROM {tableName};"; // Clear old cache data before inserting fresh sync
                await createTableCmd.ExecuteNonQueryAsync();


                using var jsonDoc = System.Text.Json.JsonDocument.Parse(jsonPayload);
                if (jsonDoc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var element in jsonDoc.RootElement.EnumerateArray())
                    {
                        var firstPropVal = element.EnumerateObject().FirstOrDefault().Value;
                        string id = firstPropVal.ValueKind switch
                        {
                            System.Text.Json.JsonValueKind.Number => firstPropVal.GetInt64().ToString(),
                            System.Text.Json.JsonValueKind.String => firstPropVal.GetString() ?? Guid.NewGuid().ToString(),
                            _ => firstPropVal.ToString() ?? Guid.NewGuid().ToString()
                        };

                        var insertCmd = connection.CreateCommand();
                        insertCmd.CommandText = $@"
                            INSERT OR REPLACE INTO {tableName} (id, payload_json) 
                            VALUES ($id, $payload);";
                        
                        insertCmd.Parameters.AddWithValue("$id", id ?? Guid.NewGuid().ToString());
                        insertCmd.Parameters.AddWithValue("$payload", element.GetRawText());
                        
                        await insertCmd.ExecuteNonQueryAsync();
                    }
                }

                GD.Print($"[DatabaseSeed]: Successfully cached table '{tableName}' locally.");
            }

            await transaction.CommitAsync();
            GD.Print("[DatabaseSeed]: Full local SQLite cache sync completed successfully!");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            GD.PrintErr($"[DatabaseSeed] Error during local caching: {ex.Message}");
            throw;
        }
    }
}