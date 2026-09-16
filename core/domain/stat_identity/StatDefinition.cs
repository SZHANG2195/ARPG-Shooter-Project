using Godot;
using System.Collections.Generic;

namespace lethal.core.domain.stat_identity;
public class StatDefinition
{
    public StatId Id { get; init; }
    public required string LocalizationKey { get; init; }
    public bool IsRangePaired { get; init; }
    public StatId? PairedCounterpart { get; init; }
    public HashSet<StringName> Tags { get; init; } = new();
}