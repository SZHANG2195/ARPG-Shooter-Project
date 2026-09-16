using Godot;
using lethal.common.registry;
using lethal.core.domain.stat_identity;
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
	public List<StatId> AffectedStats { get; set; } = new();


	public virtual bool isExpired => false;
	public virtual void Tick(float delta) {}
	
	public abstract float GetValue();

	public virtual float GetScalar() => GetValue();

	public static StaticStatModifier? CreateSingleStaticModifier(
		ModifierType type, 
		float value, 
		StatId? stat = null, 
		ModifierSource source = default, 
		EquipmentSlot slot = EquipmentSlot.None, 
		AffixType affixType = AffixType.None, 
		ModifierScope scope = ModifierScope.Stat, 
		string conditionKey = "")
	{
		if (stat is null)
		{
			GD.PrintErr($"[Modifiers] Warning: Attempted to create a single static modifier of type {type} with no stat type!");
			return null;
		}

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

		modifier.AffectedStats.Add(stat.Value);
		return modifier;
	}

	public static StaticStatModifier? CreateTaggedStaticModifier(
		ModifierType type, 
		float value, 
		StringName tag, 
		ModifierSource source, 
		EquipmentSlot slot = EquipmentSlot.None, 
		AffixType affixType = AffixType.None, 
		ModifierScope scope = ModifierScope.Stat, 
		string conditionKey = "")
	{
		if (tag == null || tag.IsEmpty || tag == "none")
    	{
        	GD.PrintErr($"[Modifiers] Warning: Attempted to create a tagged static modifier with an invalid or 'none' tag of type {type}!");
        	return null;
    	}

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

		modifier.AffectedStats.AddRange(StatDefinitionRegistry.GetByTag(tag));
		return modifier;
	}

	public static DerivedStatModifier? CreateSingleDerivedModifier(
		ModifierType type, 
		float ratio, 
		StatId? sourceStat, 
		StatId? targetStat, 
		ModifierExecutionPhase phase, 
		ModifierSource source, 
		EquipmentSlot slot = EquipmentSlot.None, 
		AffixType affixType = AffixType.None,
		ModifierScope scope = ModifierScope.Stat, 
		string conditionKey = "")
	{
		if (sourceStat is null || targetStat is null)
		{
			GD.PrintErr($"[Modifiers] Warning: Attempted to create a single derived modifier of type {type} with no source or target stat!");
			return null;
		}

		var modifier = new DerivedStatModifier
		{
			Type = type,
			Source = source,
			Slot = slot,
			AffixType = affixType,
			Scope = scope,
			ConditionKey = conditionKey,

			SourceStat = sourceStat.Value,
			Ratio = ratio,
			Phase = phase
		};

		modifier.AffectedStats.Add(targetStat.Value);
		return modifier;
	}

	public static DerivedStatModifier? CreateTaggedDerivedModifier(
		ModifierType type, 
		float ratio, 
		StatId? sourceStat, 
		StringName tag,
		ModifierExecutionPhase phase, 
		ModifierSource source, 
		EquipmentSlot slot = EquipmentSlot.None, 
		AffixType affixType = AffixType.None,
		ModifierScope scope = ModifierScope.Stat, 
		string conditionKey = "")
	{
		if (sourceStat is null || tag == null || tag.IsEmpty || tag == "none")
    	{
        	GD.PrintErr($"[Modifiers] Warning: Attempted to create tagged derived modifier of type {type} with " +
            $"{(sourceStat is null ? "no source stat" : "an invalid or 'none' tag")}!");
        	return null;
    	}

		var modifier = new DerivedStatModifier
		{
			Type = type,
			Source = source,
			Slot = slot,
			AffixType = affixType,
			Scope = scope,
			ConditionKey = conditionKey,

			SourceStat = sourceStat.Value,
			Ratio = ratio,
			Phase = phase
		};

		modifier.AffectedStats.AddRange(StatDefinitionRegistry.GetByTag(tag));
		return modifier;
	}

	public static StatConversionModifier? CreateSingleConversionModifier(
		ModifierType type, 
		float ratio, 
		StatId? sourceStat, 
		StatId? targetStat, 
		ModifierSource source, 
		EquipmentSlot slot = EquipmentSlot.None, 
		AffixType affixType = AffixType.None,
		ModifierScope scope = ModifierScope.Stat, 
		string conditionKey = "")
	{
		if (sourceStat is null || targetStat is null)
		{
			GD.PrintErr($"[Modifiers] Warning: Attempted to create a single conversion modifier of type {type} with no source or target stat!");
			return null;
		}

		var modifier = new StatConversionModifier
		{
			Type = type,
			Source = source,
			Slot = slot,
			AffixType = affixType,
			Scope = scope,
			ConditionKey = conditionKey,

			SourceStat = sourceStat.Value,
			Ratio = ratio,
			TargetSplitValues = new Dictionary<StatId, float>
			{
				{ targetStat.Value, 1.0f }
			}
		};

		return modifier;
	}

	public static StatConversionModifier? CreateTaggedConversionModifier(
		ModifierType type, 
		float ratio, 
		StatId? sourceStat, 
		StringName tag,
		ModifierSource source, 
		EquipmentSlot slot = EquipmentSlot.None, 
		AffixType affixType = AffixType.None,
		ModifierScope scope = ModifierScope.Stat, 
		string conditionKey = "",
		Dictionary<StatId, float>? manualSplitValues = null)
	{
		if (sourceStat is null || tag == null || tag.IsEmpty || tag == "none" )
		{
        	GD.PrintErr($"[Modifiers] Warning: Attempted to create tagged conversion modifier of type {type} with " +
            $"{(sourceStat is null ? "no source stat" : "an invalid or 'none' tag")}!");
        	return null;
    	}

		var modifier = new StatConversionModifier
		{
			Type = type,
			Source = source,
			Slot = slot,
			AffixType = affixType,
			Scope = scope,
			ConditionKey = conditionKey,

			SourceStat = sourceStat.Value,
			Ratio = ratio
		};

		var resolvedStats = StatDefinitionRegistry.GetByTag(tag);
		if (resolvedStats.Count() == 0)
    	{
        	GD.PrintErr($"[Modifiers] Warning: Tagged conversion modifier found no stats for tag '{tag}'!");
        	return modifier;
    	}

		Dictionary<StatId, float> finalSplitValues;

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
			finalSplitValues = new Dictionary<StatId, float>();

			foreach (var stat in resolvedStats)
			{
				finalSplitValues[stat] = splitWeightPerStat;
			}
		}

		modifier.TargetSplitValues = finalSplitValues;
		return modifier;
	}
}
