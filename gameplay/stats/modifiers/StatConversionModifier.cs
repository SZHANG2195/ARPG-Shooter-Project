using System.Collections.Generic;
using lethal.core.persistence.stat_identity;

namespace lethal.gameplay.stats.modifiers;
public class StatConversionModifier : StatModifier
{
	public StatId SourceStat { get; set; }
	public float Ratio { get; set; }

	private Dictionary<StatId, float> _targetSplitValues = new();

	public Dictionary<StatId, float> TargetSplitValues
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
