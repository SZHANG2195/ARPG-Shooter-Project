using Godot;
using lethal.core.domain.stat_identity;
using System;
using System.Collections.Generic;

namespace lethal.gameplay.stats.logic;
public class ResourcePool
{
    public StatId MaxStat { get; private set; }
    public float CurrentValue { get; private set; }
    public float ReservedValue { get; private set; }

    public event Action? MeaningfulChange;

    private readonly SortedSet<float> _watchedThresholds = new();
    private float _nextThresholdAbove = float.PositiveInfinity;
    private float _nextThresholdBelow = float.NegativeInfinity;
    private float _lastDirtyTriggerValue = 0f;
    private const float DirtyTriggerMagnitude = 1.0f;

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

    public void Spend(float amount)
    {
        CurrentValue = Mathf.Max(CurrentValue - amount, 0.0f);
        OnValueChanged();
    }

    public void Restore(float amount, float maxLimit)
    {
        CurrentValue = Mathf.Min(CurrentValue + amount, maxLimit);
        OnValueChanged();
    }

    public void Set(float amount, float maxLimit)
    {
        CurrentValue = Mathf.Clamp(amount, 0.0f, maxLimit);
        OnValueChanged();
    }

    public void ClampTo(float max)
    {
        CurrentValue = Mathf.Min(CurrentValue, max);
        OnValueChanged();
    }

    public bool IsEmpty() => CurrentValue <= 0;

    public void RegisterThreshold(float value)
    {
        if (_watchedThresholds.Add(value))
        {
            RecomputeNearestBounds();
        }
    }

    public void UnregisterThreshold(float value)
    {
        if (_watchedThresholds.Remove(value))
        {
            RecomputeNearestBounds();
        }
    }

    private void OnValueChanged()
    {
        bool magnitudeCrossed = Mathf.Abs(CurrentValue - _lastDirtyTriggerValue) >= DirtyTriggerMagnitude;
        bool boundaryCrossed = CurrentValue >= _nextThresholdAbove || CurrentValue <= _nextThresholdBelow;

        if (magnitudeCrossed || boundaryCrossed)
        {
            _lastDirtyTriggerValue = CurrentValue;
            RecomputeNearestBounds();
            MeaningfulChange?.Invoke();
        }
    }

    private void RecomputeNearestBounds()
	{
    	var viewAbove = _watchedThresholds.GetViewBetween(CurrentValue, float.PositiveInfinity);
    	if (viewAbove.Count > 0)
    	{
        	using var enumerator = viewAbove.GetEnumerator();
        	enumerator.MoveNext();
        	_nextThresholdAbove = enumerator.Current;
    	}
    	else
    	{
        	_nextThresholdAbove = float.PositiveInfinity;
    	}

    	var viewBelow = _watchedThresholds.GetViewBetween(float.NegativeInfinity, CurrentValue);
    	if (viewBelow.Count > 0)
    	{
        	using var revEnumerator = viewBelow.Reverse().GetEnumerator();
        	revEnumerator.MoveNext();
        	_nextThresholdBelow = revEnumerator.Current;
    	}
    	else
    	{
        	_nextThresholdBelow = float.NegativeInfinity;
    	}
	}
}