using Godot;
using lethal.common.context;
using lethal.common.context.enums;
using lethal.common.registry;
using lethal.core.domain.stat_identity;
using lethal.core.persistence.generated;
using lethal.gameplay.equipment.enums;
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
	public StatsData? CharacterData { get; set; }
	public string? CharacterId { get; private set; }
	private Dictionary<StatId, float> _baseStats = new();
	private Dictionary<StatId, ResourcePool> _resourcePools = new();
	private readonly ModifierManager _modifierManager = new();
	private readonly Dictionary<StatId, float> _cachedFinalStats = new();
	private StatPipelineFlags _pipelineFlags = StatPipelineFlags.DefaultPlayer;

	private record GearFlatValuesResult(
    	Dictionary<(EquipmentSlot Slot, StatId Stat), float> AggregatedValues,
		Dictionary<(EquipmentSlot Slot, StatId Stat, AffixType Affix), float> AffixValues);
	
	private record EffectivenessValueResolution(
		Dictionary<StatModifier, float> ScaledValues,
		Dictionary<(EquipmentSlot Slot, StatId Stat, AffixType Affix), (float IncreasedMultiplier, float MoreMultiplier)> GearDirectScaledValues,
		Dictionary<(EquipmentSlot Slot, StatId Stat), (float IncreasedMultiplier, float MoreMultiplier)> SlotStatExtraValues);

	private static readonly Dictionary<ThresholdType, (StatId Stat, StatId DefaultSource, float DefaultRatio)> _thresholdConfig = new()
	{
    	[ThresholdType.Stun] = (Stats.StunThreshold, Stats.MaxHealth, 0.5f),
    	[ThresholdType.Affliction] = (Stats.AfflictionThreshold, Stats.MaxHealth, 0.5f),
	};

	private static readonly HashSet<StatId> _attributeSourceStats = new()
	{
    	Stats.Strength, 
		Stats.Agility, 
		Stats.Intelligence
	};

	private double _lastRecalculationTime = 0.0;
	private const double MinRecalculationIntervalSeconds = 0.05;

	private bool _isDirty = true;

	public override void _Ready()
	{
		if (CharacterData != null)
    	{
        	Initialize(CharacterData.CharacterId);
    	}
	}

	public void Initialize(string characterId)
	{
		CharacterId = characterId;
		InitializeBaseStats();
		InitializeResourcePools();
	}

	public void InitializeBaseStats()
	{
		if (CharacterId == null)
    	{
        	GD.PrintErr("[StatsComponent] Warning: InitializeBaseStats called with no CharacterId set!");
        	return;
    	}

    	var characterDefinition = CharacterDefinitionRegistry.Get(CharacterId);
    	if (characterDefinition == null)
    	{
    	    GD.PrintErr($"[StatsComponent] Warning: No character definition found for '{CharacterId}'!");
    	    return;
    	}

		_pipelineFlags = characterDefinition.PipelineFlags;

    	foreach (var def in StatDefinitionRegistry.GetAll())
    	{
        	_baseStats[def.Id] = def.DefaultValue;
    	}

		foreach (var stat in characterDefinition.BaseStats)
    	{
        	_baseStats[stat.Key] = stat.Value;
    	}
	}

	public void InitializeResourcePools()
	{
    	if (CharacterId == null) return;

    	var characterDef = CharacterDefinitionRegistry.Get(CharacterId);
    	if (characterDef == null) return;

    	foreach (var stat in characterDef.StartingResources)
    	{
        	if (!_baseStats.ContainsKey(stat))
        	{
        	    GD.PrintErr($"[StatsComponent] Warning: Base stat for {stat} not found! Please ensure it is defined in BaseStats.");
        	    continue;
        	}
        	var pool = new ResourcePool(stat);
        	pool.MeaningfulChange += () => _isDirty = true;
        	_resourcePools[stat] = pool;
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
		double now = Time.GetTicksMsec() / 1000.0;

		if (_isDirty && (now - _lastRecalculationTime >= MinRecalculationIntervalSeconds))
    	{
        	_RecalculateFinalStats();
        	_isDirty = false;
        	_lastRecalculationTime = now;
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

	public ResourcePool? GetResourcePool(StatId maxStat)
	{
		return _resourcePools.TryGetValue(maxStat, out var pool) ? pool : null;
	}

	internal float? GetResourcePoolPercentage(StatId maxStat)
	{
    	if (_resourcePools == null || !_resourcePools.TryGetValue(maxStat, out var pool))
    	{
        	return null;
    	}

    	if (_cachedFinalStats.TryGetValue(maxStat, out float finalMax) && finalMax > 0f)
    	{
        	return pool.CurrentValue / finalMax;
    	}

    	if (_baseStats.TryGetValue(maxStat, out float baseVal) && baseVal > 0f)
    	{
        	return pool.CurrentValue / baseVal;
    	}

    	return null;
	}

	public void DebugPrintPools()
	{
    	GD.Print($"[StatsComponent] Active Resource Pools: {string.Join(", ", _resourcePools.Keys)}");
	}

	private void _RecalculateFinalStats()
	{
		_cachedFinalStats.Clear();

		var activeModifiers = new List<StatModifier>(_modifierManager.GetAllActiveModifiers());
		var allModifiers = new List<StatModifier>(activeModifiers.Count);

		var persistentContext = new ConditionContext { Source = this, Scope = ConditionScope.Persistent};

		foreach (var modifier in activeModifiers)
		{
    		if (modifier.ConditionScope != ConditionScope.Persistent) continue;
    		if (modifier.Condition != null && !modifier.Condition.Evaluate(persistentContext)) continue;

    		allModifiers.Add(modifier);
		}

		var workingFlatValues = new Dictionary<StatId, float>();
		var workingIncreasedValues = new Dictionary<StatId, float>();
		var workingMoreValues = new Dictionary<StatId, float>();
		var baseOverrides = new Dictionary<StatId, float>();
		var finalOverrides = new Dictionary<StatId, float>();

		var effectivenessValues = _HasFlag(StatPipelineFlags.Effectiveness) ?
			_ResolveEffectivenessModifiers(allModifiers) :
			new EffectivenessValueResolution(new(), new(), new());

		foreach (var pair in _baseStats)
		{
			workingFlatValues[pair.Key] = pair.Value;
			workingIncreasedValues[pair.Key] = 0.0f;
			workingMoreValues[pair.Key] = 1.0f;
		}

		GearFlatValuesResult gearFlatValues;
		if (_HasFlag(StatPipelineFlags.GearFlatValues))
		{
    		gearFlatValues = _CalculateGearFlatValues(allModifiers);
    		foreach (var gearEntry in gearFlatValues.AggregatedValues)
    		{
        		StatId statId = gearEntry.Key.Stat;
        		if (!workingFlatValues.ContainsKey(statId)) workingFlatValues[statId] = 0f;
        		workingFlatValues[statId] += gearEntry.Value;
    		}
    		_ApplyGearDirectScaling(gearFlatValues.AffixValues, effectivenessValues.GearDirectScaledValues, workingFlatValues);
		}
		else
		{
    		gearFlatValues = new GearFlatValuesResult(new(), new());
		}

		if (_HasFlag(StatPipelineFlags.Attributes))
		{
    		var (str, agi, intel) = _ResolveAttributeSourceStats(allModifiers, workingFlatValues, workingIncreasedValues, workingMoreValues, baseOverrides, finalOverrides, effectivenessValues.ScaledValues);
    		var attributeModifiers = AttributeCalculator.GetAttributeDerivedModifiers(str, agi, intel, allModifiers);
    		allModifiers.AddRange(attributeModifiers);
		}

		var globalModifiers = _HasFlag(StatPipelineFlags.GlobalMultipliers) ?
			_GroupModifiers(allModifiers) :
			new Dictionary<StatId, List<StatModifier>>();

		_PopulateGlobalFlatValues(workingFlatValues, globalModifiers, effectivenessValues.ScaledValues, baseOverrides);

		if (_HasFlag(StatPipelineFlags.Conversions)) _ExecuteStatConversions(allModifiers, workingFlatValues, effectivenessValues.ScaledValues);

		if (_HasFlag(StatPipelineFlags.DerivedStats)) _ExecutePreMultiplierDerived(allModifiers, workingFlatValues, effectivenessValues.ScaledValues);

		_PopulateWorkingMultipliers(workingIncreasedValues, workingMoreValues, finalOverrides, globalModifiers, effectivenessValues.ScaledValues);

		_CalculatePrimaryStats(workingFlatValues, workingIncreasedValues, workingMoreValues, baseOverrides, effectivenessValues.SlotStatExtraValues, gearFlatValues.AggregatedValues);

    	if (_HasFlag(StatPipelineFlags.DerivedStats)) _ExecutePostMultiplierDerivedAndFinalize(allModifiers, workingFlatValues, workingIncreasedValues, workingMoreValues, gearFlatValues.AggregatedValues, effectivenessValues.SlotStatExtraValues, effectivenessValues.ScaledValues, baseOverrides);

		_ApplyFinalOverrides(finalOverrides);

		_ResolveThresholds(allModifiers, workingIncreasedValues, workingMoreValues, finalOverrides);
	}

	private GearFlatValuesResult _CalculateGearFlatValues(IEnumerable<StatModifier> allActiveModifiers)
	{
		var aggregatedValues = new Dictionary<(EquipmentSlot Slot, StatId Stat), float>();
		var byAffixValues = new Dictionary<(EquipmentSlot Slot, StatId Stat, AffixType Affix), float>();

		var groupedEquipmentModifiers = new Dictionary<(EquipmentSlot Slot, StatId Stat), List<StatModifier>>();
		
		foreach (var modifier in allActiveModifiers)
		{
			bool isGearSourced = modifier.Source.Category == ModifierSourceCategory.Equipment || modifier.Source.Category == ModifierSourceCategory.Augment;

			if (!isGearSourced || modifier is DerivedStatModifier || modifier is StatConversionModifier) continue;

			foreach (var statId in modifier.AffectedStats)
			{
				var key = (modifier.Source.OriginSlot, statId);

				if (!groupedEquipmentModifiers.TryGetValue(key, out var list))
				{
					list = new List<StatModifier>();
					groupedEquipmentModifiers[key] = list;
				}

				list.Add(modifier);
			}
		}

		foreach (var group in groupedEquipmentModifiers)
		{
			EquipmentSlot slot = group.Key.Slot;
			StatId statId = group.Key.Stat;

			float localIncreasedMultiplier = 0.0f;
			float localMoreMultiplier = 1.0f;

			var flatValueByAffix = new Dictionary<AffixType, float>();

			foreach (var affix in group.Value)
        	{
            	float rawValue = affix.GetScalar();

            	switch (affix.Type)
            	{
                	case ModifierType.Flat:
                    	flatValueByAffix.TryGetValue(affix.AffixType, out var existingFlat);
                    	flatValueByAffix[affix.AffixType] = existingFlat + rawValue;
                    	break;
                	case ModifierType.Increased:
                    	localIncreasedMultiplier += rawValue;
                    	break;
                	case ModifierType.More:
                    	localMoreMultiplier *= 1.0f + rawValue;
                    	break;
					case ModifierType.BaseOverride:
    				case ModifierType.FinalOverride:
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

        	float itemFactor = (1.0f + localIncreasedMultiplier) * localMoreMultiplier;
        	float slotStatTotal = 0.0f;

        	foreach (var (affixType, rawFlat) in flatValueByAffix)
        	{
            	float scaledFlat = rawFlat * itemFactor;
            	byAffixValues[(slot, statId, affixType)] = scaledFlat;
            	slotStatTotal += scaledFlat;
        	}

        	aggregatedValues[(slot, statId)] = slotStatTotal;
    	}

    	return new GearFlatValuesResult(aggregatedValues, byAffixValues);
	}

	private void _ApplyGearDirectScaling(
    	Dictionary<(EquipmentSlot Slot, StatId Stat, AffixType Affix), float> byAffixValues,
    	Dictionary<(EquipmentSlot Slot, StatId Stat, AffixType Affix), (float IncreasedMultiplier, float MoreMultiplier)> gearDirectScaledValues,
    	Dictionary<StatId, float> workingFlatValues)
	{
		foreach (var byAffixEntry in byAffixValues)
		{
			var (slot, statId, affixType) = byAffixEntry.Key;
			float rawSubtotalValue = byAffixEntry.Value;

			if (!gearDirectScaledValues.TryGetValue((slot, statId, affixType), out var bonus)) continue;

			float scaledSubtotalValue = rawSubtotalValue * (1.0f + bonus.IncreasedMultiplier) * bonus.MoreMultiplier;
			float delta = scaledSubtotalValue - rawSubtotalValue;

			if (!workingFlatValues.ContainsKey(statId)) workingFlatValues[statId] = 0f;
			workingFlatValues[statId] += delta;
		}
	}

	private (float Strength, float Agility, float Intelligence) _ResolveAttributeSourceStats(
    	IEnumerable<StatModifier> allModifiers,
    	Dictionary<StatId, float> workingFlatValues,
    	Dictionary<StatId, float> workingIncreasedValues,
    	Dictionary<StatId, float> workingMoreValues,
    	Dictionary<StatId, float> baseOverrides,
		Dictionary<StatId, float> finalOverrides,
    	Dictionary<StatModifier, float> scaledValues)
	{
    	foreach (var modifier in allModifiers)
    	{
        	if (modifier.Slot != EquipmentSlot.None) continue;
        	if (modifier is EffectivenessModifier || modifier is AttributeOverrideStatModifier) continue;

			if (modifier is StatConversionModifier conversionModifier)
    		{
        		if (conversionModifier.TargetSplitValues == null || conversionModifier.TargetSplitValues.Count == 0) continue;

        		bool targetsAnyAttribute = conversionModifier.TargetSplitValues.Keys.Any(s => _attributeSourceStats.Contains(s));
        		if (!targetsAnyAttribute) continue;

        		bool targetsAllAttributes = conversionModifier.TargetSplitValues.Keys.All(s => _attributeSourceStats.Contains(s));
        		if (!targetsAllAttributes)
        		{
            		string errorMsg = $"[StatsComponent] Error: StatConversionModifier '{conversionModifier.Source}' splits between attribute and non-attribute stats — not supported!";
            		#if DEBUG
            		throw new InvalidOperationException(errorMsg);
            		#else
            		GD.PrintErr(errorMsg);
					continue;
            		#endif
        		}

        		StatId sourceStat = conversionModifier.SourceStat;
        		if (!workingFlatValues.TryGetValue(sourceStat, out float sourceValue) || sourceValue <= 0f) continue;

        		float effectiveRatio = scaledValues.TryGetValue(conversionModifier, out var scaledConversion) ? scaledConversion : conversionModifier.GetScalar();
        		float totalConvertedAmount = sourceValue * effectiveRatio;

        		foreach (var split in conversionModifier.TargetSplitValues)
        		{
            		float finalConversionValue = totalConvertedAmount * split.Value;
            		if (finalConversionValue > 0f)
            		{
                		workingFlatValues[split.Key] = workingFlatValues.GetValueOrDefault(split.Key, _baseStats.GetValueOrDefault(split.Key, 0f)) + finalConversionValue;
            		}
        		}

        		continue;
    		}

        	foreach (var targetStat in modifier.AffectedStats)
        	{
            	if (!_attributeSourceStats.Contains(targetStat)) continue;

				if (modifier is DerivedStatModifier derivedModifier)
        		{
            		float sourceValue = workingFlatValues.GetValueOrDefault(derivedModifier.SourceStat, 0f);
            		float ratio = scaledValues.TryGetValue(derivedModifier, out var scaledDerivation) ? scaledDerivation : derivedModifier.GetScalar();
            		workingFlatValues[targetStat] = workingFlatValues.GetValueOrDefault(targetStat, _baseStats.GetValueOrDefault(targetStat, 0f)) + sourceValue * ratio;
            		continue;
        		}

            	float value = scaledValues.TryGetValue(modifier, out var scaled) ? scaled : modifier.GetValue();

            	switch (modifier.Type)
            	{
                	case ModifierType.Flat:
                    	workingFlatValues[targetStat] = workingFlatValues.GetValueOrDefault(targetStat, _baseStats.GetValueOrDefault(targetStat, 0f)) + value;
                    	break;
                	case ModifierType.Increased:
                    	workingIncreasedValues[targetStat] = workingIncreasedValues.GetValueOrDefault(targetStat, 0f) + value;
                    	break;
                	case ModifierType.More:
                    	workingMoreValues[targetStat] = workingMoreValues.GetValueOrDefault(targetStat, 1f) * (1.0f + value);
                    	break;
                	case ModifierType.BaseOverride:
                    	baseOverrides[targetStat] = value;
                    	break;
					case ModifierType.FinalOverride:
                    	if (finalOverrides.ContainsKey(targetStat))
                    	{
                        	string warning = $"[StatsComponent] Warning: Multiple FinalOverrides found for {targetStat}! '{modifier.Source}' is overriding existing final value.";
                        	#if DEBUG
                        	throw new InvalidOperationException(warning);
                        	#else
                        	GD.PrintErr(warning);
                        	#endif
                    	}
                    	finalOverrides[targetStat] = value;
                    	break;
            	}
        	}
    	}

    	float Resolve(StatId stat)
    	{
			if (finalOverrides.TryGetValue(stat, out var overrideVal)) return overrideVal;

        	float defaultBase = _baseStats.GetValueOrDefault(stat, 0f);
        	float effectiveBase = baseOverrides.GetValueOrDefault(stat, defaultBase);
        	float flat = workingFlatValues.GetValueOrDefault(stat, defaultBase) - defaultBase;
        	float increased = workingIncreasedValues.GetValueOrDefault(stat, 0f);
        	float more = workingMoreValues.GetValueOrDefault(stat, 1f);
        	return (effectiveBase + flat) * (1.0f + increased) * more;
    	}

    	return (Resolve(Stats.Strength), Resolve(Stats.Agility), Resolve(Stats.Intelligence));
	}

	private void _PopulateGlobalFlatValues(
		Dictionary<StatId, float> workingFlatValues,
		Dictionary<StatId, List<StatModifier>> globalModifiers,
		Dictionary<StatModifier, float> scaledValues,
		Dictionary<StatId, float> baseOverrides)
	{
		foreach (var pair in globalModifiers)
		{
			StatId statId = pair.Key;
			var modifiers = pair.Value;

			foreach (var modifier in modifiers)
			{
				if (modifier.Type != ModifierType.Flat && modifier.Type != ModifierType.BaseOverride) continue;

				float value = scaledValues.TryGetValue(modifier, out var scaled) ? scaled : modifier.GetValue();

				switch (modifier.Type)
				{
					case ModifierType.Flat:
    					if (!workingFlatValues.ContainsKey(statId)) workingFlatValues[statId] = _baseStats.GetValueOrDefault(statId, 0f);
    					workingFlatValues[statId] += value;
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
						baseOverrides[statId] = value;
						break;
				}
			}
		}
	}

	private void _ExecuteStatConversions(
		IEnumerable<StatModifier> allModifiers, 
		Dictionary<StatId, float> workingFlatValues,
		Dictionary<StatModifier, float> scaledValues)
	{
		var targetAdditions = new Dictionary<StatId, float>();
		var siphonedAmounts = new Dictionary<StatId, float>();

		foreach (var modifier in allModifiers)
		{
			if (modifier is StatConversionModifier conversionModifier && conversionModifier.TargetSplitValues != null && conversionModifier.TargetSplitValues.Count > 0)
			{
				bool targetsAnyAttribute = conversionModifier.TargetSplitValues.Keys.Any(s => _attributeSourceStats.Contains(s));
        		if (targetsAnyAttribute) continue;

				StatId sourceStat = conversionModifier.SourceStat;

				if (!workingFlatValues.TryGetValue(sourceStat, out float sourceValue) || sourceValue <= 0.0f) continue;

				float effectiveRatio = scaledValues.TryGetValue(conversionModifier, out var scaled) ? scaled : conversionModifier.GetScalar();

				float alreadySiphonedAmount = siphonedAmounts.GetValueOrDefault(sourceStat, 0.0f);
				float availableSourceValue = sourceValue - alreadySiphonedAmount;

				if (availableSourceValue <= 0.0f) continue;

				float totalConvertedAmount = availableSourceValue * effectiveRatio;
				siphonedAmounts[sourceStat] = alreadySiphonedAmount + totalConvertedAmount;

				foreach (var split in conversionModifier.TargetSplitValues)
				{
					StatId targetStat = split.Key;
					float splitWeight = split.Value;
					float finalConversionValue = totalConvertedAmount * splitWeight;

					if (finalConversionValue > 0.0f)
					{
						float currentTargetAdd = targetAdditions.GetValueOrDefault(targetStat, 0.0f);
						targetAdditions[targetStat] = currentTargetAdd + finalConversionValue;
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
		
		foreach (var addition in targetAdditions)
		{
			StatId targetStat = addition.Key;
			float addedValue = addition.Value;

			if (!workingFlatValues.ContainsKey(targetStat))
			{
				workingFlatValues[targetStat] = _baseStats.GetValueOrDefault(targetStat, 0f);
			}
			workingFlatValues[targetStat] += addedValue;
		}
	}

	private void _ExecutePreMultiplierDerived(
		IEnumerable<StatModifier> allModifiers, 
		Dictionary<StatId, float> workingFlatValues,
		Dictionary<StatModifier, float> scaledValues)
	{
		foreach (var modifier in allModifiers)
		{
			if (modifier is DerivedStatModifier derivedModifier && derivedModifier.Phase == ModifierExecutionPhase.PreMultipliers)
			{
				bool targetsAttribute = derivedModifier.AffectedStats.Any(s => _attributeSourceStats.Contains(s));
            	if (targetsAttribute) continue;

				if (workingFlatValues.TryGetValue(derivedModifier.SourceStat, out float sourceValue))
				{
					float effectiveRatio = scaledValues.TryGetValue(derivedModifier, out var scaled) ? scaled : derivedModifier.GetScalar();
					float derivedValue = sourceValue * effectiveRatio;

					foreach (var targetStat in derivedModifier.AffectedStats)
					{
						if (!workingFlatValues.ContainsKey(targetStat)) workingFlatValues[targetStat] = 0.0f;

						workingFlatValues[targetStat] += derivedValue;
					}
				}
			}
		}
	}

	private EffectivenessValueResolution _ResolveEffectivenessModifiers(IEnumerable<StatModifier> allModifiers)
	{
		var effectivenessModifiers = allModifiers.OfType<EffectivenessModifier>().ToList();
		var scaledValues = new Dictionary<StatModifier, float>();
		var gearDirectScaledValues = new Dictionary<(EquipmentSlot, StatId, AffixType), (float IncreasedMultiplier, float MoreMultiplier)>();
		var slotStatExtraValues = new Dictionary<(EquipmentSlot, StatId), (float IncreasedMultiplier, float MoreMultiplier)>();

		if (effectivenessModifiers.Count == 0)
    	{
        	return new EffectivenessValueResolution(scaledValues, gearDirectScaledValues, slotStatExtraValues);
    	}

		foreach (var modifier in allModifiers)
		{
			if (modifier is EffectivenessModifier || modifier is AttributeOverrideStatModifier) continue;

			bool isGearFlatValueEligible = (modifier.Source.Category == ModifierSourceCategory.Equipment || modifier.Source.Category == ModifierSourceCategory.Augment)
				&& modifier is not DerivedStatModifier
				&& modifier is not StatConversionModifier;

			float directIncreased = 0.0f, directMore = 1.0f;
			bool hasDirectMatch = false;
			float pooledIncreased = 0.0f, pooledMore = 1.0f;
			bool hasPooledMatch = false;

			foreach (var effectiveness in effectivenessModifiers)
			{
				if (!effectiveness.Matches(modifier)) continue;

				if (effectiveness.EffectivenessApplication == EffectivenessApplication.DirectScale)
				{
					hasDirectMatch = true;
					if (effectiveness.Type == ModifierType.Increased) directIncreased += effectiveness.Value;
					else if (effectiveness.Type == ModifierType.More) directMore *= 1.0f + effectiveness.Value;
				}
				else
				{
					hasPooledMatch = true;
					if (effectiveness.Type == ModifierType.Increased) pooledIncreased += effectiveness.Value;
					else if (effectiveness.Type == ModifierType.More) pooledMore *= 1.0f + effectiveness.Value;
				}
			}

			if (hasDirectMatch)
			{
				if (isGearFlatValueEligible)
				{
					foreach (var stat in modifier.AffectedStats)
					{
						var key = (modifier.Slot, stat, modifier.AffixType);

						if (!gearDirectScaledValues.ContainsKey(key)) gearDirectScaledValues[key] = (0.0f, 1.0f);

						var existing = gearDirectScaledValues[key];
						gearDirectScaledValues[key] = (existing.IncreasedMultiplier + directIncreased, existing.MoreMultiplier * directMore);
					}
				}
				else
				{
					scaledValues[modifier] = modifier.GetScalar() * (1.0f + directIncreased) * directMore;
				}
			}

			if (hasPooledMatch && modifier.Slot != EquipmentSlot.None)
			{
				foreach (var stat in modifier.AffectedStats)
				{
					var key = (modifier.Slot, stat);
					
					if (!slotStatExtraValues.ContainsKey(key)) slotStatExtraValues[key] = (0.0f, 1.0f);

					var existing = slotStatExtraValues[key];
					slotStatExtraValues[key] = (existing.IncreasedMultiplier + pooledIncreased, existing.MoreMultiplier * pooledMore);
				}
			}
		}

		return new EffectivenessValueResolution(scaledValues, gearDirectScaledValues, slotStatExtraValues);
	}

	private Dictionary<StatId, List<StatModifier>> _GroupModifiers(IEnumerable<StatModifier> modifiers)
	{
		var globalModifierByStat = new Dictionary<StatId, List<StatModifier>>();

		foreach (var modifier in modifiers)
		{
			if (modifier.AffectedStats.Count == 0 || modifier.Slot != EquipmentSlot.None) continue;

			foreach (var stat in modifier.AffectedStats)
			{
				_AddModifierToStatList(globalModifierByStat, stat, modifier);
			}
		}

		return globalModifierByStat;
	}

	private void _PopulateWorkingMultipliers(
		Dictionary<StatId, float> workingIncreasedValues,
		Dictionary<StatId, float> workingMoreValues,
		Dictionary<StatId, float> finalOverrides,
		Dictionary<StatId, List<StatModifier>> globalModifiers,
		Dictionary<StatModifier, float> scaledValues)
	{
		foreach (var pair in globalModifiers)
		{
			StatId statId = pair.Key;

			if (_attributeSourceStats.Contains(statId)) continue;

			var modifiers = pair.Value;

			foreach (var modifier in modifiers)
			{
				if (modifier.Type != ModifierType.Increased && modifier.Type != ModifierType.More && modifier.Type != ModifierType.FinalOverride) continue;
				if (_attributeSourceStats.Contains(statId)) continue;

				float value = scaledValues.TryGetValue(modifier, out var scaled) ? scaled : modifier.GetValue();

				switch (modifier.Type)
				{
					case ModifierType.Increased:
						if (!workingIncreasedValues.ContainsKey(statId)) workingIncreasedValues[statId] = 0.0f;
						workingIncreasedValues[statId] += value;
						break;
					case ModifierType.More:
						if (!workingMoreValues.ContainsKey(statId)) workingMoreValues[statId] = 1.0f;
						workingMoreValues[statId] += value;
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
						finalOverrides[statId] = value;
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
		Dictionary<(EquipmentSlot Slot, StatId Stat), (float IncreasedMultiplier, float MoreMultiplier)> slotStatExtraValues,
		Dictionary<(EquipmentSlot Slot, StatId Stat), float> resolvedGearFlatValues)
	{
		var gearFlatValuesByStat = new Dictionary<StatId, List<(EquipmentSlot Slot, float Value)>>();

		foreach (var gearEntry in resolvedGearFlatValues)
		{
			var stat = gearEntry.Key.Stat;

			if (!gearFlatValuesByStat.TryGetValue(stat, out var list))
			{
				list = new List<(EquipmentSlot, float)>();
				gearFlatValuesByStat[stat] = list;
			}

			list.Add((gearEntry.Key.Slot, gearEntry.Value));
		}

		foreach (var pair in _baseStats)
		{
			var statId = pair.Key;
			float defaultBaseValue = pair.Value;

			float effectiveBaseValues = baseOverrides.GetValueOrDefault(statId, defaultBaseValue);
			float globalIncreasedMultipliers = workingIncreasedValues.GetValueOrDefault(statId, 0.0f);
			float globalMoreMultipliers = workingMoreValues.GetValueOrDefault(statId, 1.0f);

			float genericFlatValues = workingFlatValues.GetValueOrDefault(statId, defaultBaseValue) - defaultBaseValue;
			float slotScopedTotalValues = 0.0f;

			if (gearFlatValuesByStat.TryGetValue(statId, out var contributingSlotValues))
			{
				foreach (var (slot, rawSlotValue) in contributingSlotValues)
				{
					if (slotStatExtraValues.TryGetValue((slot, statId), out var extra))
					{
						float slotContribution = rawSlotValue * (1.0f + globalIncreasedMultipliers + extra.IncreasedMultiplier) * (globalMoreMultipliers * extra.MoreMultiplier);
						slotScopedTotalValues += slotContribution;
						genericFlatValues -= rawSlotValue;
					}
				}
			}

			float genericContribution = (effectiveBaseValues + genericFlatValues) * (1.0f + globalIncreasedMultipliers) * globalMoreMultipliers;
        	float standardCalculation = genericContribution + slotScopedTotalValues;

        	float finalValue = standardCalculation;

			_cachedFinalStats[statId] = finalValue;
		}
	}

	private void _ExecutePostMultiplierDerivedAndFinalize(
		IEnumerable<StatModifier> allModifiers,
		Dictionary<StatId, float> workingFlatValues,
		Dictionary<StatId, float> workingIncreasedValues,
		Dictionary<StatId, float> workingMoreValues,
		Dictionary<(EquipmentSlot Slot, StatId Stat), float> resolvedGearFlatValues,
		Dictionary<(EquipmentSlot Slot, StatId Stat), (float IncreasedMultiplier, float MoreMultiplier)> slotStatExtraValues,
		Dictionary<StatModifier, float> scaledValues,
		Dictionary<StatId, float> baseOverrides)
	{
		foreach (var modifier in allModifiers)
		{
			if (modifier.Slot != EquipmentSlot.None) continue;

    		if (modifier is DerivedStatModifier derived && derived.Phase == ModifierExecutionPhase.PostMultipliers)
    		{
        		bool targetsAttribute = derived.AffectedStats.Any(s => _attributeSourceStats.Contains(s));
        		if (targetsAttribute)
        		{
            		string errorMsg = $"[StatsComponent] Error: A PostMultipliers-phase DerivedStatModifier cannot target an attribute source stat (Strength/Agility/Intelligence) — its source data isn't resolved yet at attribute-resolution time!";
            		#if DEBUG
            		throw new InvalidOperationException(errorMsg);
            		#else
            		GD.PrintErr(errorMsg);
            		continue;
            		#endif
        		}
    		}

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

				float effectiveRatio = scaledValues.TryGetValue(derivedModifier, out var scaled) ? scaled : derivedModifier.GetScalar();
				float derivedFlatValue = sourceValue * effectiveRatio;

				foreach (var targetStat in derivedModifier.AffectedStats)
				{
					float defaultBaseValue = _baseStats.GetValueOrDefault(targetStat, 0.0f);
					float effectiveBaseValue = baseOverrides.GetValueOrDefault(targetStat, defaultBaseValue);

					float existingFlatValues = workingFlatValues.GetValueOrDefault(targetStat, defaultBaseValue) - defaultBaseValue;
					float totalFlatValues = existingFlatValues + derivedFlatValue;

					float increasedMultiplier = workingIncreasedValues.GetValueOrDefault(targetStat, 0.0f);
					float moreMultiplier = workingMoreValues.GetValueOrDefault(targetStat, 1.0f);

					if (derivedModifier.Slot != EquipmentSlot.None && slotStatExtraValues.TryGetValue((derivedModifier.Slot, targetStat), out var extra))
                	{
                    	increasedMultiplier += extra.IncreasedMultiplier;
                    	moreMultiplier *= extra.MoreMultiplier;
                	}

					float finalDerivedStatValue = (effectiveBaseValue + totalFlatValues) * (1.0f + increasedMultiplier) * moreMultiplier;
					_cachedFinalStats[targetStat] = finalDerivedStatValue;
				}
			}
		}
	}

	private void _ApplyFinalOverrides(Dictionary<StatId, float> finalOverrides)
	{
		foreach (var (statId, value) in finalOverrides)
		{
			_cachedFinalStats[statId] = value;
		}
	}

	private void _ResolveThresholds(
    	IEnumerable<StatModifier> allModifiers,
    	Dictionary<StatId, float> workingIncreasedValues,
    	Dictionary<StatId, float> workingMoreValues,
    	Dictionary<StatId, float> finalOverrides)
	{
		var overridesByTarget = new Dictionary<ThresholdType, ThresholdOverrideDerivedStatModifier>();

		foreach (var modifier in allModifiers)
		{
			if (modifier is ThresholdOverrideDerivedStatModifier thresholdOverride)
			{
				if (overridesByTarget.ContainsKey(thresholdOverride.TargetThreshold))
				{
					string warning = $"[StatsComponent] Warning: Multiple ThresholdOverrides found for {thresholdOverride.TargetThreshold}!";
                	#if DEBUG
                	throw new InvalidOperationException(warning);
                	#else
                	GD.PrintErr(warning);
                	#endif
				}

				overridesByTarget[thresholdOverride.TargetThreshold] = thresholdOverride;
			}
		}

		foreach (var (thresholdType, config) in _thresholdConfig)
		{
			float computedBaseValue;

			if (overridesByTarget.TryGetValue(thresholdType, out var overrideModifier))
			{
				computedBaseValue = 0.0f;

				foreach (var (sourceStat, ratio) in overrideModifier.SourceStatRatios)
				{
					computedBaseValue += _cachedFinalStats.GetValueOrDefault(sourceStat, 0.0f) * ratio;
				}
			}
			else
			{
				computedBaseValue = _cachedFinalStats.GetValueOrDefault(config.DefaultSource, 0.0f) * config.DefaultRatio;
			}

			float increasedMultiplier = workingIncreasedValues.GetValueOrDefault(config.Stat, 0.0f);
			float moreMultiplier = workingMoreValues.GetValueOrDefault(config.Stat, 1.0f);
			float scaledBaseValue = computedBaseValue * (1.0f + increasedMultiplier) * moreMultiplier;

			float existingContributionValue = _cachedFinalStats.GetValueOrDefault(config.Stat, 0.0f);
			_cachedFinalStats[config.Stat] = scaledBaseValue + existingContributionValue;

			if (finalOverrides.TryGetValue(config.Stat, out var overrideValue))
			{
				_cachedFinalStats[config.Stat] = overrideValue;
			}
		}
	}

	private void _AddModifierToStatList(Dictionary<StatId, List<StatModifier>> modifierByStat, StatId statId, StatModifier modifier)
	{
		if (!modifierByStat.ContainsKey(statId)) modifierByStat[statId] = new List<StatModifier>();
		modifierByStat[statId].Add(modifier);
	}

	private bool _HasFlag(StatPipelineFlags flag) => (_pipelineFlags & flag) != 0;
}