using System.Collections.Generic;
using lethal.core.domain.stats;
using lethal.gameplay.payloads.instances;

namespace lethal.gameplay.stats.components;
public sealed partial class StatsComponent
{
	private sealed class StatState
    {
        public readonly StatId Id;
        
        public readonly List<StatPayloadInstance> PrimaryInstances = new();
        public readonly List<StatPayloadInstance> SecondaryInstances = new();
        public readonly List<StatPayloadInstance> DrainInstances = new();
        public readonly List<StatSourceValue> SourceValues = new();

        public readonly double OriginalBaseValue;
        public double? DefaultOverrideValue;
        public double? FlatOverrideValue;
        public double? FinalOverrideValue;
        public StatLaneResult Default;
        public StatLaneResult[]? Lanes;

        public int LanePushCount;

        public StatState(StatId id, double originalBaseValue)
        {
            Id = id;
            OriginalBaseValue = originalBaseValue;
            Default = new StatLaneResult
            {
                Result = StatResult.Seed(originalBaseValue),
                IsDirty = true
            };
        }

        public void MarkAllDirty()
        {
            Default.IsDirty = true;
            if (Lanes != null)
            {
                for (int i = 0; i < Lanes.Length; i++)
                {
                    Lanes[i].IsDirty = true;
                }
            }
        }

        public void MarkLaneDirty(StatStateLane lane)
        {
            if (lane == StatStateLane.Default)
            {
                Default.IsDirty = true;
                return;
            }
            EnsureLanes()[(int)lane - 1].IsDirty = true;
        }

        public bool IsAnyDirty()
        {
            if (Default.IsDirty) return true;
            if (Lanes != null)
            {
                for (int i = 0; i < Lanes.Length; i++)
                {
                    if (Lanes[i].IsDirty) return true;
                }
            }
            return false;
        }

        public bool IsFullyDirty()
        {
            if (!Default.IsDirty) return false;
            if (Lanes != null)
            {
                for (int i = 0; i < Lanes.Length; i++)
                {
                    if (!Lanes[i].IsDirty) return false;
                }
            }
    
            return true;
        }

        public StatLaneResult[] EnsureLanes()
        {
            if (Lanes is null)
            {
                Lanes = new StatLaneResult[2];
                for (int i = 0; i < Lanes.Length; i++) Lanes[i].IsDirty = true;
            }
            return Lanes;
        }

        public bool HasNoModifiers =>
            PrimaryInstances.Count == 0 && 
            SecondaryInstances.Count == 0 && 
            DrainInstances.Count == 0 && 
            SourceValues.Count == 0;

        public bool HasOnlySourceValues =>
            PrimaryInstances.Count == 0 && 
            SecondaryInstances.Count == 0 && 
            DrainInstances.Count == 0 && 
            SourceValues.Count > 0;
    }
}
