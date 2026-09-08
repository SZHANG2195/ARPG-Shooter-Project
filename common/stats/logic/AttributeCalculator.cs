using Godot;
using lethal.stats.data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;


namespace lethal.stats.logic;
public static class AttributeCalculator
{
	public static IEnumerable<StatModifier> GetDerivedModifiers(
		float strength, 
		float agility, 
		float intelligence,
		IEnumerable<StatModifier> activeModifers = null)
	{
		var suppressedAttributes = new HashSet<StatType>();

		float GetAttributeValue(StatType stat) => stat switch
        {
            StatType.Strength => strength,
            StatType.Agility => agility,
            StatType.Intelligence => intelligence,
            _ => 0.0f
        };

		if (activeModifers != null)
		{
			foreach (var modifier in activeModifers)
			{
				if (modifier is AttributeOverrideStatModifier overrideModifier)
				{
					float combinedPoolValue = 0.0f;

					foreach(var sourceKvp in overrideModifier.SourceAttributeWeights)
					{
						StatType sourceAttribute = sourceKvp.Key;
						float weight = sourceKvp.Value;

						combinedPoolValue += GetAttributeValue(sourceAttribute) * weight;

						if (overrideModifier.SuppressDefaultYields)
						{
							suppressedAttributes.Add(sourceAttribute);
						}
					}

					foreach (var targetKvp in overrideModifier.TargetYields)
					{
						float finalValue = combinedPoolValue * targetKvp.RatioPerPoint;
						
                        if (finalValue != 0f)
                        {
                            yield return StatModifier.CreateSingleStaticModifier(
                                targetKvp.Type,
                                finalValue, 
                                targetKvp.TargetStat, 
                                new ModifierSource(ModifierSourceCategory.Attribute, "Custom Scaling Rule")
                            );
                        }
					}
				}
			}
		}

		if (strength > 0 && !suppressedAttributes.Contains(StatType.Strength))
        {
            yield return StatModifier.CreateSingleStaticModifier(
				ModifierType.Flat, 
				strength * 5f, StatType.MaxHealth, 
				new(ModifierSourceCategory.Attribute, "Strength"));
        }
        if (agility > 0 && !suppressedAttributes.Contains(StatType.Agility))
        {
            yield return StatModifier.CreateSingleStaticModifier(
				ModifierType.Increased, 
				agility * 0.5f, StatType.MovementSpeedWhileFiring, 
				new(ModifierSourceCategory.Attribute, "Agility"));
        }
        if (intelligence > 0 && !suppressedAttributes.Contains(StatType.Intelligence))
        {
            yield return StatModifier.CreateSingleStaticModifier(
				ModifierType.Flat, 
				intelligence * 5f, StatType.MaxFuel, 
				new(ModifierSourceCategory.Attribute, "Intelligence"));
        }
	}
}
