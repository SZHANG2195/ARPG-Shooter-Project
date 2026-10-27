using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Microsoft.Data.Sqlite;
using System.Linq;

namespace lethal.core.services;

public class DatabaseSeedService
{
    private readonly string _connectionString;

    public DatabaseSeedService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task SeedAllFromSupabaseAsync(Dictionary<string, string> tableJsonPayloads)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        //i love atomicity
        await using var transaction = connection.BeginTransaction();
        try
        {
            foreach (var kvp in tableJsonPayloads)
            {
                string tableName = kvp.Key;
                string jsonPayload = kvp.Value;

                if (string.IsNullOrWhiteSpace(jsonPayload)) continue;

                //table might not exist, i.e. deleted for some reason
                var createTableCmd = connection.CreateCommand();
                createTableCmd.CommandText = $@"
                    CREATE TABLE IF NOT EXISTS {tableName} (
                        id TEXT PRIMARY KEY,
                        payload_json TEXT NOT NULL
                    );
                    DELETE FROM {tableName};"; //make sure table is blank, no point in wrestling with upserts and deletes
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