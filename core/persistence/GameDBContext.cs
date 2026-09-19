using Godot;
using lethal.core.persistence.entities.characters;
using lethal.core.persistence.entities.stats;
using Microsoft.EntityFrameworkCore;

namespace lethal.core.persistence;
public class GameDbContext : DbContext
{
    public DbSet<StatDefinitionEntity> StatDefinitions { get; set; }
    public DbSet<TagEntity> TagEntity { get; set; }
    public DbSet<CharacterDefinitionEntity> CharacterDefinitions { get; set; }
    public DbSet<CharacterBaseStatEntity> CharacterBaseStats { get; set; }
    public DbSet<CharacterStartingResourceEntity> CharacterStartingResources { get; set; }

    public GameDbContext() { }
    public GameDbContext(DbContextOptions<GameDbContext> options) : base(options) { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            string dbPath = ProjectSettings.GlobalizePath("res://data/game_data.db");
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TagEntity>()
            .HasKey(t => t.Name);
        modelBuilder.Entity<StatTagEntity>()
            .HasKey(t => new { t.StatId, t.Tag });
        modelBuilder.Entity<StatDefinitionEntity>()
            .HasMany(s => s.Tags)
            .WithOne()
            .HasForeignKey(t => t.StatId);

        modelBuilder.Entity<StatTagEntity>()
            .HasOne<TagEntity>()
            .WithMany()
            .HasForeignKey(t => t.Tag);
        
        modelBuilder.Entity<CharacterDefinitionEntity>()
            .HasKey(c => c.Id);
        modelBuilder.Entity<CharacterBaseStatEntity>()
            .HasKey(b => new { b.CharacterId, b.StatId });
        modelBuilder.Entity<CharacterBaseStatEntity>()
            .HasOne<StatDefinitionEntity>()
            .WithMany()
            .HasForeignKey(b => b.StatId);
        modelBuilder.Entity<CharacterStartingResourceEntity>()
            .HasKey(r => new { r.CharacterId, r.StatId });
        modelBuilder.Entity<CharacterStartingResourceEntity>()
            .HasOne<StatDefinitionEntity>()
            .WithMany()
            .HasForeignKey(r => r.StatId);
        modelBuilder.Entity<CharacterDefinitionEntity>()
            .HasMany(c => c.BaseStats)
            .WithOne()
            .HasForeignKey(b => b.CharacterId);
        modelBuilder.Entity<CharacterDefinitionEntity>()
            .HasMany(c => c.StartingResources)
            .WithOne()
            .HasForeignKey(r => r.CharacterId);
    }
}
