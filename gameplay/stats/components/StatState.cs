using System.Collections.Generic;
using lethal.core.domain.stats;
using lethal.gameplay.payloads.instances;

namespace lethal.gameplay.stats.components;
public sealed partial class StatsComponent
{
	private sealed class StatState
    {
        public StatId Id;
        
        public readonly List<StatPayloadInstance> PrimaryInstances = new();
        public readonly List<StatPayloadInstance> SecondaryInstances = new();
        public readonly List<StatPayloadInstance> DrainInstances = new();
        public readonly List<StatSourceValue> SourceValues = new();

        public readonly double OriginalBaseValue;
        public double? DefaultOverrideValue;
        public StatLaneResult Default;
        public StatLaneResult[]? Lanes;

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

            int laneIndex = (int)lane - 1;
            Lanes ??= new StatLaneResult[2];
            Lanes[laneIndex].IsDirty = true;
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
    }
}
