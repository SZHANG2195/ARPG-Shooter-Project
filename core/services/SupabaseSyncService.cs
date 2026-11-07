using System;
using System.Net.Http;
using System.Threading.Tasks;
using Godot;
using lethal.core.config;

namespace lethal.core.services;

public static class SupabaseSyncService
{
    private const string Source = nameof(SupabaseSyncService);

    private static readonly System.Net.Http.HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(5)
    };

    public static async Task<string?> FetchTableJsonAsync(string tableName)
    {
        string url = $"{GameConfig.SupabaseUrl}/{tableName}?select=*";

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("apikey", GameConfig.SupabaseAnonKey);
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {GameConfig.SupabaseAnonKey}");
        _httpClient.DefaultRequestHeaders.Add("Accept-Profile", "gamedata");

        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                GD.Print($"[{Source}]: Successfully fetched {tableName}");
                return json;
            }
            
            string error = await response.Content.ReadAsStringAsync();
            GD.PushError($"[{Source}]: Error fetching {tableName}: HTTP {(int)response.StatusCode} - {error}");
        }
        catch (Exception ex)
        {
            GD.PushError($"[{Source}]: Network exception for {tableName}: {ex}");
        }

        return null;
    }
}