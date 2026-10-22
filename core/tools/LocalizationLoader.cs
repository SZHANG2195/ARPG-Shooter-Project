using Godot;
using Microsoft.Data.Sqlite;
using lethal.core.config;
using System.Text.Json;

namespace lethal.core.tools;

public partial class LocalizationLoader
{
    public static void LoadLocalizationsIntoGodot()
    {
        var dbPath = ProjectSettings.GlobalizePath("res://data/game_data.db");
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Password = DatabaseSecurityConfig.GetEncryptionKey()
        }.ToString();

        var enTranslation = new Translation { Locale = "en" };
        int count = 0;

        try
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT payload_json FROM Localizations;";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                string json = reader.GetString(0);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // Safely extract Key and En from the stored JSON payload
                string? key = root.TryGetProperty("key", out var kProp) ? kProp.GetString() 
                            : root.TryGetProperty("Key", out kProp) ? kProp.GetString() : null;

                string? en = root.TryGetProperty("en", out var eProp) ? eProp.GetString() 
                           : root.TryGetProperty("En", out eProp) ? eProp.GetString() : null;

                if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(en))
                {
                    enTranslation.AddMessage(key, en);
                    count++;
                }
            }
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[LocalizationLoader] Failed to load localizations: {ex.Message}");
            return;
        }

        if (count > 0)
        {
            TranslationServer.AddTranslation(enTranslation);
            GD.Print($"[LocalizationLoader]: Loaded {count} localizations into TranslationServer.");
        }
        else
        {
            GD.Print("[LocalizationLoader]: No localizations found to load.");
        }
    }
}