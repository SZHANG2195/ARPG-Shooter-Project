using Godot;
using Microsoft.Data.Sqlite;
using lethal.core.config;
using System.Text.Json;
using lethal.core.domain.localization;
using lethal.core.domain.localizations;

namespace lethal.core.tools;

public class LocalizationLoader
{
    public static LocalizationKeySet LoadLocalizationsIntoGodot()
    {
        var dbPath = ProjectSettings.GlobalizePath("res://data/game_data.db");
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Password = DatabaseSecurityConfig.GetEncryptionKey()
        }.ToString();

        var enTranslation = new Translation { Locale = "en" };
        var keys = new LocalizationKeySet();
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

                if (string.IsNullOrWhiteSpace(key))
                {
                    GD.PushWarning("[LocalizationLoader] Skipped a row with no key.");
                    continue;
                }
                
                if (string.IsNullOrWhiteSpace(en))
                {
                    GD.PushWarning($"[LocalizationLoader] Key '{key}' has no En text.");
                    continue;
                }

                bool dataOnly = LocalizationKeySet.IsDataOnlyMarker(en);

                if (!keys.Add(new LocalizationKey(key), dataOnly))
                {
                    GD.PushWarning($"[LocalizationLoader] Duplicate key '{key}'.");
                    continue;
                }

                if (dataOnly) continue;

                if (en.TrimStart().StartsWith('['))
                GD.PushWarning($"[LocalizationLoader] '{key}' starts with '[' but isn't the data-only marker.");

                enTranslation.AddMessage(key, en);
                count++;
            }
        }
        catch (System.Exception ex)
        {
        GD.PrintErr($"[LocalizationLoader] Failed to load localizations: {ex.Message}");
        throw;   // see below
        }

        if (count > 0)
            TranslationServer.AddTranslation(enTranslation);

        GD.Print($"[LocalizationLoader]: Loaded {count} localizations ({keys} keys total).");
        return keys;
    }
}