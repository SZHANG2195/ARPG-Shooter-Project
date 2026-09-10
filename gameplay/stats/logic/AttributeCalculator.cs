using System.Collections.Generic;
using lethal.core.persistence;
using lethal.core.persistence.stat_identity;
using lethal.gameplay.stats.enums;
using lethal.gameplay.stats.modifiers;


namespace lethal.gameplay.stats.logic;
public static class AttributeCalculator
{
	public static IEnumerable<StatModifier> GetDerivedModifiers(
		float strength, 
		float agility, 
		float intelligence,
		IEnumerable<StatModifier> activeModifers = null)
	{
		var suppressedAttributes = new HashSet<StatId>();

		float GetAttributeValue(StatId stat)
        {
            if (stat == Stats.Strength) return strength;
            if (stat == Stats.Agility) return agility;
            if (stat == Stats.Intelligence) return intelligence;
            return 0.0f;
        }

		if (activeModifers != null)
		{
			foreach (var modifier in activeModifers)
			{
				if (modifier is AttributeOverrideStatModifier overrideModifier)
				{
					float combinedPoolValue = 0.0f;

					foreach(var sourceKvp in overrideModifier.SourceAttributeWeights)
					{
						StatId sourceAttribute = sourceKvp.Key;
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

		if (strength > 0 && !suppressedAttributes.Contains(Stats.Strength))
        {
            yield return StatModifier.CreateSingleStaticModifier(
                ModifierType.Flat,
                strength * 5f, Stats.Strength,
                new(ModifierSourceCategory.Attribute, "Strength"));
        }
        if (agility > 0 && !suppressedAttributes.Contains(Stats.Agility))
        {
            yield return StatModifier.CreateSingleStaticModifier(
                ModifierType.Increased,
                agility * 0.5f, Stats.Agility,
                new(ModifierSourceCategory.Attribute, "Agility"));
        }
        if (intelligence > 0 && !suppressedAttributes.Contains(Stats.Intelligence))
        {
            yield return StatModifier.CreateSingleStaticModifier(
                ModifierType.Flat,
                intelligence * 5f, Stats.Intelligence,
                new(ModifierSourceCategory.Attribute, "Intelligence"));
        }
	}
}
