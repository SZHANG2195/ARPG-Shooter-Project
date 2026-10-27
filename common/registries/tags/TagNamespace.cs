using System;
using lethal.core.domain.tags;

namespace lethal.common.registries.tags;
public readonly struct TagNamespace
{
    private readonly string _dotted;
    public string Prefix { get; }

    public TagNamespace(string prefix) { Prefix = prefix; _dotted = prefix + "."; }

    public bool Contains(TagId tag) => tag.Value is { } v && v.StartsWith(_dotted, StringComparison.Ordinal);
    public override string ToString() => Prefix;
}
