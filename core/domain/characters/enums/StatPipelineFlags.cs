using System;

namespace lethal.core.domain.characters.enums;
[Flags]
public enum StatPipelineFlags
{
	None = 0,
	BaseStats = 1 << 0,
	Conversions = 1 << 1,
	Attributes = 1 << 2,
	Effectiveness = 1 << 3,
	GlobalMultipliers = 1 << 4,
	DerivedStats = 1 << 5,

	DefaultPlayer = BaseStats | Conversions | Attributes | Effectiveness | GlobalMultipliers | DerivedStats,
	
	SimpleEnemy = BaseStats | GlobalMultipliers
}
