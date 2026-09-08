using Godot;
using lethal.stats.data;
using lethal.stats.logic;
using lethal.stats.resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace lethal.stats.components;
public partial class StatsComponent : Node
{
	[Export]
	public StatsData CharacterData { get; set; }
	private Godot.Collections.Dictionary<StatType, float> _baseStats = new();
	private Dictionary<StatType, ResourcePool> _resourcePools = new();
	private readonly ModifierManager _modifierManager = new();
	private readonly Dictionary<StatType, float> _cachedFinalStats = new();

	private bool _isDirty = true;

	public override void _Ready()
	{
		if (CharacterData == null)
		{
			GD.PrintErr("[StatsComponent] Warning: CharacterData is null! Please assign a StatsData resource.");
			return;
		}
		InitializeBaseStats();
		InitializeResourcePools();
	}

	public void InitializeBaseStats()
	{
		foreach (var stat in CharacterData.BaseStats)
		{
			_baseStats[stat.Key] = stat.Value;
		}
	}

	public void InitializeResourcePools()
	{
		foreach (var stat in CharacterData.StartingResources)
		{
			if (!_baseStats.ContainsKey(stat))
			{
				GD.PrintErr($"[StatsComponent] Warning: Base stat for {stat} not found! Please ensure it is defined in BaseStats.");
				continue;
			}
			_resourcePools[stat] = new ResourcePool(stat);
			_resourcePools[stat].Set(_baseStats[stat], _baseStats[stat]);
		}
	}

	public void AddModifiers(ModifierSource source, IEnumerable<StatModifier> modifiers)
	{
		_modifierManager.AddModifiers(source, modifiers);
		_isDirty = true;
	}

	public void AddModifier(ModifierSource source, StatModifier modifier)
	{
		_modifierManager.AddModifier(source, modifier);
		_isDirty = true;
	}

	public void RemoveModifiers(ModifierSource source)
	{
		_modifierManager.RemoveModifiers(source);
		_isDirty = true;
	}

	public float GetFinalStat(StatType statType)
	{
		if (_isDirty)
		{
			_RecalculateFinalStats();
			_isDirty = false;
		}

		if (_cachedFinalStats.TryGetValue(statType, out var finalValue))
		{
			return finalValue;
		}
		else
		{
			GD.PrintErr($"[StatsComponent] Warning: Final stat for {statType} not found!");
			return 0.0f;
		}
	}

	private (Dictionary<EquipmentSlot, ScalingPool> SlotPools, Dictionary<(EquipmentSlot Slot, StatType Stat), ScalingPool> StatSlotPools) _GetScalars(IEnumerable<StatModifier> rawModifiers)
	{
		var slotPools = new Dictionary<EquipmentSlot, ScalingPool>();
		var statSpecificPools = new Dictionary<(EquipmentSlot, StatType), ScalingPool>();

		foreach (var modifier in rawModifiers)
		{
			bool isScalable = modifier.Type == ModifierType.Increased || modifier.Type == ModifierType.More;

			if (!isScalable || modifier.Slot == EquipmentSlot.None)
			{
				continue;
			}

			switch(modifier.Scope)
			{
				case ModifierScope.SlotEffect when modifier.Slot != EquipmentSlot.None && modifier.AffectedStats.Count == 0:
					_GetOrCreateSlotPool(slotPools, modifier.Slot).AddModifier(modifier);
					break;
				case ModifierScope.Stat when modifier.Slot != EquipmentSlot.None && modifier.AffectedStats.Count != 0:
					foreach (var stat in modifier.AffectedStats)
					{
						_GetOrCreateStatSpecificSlotPool(statSpecificPools, modifier.Slot, stat).AddModifier(modifier);
					}
					break;
				default:
					break;
			}
		}

		return (slotPools, statSpecificPools);
	}

