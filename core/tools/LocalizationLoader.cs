using Godot;
using Microsoft.Data.Sqlite;
using System.Text.Json;
using lethal.core.domain.localizations;

namespace lethal.core.tools;

public class LocalizationLoader
{
    public static LocalizationKeySet LoadLocalizationsIntoGodot(string connectionString)
    {
        var enTranslation = new Translation { Locale = "en" };
        var keys = new LocalizationKeySet();
        int count = 0;

        try
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT payload_json FROM Localizations;";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                using var doc = JsonDocument.Parse(reader.GetString(0));
                var root = doc.RootElement;

                //don't even know why I am bothering, probably never gonna add lang support outside of english
                string? key = root.TryGetProperty("key", out var keyProperty) ? keyProperty.GetString()  : root.TryGetProperty("Key", out keyProperty) ? keyProperty.GetString() : null;

                string? en = root.TryGetProperty("en", out var enProperty) ? enProperty.GetString() : root.TryGetProperty("En", out enProperty) ? enProperty.GetString() : null;

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

                if (LocalizationKeySet.IsDataOnlyMarker(en))
                {
                    keys.MarkDataOnly(new LocalizationKey(key));
                    continue;
                }


                if (en.TrimStart().StartsWith('[')) GD.PushWarning($"[LocalizationLoader] '{key}' starts with '[' but isn't the data-only marker.");

                enTranslation.AddMessage(key, en);
                count++;
            }
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[LocalizationLoader] Failed to load localizations: {ex.Message}");
            throw;
        }

        if (count > 0) TranslationServer.AddTranslation(enTranslation);

        GD.Print($"[LocalizationLoader]: Loaded {count} localizations total).");
        return keys;
    }
}