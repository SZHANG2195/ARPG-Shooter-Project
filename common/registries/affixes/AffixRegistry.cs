using lethal.core.config;
using lethal.core.domain.affixes;
using lethal.core.domain.tags;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace lethal.common.registries.affixes;
public sealed class AffixRegistry
{
    private readonly Registry<AffixId, AffixDefinition> _all;
    private readonly FrozenDictionary<TagId, ImmutableArray<AffixDefinition>> _byType;
    private readonly ErrorConfig _errorConfig;

    private const string Source = nameof(AffixRegistry);

    private AffixRegistry(Registry<AffixId, AffixDefinition> all, FrozenDictionary<TagId, ImmutableArray<AffixDefinition>> byType, ErrorConfig errorConfig)
    {
        _all = all;
        _byType = byType;
        _errorConfig = errorConfig;
    }

    public static AffixRegistry Build(IEnumerable<AffixDefinition> defs, ErrorConfig errorConfig)
    {
        var all = Registry<AffixId, AffixDefinition>.Build(defs, d => d.Id, "DefinitionAffixes", errorConfig);
        var byType = all.All
            .GroupBy(a => a.AffixTypeTagId)
            .ToFrozenDictionary(g => g.Key, g => g.ToImmutableArray());
        return new AffixRegistry(all, byType, errorConfig);
    }

    public bool Contains(AffixId id) => _all.Contains(id);
    public AffixDefinition Get(AffixId id) => _all.Get(id);
    public IReadOnlyList<AffixDefinition> All => _all.All;
    public ImmutableArray<AffixDefinition> OfType(TagId type) => _byType.TryGetValue(type, out var list) ? list : ImmutableArray<AffixDefinition>.Empty;
	public int Count => _all.Count;
}