	private void _RecalculateFinalStats()
	{
		_cachedFinalStats.Clear();

		var allActiveModifiers = _modifierManager.GetAllActiveModifiers();

		var (slotPools, statSpecificPools) = _GetScalars(allActiveModifiers);
		var (slotAffixes, globalModifiers) = _GroupModifiers(allActiveModifiers);

		var workingFlatValues = new Dictionary<StatType, float>();
		var workingIncreasedValues = new Dictionary<StatType, float>();
		var workingMoreValues = new Dictionary<StatType, float>();
		var baseOverrides = new Dictionary<StatType, float>();
		var finalOverrides = new Dictionary<StatType, float>();

		foreach (var pair in _baseStats)
		{
			workingFlatValues[pair.Key] = pair.Value;
			workingIncreasedValues[pair.Key] = 0.0f;
			workingMoreValues[pair.Key] = 1.0f;
		}

		var resolvedGearFlatValues = _CalculateGearFlatValues(allActiveModifiers);

		foreach (var gearEntry in resolvedGearFlatValues)
		{
			StatType statType = gearEntry.Key.Stat;
			float gearValue = gearEntry.Value;

			if (!workingFlatValues.ContainsKey(statType))
			{
				workingFlatValues[statType] = 0f;
			}

			workingFlatValues[statType] += gearValue;
		}

		_ExecutePreMultiplierDerived(allActiveModifiers, workingFlatValues);

		
	}

	private void _AddModifierToStatList(Dictionary<StatType, List<StatModifier>> modifierByStat, StatType statType, StatModifier modifier)
	{
		if (!modifierByStat.ContainsKey(statType))
		{
			modifierByStat[statType] = new List<StatModifier>();
		}
		modifierByStat[statType].Add(modifier);
	}

	private ScalingPool _GetOrCreateSlotPool(Dictionary<EquipmentSlot, ScalingPool> pools, EquipmentSlot slot)
	{
		if (!pools.TryGetValue(slot, out var pool))
		{
			pool = new ScalingPool();
			pools[slot] = pool;
		}
		return pool;
	}

	private ScalingPool _GetOrCreateStatSpecificSlotPool(Dictionary<(EquipmentSlot, StatType), ScalingPool> pools, EquipmentSlot slot, StatType stat)
	{
		var key = (slot, stat);
		if (!pools.TryGetValue(key, out var pool))
		{
			pool = new ScalingPool();
			pools[key] = pool;
		}
		return pool;
	}

	private (Dictionary<(EquipmentSlot, StatType), List<StatModifier>> ItemAffixes, Dictionary<StatType, List<StatModifier>> GlobalModifiers) _GroupModifiers(IEnumerable<StatModifier> modifiers)
	{
		var itemAffixesBySlotStat = new Dictionary<(EquipmentSlot Slot, StatType Stat), List<StatModifier>>();
		var globalModifierByStat = new Dictionary<StatType, List<StatModifier>>();

		foreach (var modifier in modifiers)
		{
			if (modifier.AffectedStats.Count == 0)
			{
				continue;
			}

			if (modifier.Slot != EquipmentSlot.None && modifier.Scope == ModifierScope.Stat)
			{
				foreach (var stat in modifier.AffectedStats)
				{
					var key = (modifier.Slot, stat);
					if (!itemAffixesBySlotStat.ContainsKey(key))
					{
						itemAffixesBySlotStat[key] = new List<StatModifier>();
					}
					itemAffixesBySlotStat[key].Add(modifier);
				}
			}
			else if (modifier.Slot == EquipmentSlot.None)
			{
				foreach (var stat in modifier.AffectedStats)
				{
					_AddModifierToStatList(globalModifierByStat, stat, modifier);
				}
			}
		}

		return (itemAffixesBySlotStat, globalModifierByStat);
	}

