using Godot;
using lethal.stats.data;
using System;
using System.Collections.Generic;

namespace lethal.stats.logic;
public partial class ModifierManager
{
	private readonly Dictionary<ModifierSource, List<StatModifier>> _sourceModifiers = new();
	
	public void AddModifiers(ModifierSource source, IEnumerable<StatModifier> modifiers)
	{
		if (!_sourceModifiers.TryGetValue(source, out var list))
		{
			list = new List<StatModifier>();
			_sourceModifiers[source] = list;
		} 
		else
		{
			list.Clear();
		}

		list.AddRange(modifiers);
	}

	public void AddModifier(ModifierSource source, StatModifier modifier)
	{
		if (!_sourceModifiers.TryGetValue(source, out var list))
		{
			list = new List<StatModifier>();
			_sourceModifiers[source] = list;
		}

		list.Add(modifier);
	}

	public void RemoveModifiers(ModifierSource source)
	{
		_sourceModifiers.Remove(source);
	}

	public IEnumerable<StatModifier> GetAllActiveModifiers()
	{
		foreach (var list in _sourceModifiers.Values)
		{
			foreach (var modifier in list)
			{
				yield return modifier;
			}
		}
	}

	public void ClearAllModifiers()
	{
		_sourceModifiers.Clear();
	}
}
