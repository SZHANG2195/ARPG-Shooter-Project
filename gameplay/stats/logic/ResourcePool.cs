using Godot;
using lethal.core.persistence.stat_identity;

namespace lethal.gameplay.stats.logic;
public class ResourcePool 
{
	//We need the base Stat as our reference when initializing
	public StatId MaxStat { get; private set; }
	public float Current { get; private set; }
	public float Reserved { get; private set; }

	public ResourcePool(StatId maxStat)
	{
		MaxStat = maxStat;
		Current = 0.0f;
		Reserved = 0.0f;
	}

	public float GetEffectiveMax(float rawMax) => Mathf.Max(rawMax - Reserved, 0.0f);
	public void SetReservation(float reservedAmount, float rawMax) 
	{
		Reserved = Mathf.Clamp(reservedAmount, 0.0f, rawMax);
		ClampTo(GetEffectiveMax(rawMax));
	} 	
	public void Spend(float amount) => Current = Mathf.Max(Current - amount, 0.0f);
	public void Restore(float amount, float maxLimit) => Current = Mathf.Min(Current + amount, maxLimit);
	public void Set(float amount, float maxLimit) => Current = Mathf.Clamp(amount, 0.0f, maxLimit);
	public void ClampTo(float max) => Current = Mathf.Min(Current, max);
	public bool IsEmpty() => Current <= 0;
}
