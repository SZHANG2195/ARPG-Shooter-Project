using Godot;
using lethal.common.registry;
using lethal.gameplay.stats.enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace lethal.gameplay.stats.modifiers;
public abstract class StatModifier
{
	public ModifierType Type;
	public ModifierSource Source;
	public EquipmentSlot Slot = EquipmentSlot.None;
	public AffixType AffixType = AffixType.None;
	public ModifierScope Scope = ModifierScope.Stat;
	public string ConditionKey { get; set; } = string.Empty;
	public List<StatType> AffectedStats { get; set; } = new();


	public virtual bool isExpired => false;
	public virtual void Tick(float delta) {}
	
	public abstract float GetValue();

	public static StaticStatModifier CreateSingleStaticModifier(
		ModifierType type, 
		float value, 
		StatType stat = StatType.None, 
		ModifierSource source = default, 
		EquipmentSlot slot = EquipmentSlot.None, 
		AffixType affixType = AffixType.None, 
		ModifierScope scope = ModifierScope.Stat, 
		string conditionKey = "")
	{
        var modifier = new StaticStatModifier
        {
			//Modifier defaults
			Type = type,
			Source = source,
			Slot = slot,
			AffixType = affixType,
			Scope = scope,
			ConditionKey = conditionKey,

			//Class specific
			Value = value,
			
		};

		if (stat == StatType.None)
		{
			GD.PrintErr($"[Modifiers] Warning: Created single static modifier of type {type} with StatType.None!");
		}

		modifier.AffectedStats.Add(stat);
		return modifier;
	}

	public static StaticStatModifier CreateTaggedStaticModifier(
		ModifierType type, 
		float value, 
		StringName tag, 
		ModifierSource source, 
		EquipmentSlot slot = EquipmentSlot.None, 
		AffixType affixType = AffixType.None, 
		ModifierScope scope = ModifierScope.Stat, 
		string conditionKey = "")
	{
		var modifier = new StaticStatModifier
		{
			Type = type,
			Source = source,
			Slot = slot,
			AffixType = affixType,
			Scope = scope,
			ConditionKey = conditionKey,

			Value = value,
		};

		if (tag == null || tag.IsEmpty || tag == "none")
    	{
        	GD.PrintErr($"[Modifiers] Warning: Created a tagged static modifier with an invalid or 'none' tag of type {type}!");
        	return modifier;
    	}

		modifier.AffectedStats.AddRange(TagRegistry.GetStatsByTag(tag));
		return modifier;
	}

	public static DerivedStatModifier CreateSingleDerivedModifier(
		ModifierType type, 
		float ratio, 
		StatType sourceStat, 
		StatType targetStat, 
		ModifierExecutionPhase phase, 
		ModifierSource source, 
		EquipmentSlot slot = EquipmentSlot.None, 
		AffixType affixType = AffixType.None,
		ModifierScope scope = ModifierScope.Stat, 
		string conditionKey = "")
	{
		var modifier = new DerivedStatModifier
		{
			Type = type,
			Source = source,
			Slot = slot,
			AffixType = affixType,
			Scope = scope,
			ConditionKey = conditionKey,

			SourceStat = sourceStat,
			Ratio = ratio,
			Phase = phase
		};

		if (sourceStat == StatType.None || targetStat == StatType.None)
		{
			GD.PrintErr($"[Modifiers] Warning: Created single derived modifier of type {type} with StatType.None!");
		}

		modifier.AffectedStats.Add(targetStat);
		return modifier;
	}

	public static DerivedStatModifier CreateTaggedDerivedModifier(
		ModifierType type, 
		float ratio, 
		StatType sourceStat, 
		StringName tag,
		ModifierExecutionPhase phase, 
		ModifierSource source, 
		EquipmentSlot slot = EquipmentSlot.None, 
		AffixType affixType = AffixType.None,
		ModifierScope scope = ModifierScope.Stat, 
		string conditionKey = "")
	{
		var modifier = new DerivedStatModifier
		{
			Type = type,
			Source = source,
			Slot = slot,
			AffixType = affixType,
			Scope = scope,
			ConditionKey = conditionKey,

			SourceStat = sourceStat,
			Ratio = ratio,
			Phase = phase
		};

		if (sourceStat == StatType.None || tag == null || tag.IsEmpty || tag == "none" )
		{
			GD.PrintErr($"[Modifiers] Warning: Created a tagged derived modifier of type {type} with StatType.None!");
		}

		modifier.AffectedStats.AddRange(TagRegistry.GetStatsByTag(tag));
		return modifier;
	}

