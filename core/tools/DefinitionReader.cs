using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace lethal.core.tools;
public static class DefinitionReader
{
    public static List<TDef> ReadTable<TDef>(SqliteConnection connection, string table, JsonSerializerOptions options)
    {
        var defs = new List<TDef>();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT payload_json FROM {table};"; 
        using var reader = cmd.ExecuteReader();

        int row = 0;
        while (reader.Read())
        {
            row++;
            string json = reader.GetString(0);
            try
            {
                defs.Add(JsonSerializer.Deserialize<TDef>(json, options) ?? throw new JsonException("row deserialized to null."));
            }
            catch (JsonException ex)
            {
                string snippet = json.Length <= 120 ? json : json[..120] + "...";
                throw new InvalidDataException($"{table} row {row}: {ex.Message}\n  {snippet}", ex);
            }
        }
        return defs;
    }
}