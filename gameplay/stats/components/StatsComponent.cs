using Godot;
using lethal.common.registry;
using lethal.core.persistence;
using lethal.core.persistence.stat_identity;
using lethal.gameplay.stats.data;
using lethal.gameplay.stats.enums;
using lethal.gameplay.stats.logic;
using lethal.gameplay.stats.modifiers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace lethal.gameplay.stats.components;
public partial class StatsComponent : Node
{
	[Export]
	public required StatsData CharacterData { get; set; }
	private Dictionary<StatId, float> _baseStats = new();
	private Dictionary<StatId, ResourcePool> _resourcePools = new();
	private readonly ModifierManager _modifierManager = new();
	private readonly Dictionary<StatId, float> _cachedFinalStats = new();

	private bool _isDirty = true;

	public override void _Ready()
	{
		using var db = new GameDbContext();
		GD.Print($"Stat count: {db.StatDefinitions.Count()}");
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
    	var characterDef = CharacterDefinitionRegistry.Get(CharacterData.CharacterId);
    	if (characterDef == null)
    	{
    	    GD.PrintErr($"[StatsComponent] Warning: No character definition found for '{CharacterData.CharacterId}'!");
    	    return;
    	}

    	foreach (var stat in characterDef.BaseStats)
    	{
    		_baseStats[stat.Key] = stat.Value;
    	}
	}

