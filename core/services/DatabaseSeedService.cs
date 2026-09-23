using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Godot;
using lethal.core.persistence;
using lethal.core.persistence.entities.stats;
using System.Globalization;
using lethal.core.persistence.entities.characters;
using lethal.gameplay.stats.enums;
using System.Text.RegularExpressions;

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
                
                string name = parts[0].Trim();

                var tagEntity = new TagEntity
                {
                    Name = name,
                    CodeName = parts[1].Trim(),
                    LocalizationKey = parts[2].Trim(),
                    IsPlayerVisible = bool.TryParse(parts[3].Trim(), out bool vis) && vis,
                };

                var existing = await _dbContext.TagEntity.FindAsync(name);
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

                // Split by comma only outside of double quotes to preserve quoted CSV strings/multi-selects
                string[] parts = Regex.Split(line, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
                if (parts.Length < 3) continue;

                string statId = parts[0].Trim();

                var statEntity = new StatDefinitionEntity
                {
                    Id = statId,
                    CodeName = parts[1].Trim(),
                    LocalizationKey = parts[2].Trim(),
                    IsRangePaired = parts.Length > 3 && bool.TryParse(parts[3].Trim(), out bool parsedRange) && parsedRange,
                    PairedCounterpartId = parts.Length > 4 && !string.IsNullOrWhiteSpace(parts[4]) ? parts[4].Trim() : null,
                    DefaultValue = parts.Length > 5 && float.TryParse(parts[5].Trim(), CultureInfo.InvariantCulture, out float parsedValue) ? parsedValue : 0.0f
                };

                var existingStat = await _dbContext.StatDefinitions.FindAsync(statId);
                if (existingStat == null)
                {
                    _dbContext.StatDefinitions.Add(statEntity);
                }

                if (parts.Length > 6 && !string.IsNullOrWhiteSpace(parts[6]))
                {
                    string rawTags = parts[6].Trim().Trim('"');
                    string[] tagNames = rawTags.Split(',');

                    foreach (var tagName in tagNames)
                    {
                        string cleanTag = tagName.Trim();
                        if (string.IsNullOrEmpty(cleanTag)) continue;

                        bool tagExists = await _dbContext.TagEntity
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

    public async Task SeedCharacterDefinitionsAsync(string csvFilePath)
    {
        string globalPath = ProjectSettings.GlobalizePath(csvFilePath);

        if (!File.Exists(globalPath))
        {
            GD.PrintErr($"[DatabaseSeed] Warning: Character definitions file not found on disk at: {globalPath}");
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

                string charId = parts[0].Trim();

                var characterDefinitionEntity = new CharacterDefinitionEntity
                {
                    Id = charId,
                    TemplateId = parts[1].Trim(),
                    LocalizationKey = parts[2].Trim(),
                    PipelineFlags = parts.Length > 3 && Enum.TryParse<StatPipelineFlags>(parts[3].Trim(), out StatPipelineFlags parsedValue) ? parsedValue : StatPipelineFlags.SimpleEnemy
                };

                var existing = await _dbContext.CharacterDefinitions.FindAsync(charId);

                if (existing == null)
                {
                    _dbContext.Set<CharacterDefinitionEntity>().Add(characterDefinitionEntity);
                }
            }

            await _dbContext.SaveChangesAsync();
            GD.Print("[DatabaseSeed]: Character definition entities successfully seeded.");
        }
        catch (Exception ex)
        {
            string innerMessage = ex.InnerException?.Message ?? "No inner exception";
            GD.PrintErr($"[DatabaseSeed] Error: {ex.Message} | Inner: {innerMessage}");
            throw;
        }
    }

    public async Task SeedCharacterBaseStatsAsync(string csvFilePath)
    {
        string globalPath = ProjectSettings.GlobalizePath(csvFilePath);

        if (!File.Exists(globalPath))
        {
            GD.PrintErr($"[DatabaseSeed] Warning: Character base stats file not found on disk at: {globalPath}");
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

                string charId = parts[0].Trim();
                string statId = parts[1].Trim();

                var characterBaseStatEntity = new CharacterBaseStatEntity
                {
                    CharacterId = charId,
                    StatId = statId,
                    Value = parts.Length > 2 && float.TryParse(parts[2].Trim(), out float parsedValue) ? parsedValue : 0.0f
                };

                var existing = await _dbContext.CharacterBaseStats.FindAsync(charId, statId);
                if (existing == null)
                {
                    _dbContext.CharacterBaseStats.Add(characterBaseStatEntity);
                }
            }

            await _dbContext.SaveChangesAsync();
            GD.Print("[DatabaseSeed]: Character base stat entities successfully seeded.");
        }
        catch (Exception ex)
        {
            string innerMessage = ex.InnerException?.Message ?? "No inner exception";
            GD.PrintErr($"[DatabaseSeed] Error: {ex.Message} | Inner: {innerMessage}");
            throw;
        }
    }

    public async Task SeedCharacterStartingResourcesAsync(string csvFilePath)
    {
        string globalPath = ProjectSettings.GlobalizePath(csvFilePath);

        if (!File.Exists(globalPath))
        {
            GD.PrintErr($"[DatabaseSeed] Warning: Character starting resources file not found on disk at: {globalPath}");
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
                if (parts.Length < 2) continue;

                string charId = parts[0].Trim();
                string statId = parts[1].Trim();

                var characterStartingResourceEntity = new CharacterStartingResourceEntity
                {
                    CharacterId = charId,
                    StatId = statId,
                };

                var existingStat = await _dbContext.CharacterStartingResources.FindAsync(charId, statId);
                if (existingStat == null)
                {
                    _dbContext.CharacterStartingResources.Add(characterStartingResourceEntity);
                }
            }

            await _dbContext.SaveChangesAsync();
            GD.Print("[DatabaseSeed]: Character starting resource entities successfully seeded.");
        }
        catch (Exception ex)
        {
            string innerMessage = ex.InnerException?.Message ?? "No inner exception";
            GD.PrintErr($"[DatabaseSeed] Error: {ex.Message} | Inner: {innerMessage}");
            throw;
        }
    }

    public async Task ClearDatabaseAsync()
    {
        GD.Print("[DatabaseSeed]: Clearing stale database data...");

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
         
        try
        {
            await _dbContext.CharacterBaseStats.ExecuteDeleteAsync();
            await _dbContext.CharacterStartingResources.ExecuteDeleteAsync();
            await _dbContext.Set<StatTagEntity>().ExecuteDeleteAsync();

            await _dbContext.CharacterDefinitions.ExecuteDeleteAsync();
            await _dbContext.StatDefinitions.ExecuteDeleteAsync();
            await _dbContext.TagEntity.ExecuteDeleteAsync();

            await transaction.CommitAsync();

            GD.Print("[DatabaseSeed]: Database successfully cleared.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            GD.PrintErr($"[DatabaseSeed]: Clear failed, rolled back. {ex.Message}");
            throw;
        }
    }
}