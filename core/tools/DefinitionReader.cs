using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace lethal.core.tools;
public static class DefinitionReader
{

    private const string Source = nameof(DefinitionReader);

    public static List<TDef> ReadTable<TDef>(SqliteConnection connection, string table, JsonSerializerOptions options)
    {
        var definitions = new List<TDef>();

        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT payload_json FROM {table};"; 
        using var reader = command.ExecuteReader();

        int row = 0;
        while (reader.Read())
        {
            row++;
            string json = reader.GetString(0);
            try
            {
                definitions.Add(JsonSerializer.Deserialize<TDef>(json, options) ?? throw new JsonException($"[{Source}]: {table} row {row}: Deserialized to null."));
            }
            catch (JsonException exception)
            {
                string snippet = json.Length <= 120 ? json : json[..120] + "...";
                throw new InvalidDataException($"[{Source}]: {table} row {row}: {exception.Message}\n  {snippet}", exception);
            }
        }
        return definitions;
    }
}