	public void InitializeResourcePools()
	{
    	var characterDef = CharacterDefinitionRegistry.Get(CharacterData.CharacterId);
    	if (characterDef == null)
    	{
        	return;
    	}

    	foreach (var stat in characterDef.StartingResources)
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

	public float GetFinalStat(StatId statId)
	{
		if (_isDirty)
		{
			_RecalculateFinalStats();
			_isDirty = false;
		}

		if (_cachedFinalStats.TryGetValue(statId, out var finalValue))
		{
			return finalValue;
		}
		else
		{
			GD.PrintErr($"[StatsComponent] Warning: Final stat for {statId} not found!");
			return 0.0f;
		}
	}

	private void _RecalculateFinalStats()
	{
		_cachedFinalStats.Clear();

		var allModifiers = new List<StatModifier>(_modifierManager.GetAllActiveModifiers());

		var workingFlatValues = new Dictionary<StatId, float>();
		var workingIncreasedValues = new Dictionary<StatId, float>();
		var workingMoreValues = new Dictionary<StatId, float>();
		var baseOverrides = new Dictionary<StatId, float>();
		var finalOverrides = new Dictionary<StatId, float>();

		foreach (var pair in _baseStats)
		{
			workingFlatValues[pair.Key] = pair.Value;
			workingIncreasedValues[pair.Key] = 0.0f;
			workingMoreValues[pair.Key] = 1.0f;
		}

		var resolvedGearFlatValues = _CalculateGearFlatValues(allModifiers);

		var aggregatedGearFlats = new Dictionary<StatId, float>();

		foreach (var gearEntry in resolvedGearFlatValues)
		{
			StatId statId = gearEntry.Key.Stat;
			float gearValue = gearEntry.Value;

			if (!aggregatedGearFlats.ContainsKey(statId))
			{
				aggregatedGearFlats[statId] = 0f;
			}
			aggregatedGearFlats[statId] += gearValue;

			if (!workingFlatValues.ContainsKey(statId))
			{
				workingFlatValues[statId] = 0f;
			}

			workingFlatValues[statId] += gearValue;
		}

		var attributeModifiers = _ExecuteAttributeCalculations(allModifiers, workingFlatValues);
		allModifiers.AddRange(attributeModifiers);

		var conversionModifiers = _ExecuteStatConversions(allModifiers, workingFlatValues);
		allModifiers.AddRange(conversionModifiers);

		_ExecutePreMultiplierDerived(allModifiers, workingFlatValues);

		var (slotPools, statSpecificPools) = _GetScalars(allModifiers);
		var (slotAffixes, globalModifiers) = _GroupModifiers(allModifiers);

		_PopulateWorkingMultipliers(workingIncreasedValues, workingMoreValues, baseOverrides, finalOverrides, globalModifiers);

		_CalculatePrimaryStats(workingFlatValues, workingIncreasedValues, workingMoreValues, baseOverrides, finalOverrides);

		_ExecutePostMultiplierDerivedAndFinalize(allModifiers, workingFlatValues, workingIncreasedValues, workingMoreValues, resolvedGearFlatValues);
	}

	private Dictionary<(EquipmentSlot Slot, StatId Stat), float> _CalculateGearFlatValues(
		IEnumerable<StatModifier> allActiveModifiers)
	{
		var resolvedLocalGearValues = new Dictionary<(EquipmentSlot Slot, StatId Stat), float>();

		var localItemModifiers = allActiveModifiers
			.Where(m =>
				m.Source.Category == ModifierSourceCategory.Equipment ||
				m.Source.Category == ModifierSourceCategory.Augment)
			.SelectMany(m =>
				m.AffectedStats, (modifier, statId) => new
				{
					Modifier = modifier,
					Stat = statId
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
			StatId statId = itemModifier.Key.Stat;

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

			resolvedLocalGearValues[(slot, statId)] = localSubtotal;
		}

		return resolvedLocalGearValues;
	}

	private IEnumerable<StatModifier> _ExecuteAttributeCalculations(IEnumerable<StatModifier> allModifiers, Dictionary<StatId, float> workingFlatValues)
	{
    	float currentStrength = workingFlatValues.GetValueOrDefault(Stats.Strength, 0.0f);
    	float currentAgility = workingFlatValues.GetValueOrDefault(Stats.Agility, 0.0f);
    	float currentIntelligence = workingFlatValues.GetValueOrDefault(Stats.Intelligence, 0.0f);

    	var attributeDerivedModifiers = AttributeCalculator.GetDerivedModifiers(
    	currentStrength, currentAgility, currentIntelligence, allModifiers);

    	foreach (var attributeModifier in attributeDerivedModifiers)
    	{
        	if (attributeModifier.Type == ModifierType.Flat)
        	{
            	foreach (var targetStat in attributeModifier.AffectedStats)
            	{
                	if (!workingFlatValues.ContainsKey(targetStat))
                	{
                    	workingFlatValues[targetStat] = 0.0f;
                	}

                	workingFlatValues[targetStat] += attributeModifier.GetValue();
            	}
        	}
    	}

    	return attributeDerivedModifiers;
	}

	private IEnumerable<StatModifier> _ExecuteStatConversions(IEnumerable<StatModifier> allModifiers, Dictionary<StatId, float> workingFlatValues)
	{
		var generatedModifiers = new List<StatModifier>();
		var siphonedAmounts = new Dictionary<StatId, float>();

		foreach (var modifier in allModifiers)
		{
			if (modifier is StatConversionModifier conversion && conversion.TargetSplitValues != null && conversion.TargetSplitValues.Count > 0)
			{
				StatId sourceStat = conversion.SourceStat;

				if (!workingFlatValues.TryGetValue(sourceStat, out float sourceValue) || sourceValue <= 0.0f)
				{
					continue;
				}

				float alreadySiphonedAmount = siphonedAmounts.GetValueOrDefault(sourceStat, 0.0f);
				float availableSource = sourceValue - alreadySiphonedAmount;

				if (availableSource <= 0.0f) continue;

				float totalConvertedAmount = availableSource * conversion.Ratio;
				siphonedAmounts[sourceStat] = alreadySiphonedAmount + totalConvertedAmount;

				foreach (var split in conversion.TargetSplitValues)
				{
					StatId targetStat = split.Key;
					float splitWeight = split.Value;
					float finalConversionValue = totalConvertedAmount * splitWeight;

					if (finalConversionValue > 0.0f)
					{
						var conversionMod = StatModifier.CreateSingleStaticModifier(
							ModifierType.Flat,
							finalConversionValue,
							targetStat,
							new ModifierSource(ModifierSourceCategory.Conversion, $"Converted from {sourceStat}")
						);

						if (conversionMod != null)
						{
							generatedModifiers.Add(conversionMod);
						}
					}
				}
			}
		}

		foreach (var siphon in siphonedAmounts)
		{
			if (workingFlatValues.ContainsKey(siphon.Key))
			{
				workingFlatValues[siphon.Key] -= siphon.Value;
				if (workingFlatValues[siphon.Key] < 0.0f) workingFlatValues[siphon.Key] = 0.0f;
			}
		}

		return generatedModifiers;
	}

	private void _ExecutePreMultiplierDerived(IEnumerable<StatModifier> allModifiers, Dictionary<StatId, float> workingFlatValues)
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

	private (Dictionary<EquipmentSlot, ScalingPool> SlotPools, Dictionary<(EquipmentSlot Slot, StatId Stat), ScalingPool> StatSlotPools) _GetScalars(IEnumerable<StatModifier> rawModifiers)
	{
		var slotPools = new Dictionary<EquipmentSlot, ScalingPool>();
		var statSpecificPools = new Dictionary<(EquipmentSlot, StatId), ScalingPool>();

		foreach (var modifier in rawModifiers)
		{
			bool isScalable = modifier.Type == ModifierType.Increased || modifier.Type == ModifierType.More;

			if (!isScalable || modifier.Slot == EquipmentSlot.None)
			{
				continue;
			}

			switch (modifier.Scope)
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

	private (Dictionary<(EquipmentSlot, StatId), List<StatModifier>> ItemAffixes, Dictionary<StatId, List<StatModifier>> GlobalModifiers) _GroupModifiers(IEnumerable<StatModifier> modifiers)
	{
		var itemAffixesBySlotStat = new Dictionary<(EquipmentSlot Slot, StatId Stat), List<StatModifier>>();
		var globalModifierByStat = new Dictionary<StatId, List<StatModifier>>();

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

	public void _PopulateWorkingMultipliers(
		Dictionary<StatId, float> workingIncreasedValues,
		Dictionary<StatId, float> workingMoreValues,
		Dictionary<StatId, float> baseOverrides,
		Dictionary<StatId, float> finalOverrides,
		Dictionary<StatId, List<StatModifier>> globalModifiers)
	{
		foreach (var pair in globalModifiers)
		{
			StatId statId = pair.Key;
			var modifiers = pair.Value;

			foreach (var modifier in modifiers)
			{
				switch (modifier.Type)
				{
					case ModifierType.Increased:
						if (!workingIncreasedValues.ContainsKey(statId)) workingIncreasedValues[statId] = 0.0f;
						workingIncreasedValues[statId] += modifier.GetValue();
						break;
					case ModifierType.More:
						if (!workingMoreValues.ContainsKey(statId)) workingMoreValues[statId] = 1.0f;
						workingMoreValues[statId] += modifier.GetValue();
						break;
					case ModifierType.BaseOverride:
						if (baseOverrides.ContainsKey(statId))
						{
							string warning = $"[StatsComponent] Warning: Multiple BaseOverrides found for {statId}! '{modifier.Source}' is overriding existing base value.";
							#if DEBUG
							throw new InvalidOperationException(warning);
							#else
							GD.PrintErr(warning);
							#endif
						}
						baseOverrides[statId] = modifier.GetValue();
						break;
					case ModifierType.FinalOverride:
						if (finalOverrides.ContainsKey(statId))
						{
							string warning = $"[StatsComponent] Warning: Multiple FinalOverrides found for {statId}! '{modifier.Source}' is overriding existing final value.";
							#if DEBUG
							throw new InvalidOperationException(warning);
							#else
							GD.PrintErr(warning);
							#endif
						}
						finalOverrides[statId] = modifier.GetValue();
						break;
				}
			}
		}
	}

	private void _CalculatePrimaryStats(
		Dictionary<StatId, float> workingFlatValues,
		Dictionary<StatId, float> workingIncreasedValues,
		Dictionary<StatId, float> workingMoreValues,
		Dictionary<StatId, float> baseOverrides,
		Dictionary<StatId, float> finalOverrides)
	{
		foreach (var pair in _baseStats)
		{
			var statId = pair.Key;
			float defaultBaseValue = pair.Value;

			float effectiveBase = baseOverrides.GetValueOrDefault(statId, defaultBaseValue);
			float flatValue = workingFlatValues.GetValueOrDefault(statId, defaultBaseValue) - defaultBaseValue;
			float increasedMultiplier = workingIncreasedValues.GetValueOrDefault(statId, 0.0f);
			float moreMultiplier = workingMoreValues.GetValueOrDefault(statId, 1.0f);

			float standardCalculation = (effectiveBase + flatValue) * (1.0f + increasedMultiplier) * moreMultiplier;

			float finalValue = finalOverrides.TryGetValue(statId, out var overrideVal) ? overrideVal : standardCalculation;

			_cachedFinalStats[statId] = finalValue;
		}
	}

	private void _ExecutePostMultiplierDerivedAndFinalize(
		IEnumerable<StatModifier> allModifiers,
		Dictionary<StatId, float> workingFlatValues,
		Dictionary<StatId, float> workingIncreasedValues,
		Dictionary<StatId, float> workingMoreValues,
		Dictionary<(EquipmentSlot Slot, StatId Stat), float> resolvedGearFlatValues)
	{
		foreach (var modifier in allModifiers)
		{
			float sourceValue = 0.0f;

			if (modifier is DerivedStatModifier derivedModifier && derivedModifier.Phase == ModifierExecutionPhase.PostMultipliers)
			{
				if (derivedModifier.Slot != EquipmentSlot.None)
				{
					sourceValue = resolvedGearFlatValues.GetValueOrDefault((derivedModifier.Slot, derivedModifier.SourceStat), 0.0f);
				}
				else
				{
					sourceValue = _cachedFinalStats.GetValueOrDefault(derivedModifier.SourceStat, 0.0f);
				}

				float derivedFlatValue = sourceValue * derivedModifier.Ratio;

				foreach (var targetStat in derivedModifier.AffectedStats)
				{
					float defaultBase = _baseStats.GetValueOrDefault(targetStat, 0.0f);

					float existingFlats = workingFlatValues.GetValueOrDefault(targetStat, defaultBase) - defaultBase;
					float totalFlat = existingFlats + derivedFlatValue;

					float increased = workingIncreasedValues.GetValueOrDefault(targetStat, 0.0f);
					float more = workingMoreValues.GetValueOrDefault(targetStat, 1.0f);

					float finalDerivedStatValue = (defaultBase + totalFlat) * (1.0f + increased) * more;
					_cachedFinalStats[targetStat] = finalDerivedStatValue;
				}
			}
		}
	}

	private void _AddModifierToStatList(Dictionary<StatId, List<StatModifier>> modifierByStat, StatId statId, StatModifier modifier)
	{
		if (!modifierByStat.ContainsKey(statId))
		{
			modifierByStat[statId] = new List<StatModifier>();
		}
		modifierByStat[statId].Add(modifier);
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

	private ScalingPool _GetOrCreateStatSpecificSlotPool(Dictionary<(EquipmentSlot, StatId), ScalingPool> pools, EquipmentSlot slot, StatId stat)
	{
		var key = (slot, stat);
		if (!pools.TryGetValue(key, out var pool))
		{
			pool = new ScalingPool();
			pools[key] = pool;
		}
		return pool;
	}
}