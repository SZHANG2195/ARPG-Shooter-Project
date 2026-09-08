using Godot;
using lethal.stats.data;
using System;
using System.Collections.Generic;

namespace lethal.stats.logic;
public class StatConversionModifier : StatModifier
{
	public StatType SourceStat { get; set; }
	public float Ratio { get; set; }

	private Dictionary<StatType, float> _targetSplitValues = new();

	public Dictionary<StatType, float> TargetSplitValues
	{
		get => _targetSplitValues;
		set
		{
			_targetSplitValues = value;

			AffectedStats.Clear();

			if (_targetSplitValues != null)
			{
				AffectedStats.AddRange(_targetSplitValues.Keys);
			}
		}
	}

    public override float GetValue() => 0.0f;
}
