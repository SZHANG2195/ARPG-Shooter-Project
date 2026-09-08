namespace lethal.gameplay.stats.enums;
public enum StatPipelineFlags
{
	None = 0,
	BaseStats = 1 << 0,
	GearFlatValues = 1 << 1,
	Conversions = 1 << 2,
	Attributes = 1 << 3,
	SlotScalars = 1 << 4,
	GlobalMultipliers = 1 << 5,
	DerivedStats = 1 << 6,

	DefaultPlayer = BaseStats | GearFlatValues | Conversions | Attributes | SlotScalars | GlobalMultipliers | DerivedStats,
	SimpleEnemy = BaseStats | GlobalMultipliers
}
