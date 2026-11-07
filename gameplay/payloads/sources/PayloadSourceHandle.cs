using System;
using lethal.gameplay.payloads.enums;

namespace lethal.gameplay.payloads.sources;
public readonly record struct PayloadSourceHandle(SourceKind Kind, long Id)
{
    public bool Equals(PayloadSourceHandle other) => Kind == other.Kind && Id == other.Id;
    public override int GetHashCode() => HashCode.Combine(Kind, Id);
}