using System;
using lethal.gameplay.equipment.enums;
using lethal.gameplay.stats.enums;

namespace lethal.gameplay.stats.modifiers;
public readonly struct ModifierSource : IEquatable<ModifierSource>
{
	public ModifierSourceCategory Category { get; }
	public string InstanceId { get; }
	public EquipmentSlot OriginSlot { get; }

	public ModifierSource(ModifierSourceCategory category, string instanceId = "", EquipmentSlot originSlot = EquipmentSlot.None)
	{
		Category = category;
		InstanceId = instanceId;
		OriginSlot = originSlot;
	}

	public bool Equals(ModifierSource other) => 
		Category == other.Category && 
		InstanceId == other.InstanceId && 
		OriginSlot == other.OriginSlot;

	public override bool Equals(object? obj) => obj is ModifierSource other && Equals(other);

	public override int GetHashCode() => HashCode.Combine(Category, InstanceId, OriginSlot);

    public override string ToString() => 
		string.IsNullOrEmpty(InstanceId) ? $"{Category}@{OriginSlot}" : $"{Category}:{InstanceId}@{OriginSlot}";

}
