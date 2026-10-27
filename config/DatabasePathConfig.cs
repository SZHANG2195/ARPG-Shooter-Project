using Godot;
using Microsoft.Data.Sqlite;

namespace lethal.core.config;
public sealed record DatabasePathConfig(string Path, string ConnectionString)
{
    public static DatabasePathConfig Resolve()
    {
        var path = ProjectSettings.GlobalizePath("res://data/game_data.db");
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Password = DatabaseSecurityConfig.GetEncryptionKey()
        }.ToString();

        return new DatabasePathConfig(path, connectionString);
    }
}