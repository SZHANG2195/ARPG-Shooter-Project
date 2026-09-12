using Godot;
using lethal.core.persistence;
using lethal.core.persistence.stat_identity;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace lethal.common.registry;
public static class StatDefinitionRegistry
{
    private static readonly Dictionary<StatId, StatDefinition> _definitions = new();
    private static readonly Dictionary<StringName, HashSet<StatId>> _byTag = new();

    public static void LoadAll(GameDbContext db)
    {
        var entities = db.StatDefinitions
            .Include(s => s.Tags)
            .ToList();

        foreach (var entity in entities)
        {
            var id = new StatId(entity.Id);
            var tags = entity.Tags.Select(t => (StringName)t.Tag).ToHashSet();

            _definitions[id] = new StatDefinition
            {
                Id = id,
                LocalizationKey = entity.LocalizationKey ?? string.Empty,
                IsRangePaired = entity.IsRangePaired,
                PairedCounterpart = entity.PairedCounterpartId is { } pairedId ? new StatId(pairedId) : null,
                Tags = tags
            };

            foreach (var tag in tags)
            {
                if (!_byTag.TryGetValue(tag, out var set))
                    _byTag[tag] = set = new HashSet<StatId>();
                set.Add(id);
            }
        }
    }

    public static void Register(StatDefinition definition)
    {
        _definitions[definition.Id] = definition;

        foreach (var tag in definition.Tags)
        {
            if (!_byTag.TryGetValue(tag, out var set))
                _byTag[tag] = set = new HashSet<StatId>();
            set.Add(definition.Id);
        }
    }

    public static StatDefinition? Get(StatId id)
	{
    	if (_definitions.TryGetValue(id, out var definition))
    	{
        	return definition;
    	}

    	string errorMsg = $"[StatDefinitionRegistry] Error: StatId '{id}' was requested but does not exist!";
    	#if DEBUG
    	throw new KeyNotFoundException(errorMsg);
    	#else
    	GD.PrintErr(errorMsg);
    	return null;
    	#endif
	}

    public static IEnumerable<StatId> GetByTag(StringName tag) =>
        _byTag.TryGetValue(tag, out var set) ? set : Enumerable.Empty<StatId>();
}