	public static StatConversionModifier CreateSingleConversionModifier(
		ModifierType type, 
		float ratio, 
		StatType sourceStat, 
		StatType targetStat, 
		ModifierSource source, 
		EquipmentSlot slot = EquipmentSlot.None, 
		AffixType affixType = AffixType.None,
		ModifierScope scope = ModifierScope.Stat, 
		string conditionKey = "")
	{
		var modifier = new StatConversionModifier
		{
			Type = type,
			Source = source,
			Slot = slot,
			AffixType = affixType,
			Scope = scope,
			ConditionKey = conditionKey,

			SourceStat = sourceStat,
			Ratio = ratio,
			TargetSplitValues = new Dictionary<StatType, float>
			{
				{ targetStat, 1.0f }
			}
		};

		if (sourceStat == StatType.None || targetStat == StatType.None)
		{
			GD.PrintErr($"[Modifiers] Warning: Created single conversion modifier of type {type} with StatType.None!");
		}

		return modifier;
	}

	public static StatConversionModifier CreateTaggedConversionModifier(
		ModifierType type, 
		float ratio, 
		StatType sourceStat, 
		StringName tag,
		ModifierSource source, 
		EquipmentSlot slot = EquipmentSlot.None, 
		AffixType affixType = AffixType.None,
		ModifierScope scope = ModifierScope.Stat, 
		string conditionKey = "",
		Dictionary<StatType, float> manualSplitValues = null)
	{
		var modifier = new StatConversionModifier
		{
			Type = type,
			Source = source,
			Slot = slot,
			AffixType = affixType,
			Scope = scope,
			ConditionKey = conditionKey,

			SourceStat = sourceStat,
			Ratio = ratio
		};

		if (sourceStat == StatType.None || tag == null || tag.IsEmpty || tag == "none" )
		{
			GD.PrintErr($"[Modifiers] Warning: Created a tagged conversion modifier of type {type} with StatType.None!");
		}

		var resolvedStats = TagRegistry.GetStatsByTag(tag);
		if (resolvedStats.Count() == 0)
    	{
        	GD.PrintErr($"[Modifiers] Warning: Tagged conversion modifier found no stats for tag '{tag}'!");
        	return modifier;
    	}

		Dictionary<StatType, float> finalSplitValues;

		if (manualSplitValues != null && manualSplitValues.Count > 0)
		{
			float totalWeight = 0f;

			foreach (var kvp in manualSplitValues)
			{
				if (!resolvedStats.Contains(kvp.Key))
				{
					string errorMsg = $"[Modifiers] Error: Manual split key '{kvp.Key}' does not match any stats registered under tag '{tag}'!";
					#if DEBUG
					throw new InvalidOperationException(errorMsg);
					#else
					GD.PrintErr(errorMsg);
					#endif
				}
				
				totalWeight += kvp.Value;
			}

			if (Math.Abs(totalWeight - 1.0f) > 0.001f)
			{
				string errorMsg = $"[Modifiers] Error: Manual target splits for tag '{tag}' sum to '{totalWeight}' but must equal 1.0f!";
				#if DEBUG
				throw new InvalidOperationException(errorMsg);
				#else
				GD.PrintErr(errorMsg);
				#endif
			}

			finalSplitValues = manualSplitValues;
		}
		else
		{
			float splitWeightPerStat = 1.0f / resolvedStats.Count();
			finalSplitValues = new Dictionary<StatType, float>();

			foreach (var stat in resolvedStats)
			{
				finalSplitValues[stat] = splitWeightPerStat;
			}
		}

		modifier.TargetSplitValues = finalSplitValues;
		return modifier;
	}
}
