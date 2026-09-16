using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Godot;
using lethal.core.persistence;
using lethal.core.persistence.entities.stats;

namespace lethal.core.services;

public class DatabaseSeedService
{
    private readonly GameDbContext _dbContext;

    public DatabaseSeedService(GameDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SeedTagEntitiesAsync(string csvFilePath)
    {
        string globalPath = ProjectSettings.GlobalizePath(csvFilePath);

        if (!File.Exists(globalPath))
        {
            GD.PrintErr($"[DatabaseSeed] Warning: Tag entities file not found on disk at: {globalPath}");
            return;
        }

        try
        {
            string[] lines = await File.ReadAllLinesAsync(globalPath);

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                string[] parts = line.Split(',');
                if (parts.Length < 3) continue;

                var tagEntity = new TagEntity
                {
                    Name = parts[0].Trim(),
                    CodeName = parts[1].Trim(),
                    LocalizationKey = parts[2].Trim(),
                    IsPlayerVisible = bool.TryParse(parts[3].Trim(), out bool vis) && vis,
                };

                var existing = await _dbContext.Set<TagEntity>().FindAsync(tagEntity.Name);
                if (existing == null)
                {
                    _dbContext.Set<TagEntity>().Add(tagEntity);
                }
            }

            await _dbContext.SaveChangesAsync();
            GD.Print("[DatabaseSeed]: Tag entities successfully seeded.");
        }
        catch (Exception ex)
        {
            string innerMessage = ex.InnerException?.Message ?? "No inner exception";
            GD.PrintErr($"[DatabaseSeed] Error: {ex.Message} | Inner: {innerMessage}");
            throw;
        }
    }

    public async Task SeedStatDefinitionsAsync(string csvFilePath)
    {
        string globalPath = ProjectSettings.GlobalizePath(csvFilePath);

        if (!File.Exists(globalPath))
        {
            GD.PrintErr($"[DatabaseSeed] Warning: Stat definitions file not found on disk at: {globalPath}");
            return;
        }

        try
        {
            string[] lines = await File.ReadAllLinesAsync(globalPath);

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                string[] parts = line.Split(',');
                if (parts.Length < 3) continue;

                string statId = parts[0].Trim();

                var statEntity = new StatDefinitionEntity
                {
                    Id = statId,
                    CodeName = parts[1].Trim(),
                    LocalizationKey = parts[2].Trim(),
                    IsRangePaired = parts.Length > 3 && bool.TryParse(parts[3].Trim(), out bool parsedRange) && parsedRange,
                    PairedCounterpartId = parts.Length > 4 && !string.IsNullOrWhiteSpace(parts[4]) ? parts[4].Trim() : null
                };

                var existingStat = await _dbContext.StatDefinitions.FindAsync(statId);
                if (existingStat == null)
                {
                    _dbContext.StatDefinitions.Add(statEntity);
                }

                if (parts.Length > 5 && !string.IsNullOrWhiteSpace(parts[5]))
                {
                    string rawTags = parts[5].Trim().Trim('"');
                    string[] tagNames = rawTags.Split(";");

                    foreach (var tagName in tagNames)
                    {
                        string cleanTag = tagName.Trim();
                        if (string.IsNullOrEmpty(cleanTag)) continue;

                        bool tagExists = await _dbContext.Set<TagEntity>()
                            .AnyAsync(t => t.Name == cleanTag);

                        if (!tagExists)
                        {
                            GD.PrintErr($"[DatabaseSeed] WARNING: Stat '{statId}' references tag '{cleanTag}', but that tag does not exist in TagEntity.csv!");
                            continue;
                        }

                        bool mappingExists = await _dbContext.Set<StatTagEntity>()
                            .AnyAsync(st => st.StatId == statId && st.Tag == cleanTag);

                        if (!mappingExists)
                        {
                            _dbContext.Set<StatTagEntity>().Add(new StatTagEntity
                            {
                                StatId = statId,
                                Tag = cleanTag
                            });
                        }
                    }
                }
            }

            await _dbContext.SaveChangesAsync();
            GD.Print("[DatabaseSeed]: Stat definitions and inline tag mappings successfully seeded.");
        }
        catch (Exception ex)
        {
            string innerMessage = ex.InnerException?.Message ?? "No inner exception";
            GD.PrintErr($"[DatabaseSeed] Error: {ex.Message} | Inner: {innerMessage}");
            throw;
        }
    }
}