	private Dictionary<(EquipmentSlot Slot, StatType Stat), float> _CalculateGearFlatValues(
		IEnumerable<StatModifier> allActiveModifiers)
	{
		var resolvedLocalGearValues = new Dictionary<(EquipmentSlot Slot, StatType Stat), float>();

		var localItemModifiers = allActiveModifiers
			.Where(m =>
				m.Source.Category == ModifierSourceCategory.Equipment ||
				m.Source.Category == ModifierSourceCategory.Augment)
			.SelectMany(m =>
				m.AffectedStats, (modifier, statType) => new
				{
					Modifier = modifier,
					Stat = statType
				})
			.GroupBy(m =>
				new
				{
					Slot = m.Modifier.Source.OriginSlot,
                    m.Stat
				});
		
		foreach (var itemModifier in localItemModifiers)
		{
			EquipmentSlot slot = itemModifier.Key.Slot;
			StatType statType = itemModifier.Key.Stat;

			float itemBaseValue = 0.0f;
			float localFlatValue = 0.0f;
			float localIncreasedMultiplier = 0.0f;
			float localMoreMultiplier = 1.0f;

			foreach (var item in itemModifier)
			{
				var affix = item.Modifier;

				switch (affix.Type)
				{
					case ModifierType.Flat:
						localFlatValue += affix.GetValue();
						break;
					case ModifierType.Increased:
						localIncreasedMultiplier += affix.GetValue();
						break;
					case ModifierType.More:
						localMoreMultiplier *= 1.0f + affix.GetValue();
						break;
					default:
						string errorMsg = $"[StatsComponent] Error: Unhandled ModifierType '{affix.Type}' in local gear calculation!";
						#if DEBUG
						throw new InvalidOperationException(errorMsg);
						#else
						GD.PrintErr(errorMsg);
						break;
						#endif
				}
			}

			float localSubtotal = (itemBaseValue + localFlatValue) * (1.0f + localIncreasedMultiplier) * localMoreMultiplier;

			resolvedLocalGearValues[(slot, statType)] = localSubtotal;
		}

		return resolvedLocalGearValues;
	}

	private void _CalculateFinalStats(Dictionary<StatType, float> resolvedGearFlatValues, Dictionary<StatType, List<StatModifier>> globalModifiers)
	{
		foreach (var pair in _baseStats)
		{
			var statType = pair.Key;
			var defaultBaseValue = pair.Value;

			float flatValue = 0.0f;
			float increasedMultiplier = 0.0f;
			float moreMultiplier = 1.0f;

			float? baseOverride = null;
			float? finalOverride = null;

			if (resolvedGearFlatValues.TryGetValue(statType, out var gearFlat))
			{
				flatValue += gearFlat;
			}

			if (globalModifiers.TryGetValue(statType, out var modifiers))
			{
				foreach (var modifier in modifiers)
				{
					switch (modifier.Type)
					{
						case ModifierType.BaseOverride:
							baseOverride = modifier.GetValue();
							break;
						case ModifierType.Flat:
							flatValue += modifier.GetValue();
							break;
						case ModifierType.Increased:
							increasedMultiplier += modifier.GetValue();
							break;
						case ModifierType.More:
							moreMultiplier *= 1.0f + modifier.GetValue();
							break;
						case ModifierType.FinalOverride:
							if (finalOverride != null)
							{
								string warning = $"[StatsComponent] Warning: Multipler FinalOverrides found for {statType}! '{modifier.Source} is overriding existing value '{finalOverride.Value}'.";
								#if DEBUG
								throw new InvalidOperationException(warning);
								#else
								GD.PrintErr(warning);
								#endif
							}
							
							finalOverride = modifier.GetValue();
							break;
						default:
							string errorMsg = $"[StatsComponent] Error: Unhandled ModifierType '{modifier.Type}' passed to global modifiers!";
							#if DEBUG
							throw new InvalidOperationException(errorMsg);
							#else
							GD.PrintErr(errorMsg);
							break;
							#endif
					}
				}
			}

			float effectiveBase = baseOverride ?? defaultBaseValue;
			float standardCalculation = (effectiveBase + flatValue) * (1.0f + increasedMultiplier) * moreMultiplier;

			float finalValue = finalOverride ?? standardCalculation;
			_cachedFinalStats[statType] = finalValue;
		}
	}

	private void _ExecutePreMultiplierDerived(IEnumerable<StatModifier> allModifiers, Dictionary<StatType, float> workingFlatValues)
	{
		foreach (var modifier in allModifiers)
		{
			if (modifier is DerivedStatModifier derivedModifier && derivedModifier.Phase == ModifierExecutionPhase.PreMultipliers)
			{
				if (workingFlatValues.TryGetValue(derivedModifier.SourceStat, out float sourceValue))
				{
					float derivedValue = sourceValue * derivedModifier.Ratio;

					foreach (var targetStat in derivedModifier.AffectedStats)
					{
						if (!workingFlatValues.ContainsKey(targetStat))
						{
							workingFlatValues[targetStat] = 0.0f;
						}

						workingFlatValues[targetStat] += derivedValue;
					}
				}
			}
		}
	}

	private void _ExecuteSTatConversions(IEnumerable<StatModifier> allModifiers, Dictionary<StatType, float> workingFlatValues)
	{
		
	}
}
