using Godot;
using lethal.gameplay.stats.enums;
using System;
using System.Collections.Generic;

namespace lethal.common.registry;
public static class TagRegistry
{
	private static readonly Dictionary<StringName, HashSet<StatType>> _taggedStats = new();

	public static void RegisterTag(StringName tag, IEnumerable<StatType> stats)
	{
		if (!_taggedStats.ContainsKey(tag))
		{
			_taggedStats[tag] = new HashSet<StatType>();
		}

		foreach (var stat in stats)
		{
			_taggedStats[tag].Add(stat);
		}
	}

	public static IEnumerable<StatType> GetStatsByTag(StringName tag)
	{
		if (_taggedStats.TryGetValue(tag, out var stats))
		{
			return stats;
		}
		GD.PrintErr($"[DynamicTagRegistry] Warning: Tag '{tag}' was requested but does not exist!");
		return Array.Empty<StatType>();
	}
}
