using System;

namespace lethal.core.domain.stat_identity;
public readonly struct StatId : IEquatable<StatId>
{
    public string Key { get; }

    public StatId(string key)
    {
        Key = key;
    }

    public bool Equals(StatId other) => Key == other.Key;
    public override bool Equals(object? obj) => obj is StatId other && Equals(other);
    public override int GetHashCode() => Key.GetHashCode();
    public override string ToString() => Key;

    public static bool operator ==(StatId left, StatId right) => left.Equals(right);
    
    public static bool operator !=(StatId left, StatId right) => !left.Equals(right);

    public static implicit operator string(StatId id) => id.Key;
}
