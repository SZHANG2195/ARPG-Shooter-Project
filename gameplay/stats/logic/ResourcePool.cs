using Godot;
using lethal.core.domain.stat_identity;

namespace lethal.gameplay.stats.logic;
public class ResourcePool 
{
	//We need the base Stat as our reference when initializing
	public StatId MaxStat { get; private set; }
	public float CurrentValue { get; private set; }
	public float ReservedValue { get; private set; }

	public ResourcePool(StatId maxStat)
	{
		MaxStat = maxStat;
		CurrentValue = 0.0f;
		ReservedValue = 0.0f;
	}

	public float GetEffectiveMax(float rawMax) => Mathf.Max(rawMax - ReservedValue, 0.0f);
	public void SetReservation(float reservedAmount, float rawMax) 
	{
		ReservedValue = Mathf.Clamp(reservedAmount, 0.0f, rawMax);
		ClampTo(GetEffectiveMax(rawMax));
	} 	
	public void Spend(float amount) => CurrentValue = Mathf.Max(CurrentValue - amount, 0.0f);
	public void Restore(float amount, float maxLimit) => CurrentValue = Mathf.Min(CurrentValue + amount, maxLimit);
	public void Set(float amount, float maxLimit) => CurrentValue = Mathf.Clamp(amount, 0.0f, maxLimit);
	public void ClampTo(float max) => CurrentValue = Mathf.Min(CurrentValue, max);
	public bool IsEmpty() => CurrentValue <= 0;
}
