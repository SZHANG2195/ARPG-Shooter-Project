using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Godot;

namespace lethal.core.services;

public static class CsvSyncService
{
    private static readonly System.Net.Http.HttpClient _httpClient = new System.Net.Http.HttpClient
    {
        Timeout = TimeSpan.FromSeconds(3)
    };

    public static async Task<string> ResolveCsvPathAsync(string fileName, string remoteUrl)
    {
        string userDir = "user://csv";
        string userFilePath = $"{userDir}/{fileName}";
        string globalUserPath = ProjectSettings.GlobalizePath(userFilePath);
        string resPath = $"res://data/csv/{fileName}";

        var dir = DirAccess.Open("user://");
        if (dir != null && !dir.DirExists("csv"))
        {
            dir.MakeDir("csv");
        }

        try
        {
            GD.Print($"[CsvSync]: Checking for cloud update: {fileName}...");
            HttpResponseMessage response = await _httpClient.GetAsync(remoteUrl);

            if (response.IsSuccessStatusCode)
            {
                string csvContent = await response.Content.ReadAsStringAsync();
                await File.WriteAllTextAsync(globalUserPath, csvContent);
                GD.Print($"[CsvSync]: Successfully cached cloud version: {fileName}");
                return userFilePath;
            }
        }
        catch (Exception ex)
        {
            GD.Print($"[CsvSync] Error: Offline or timeout for {fileName}. Reason: {ex.Message}");
        }

        GD.Print($"[CsvSync]: Using fallback bundled CSV: {fileName}");
        return resPath;
    }
}