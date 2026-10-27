using System.Collections.Generic;
using lethal.core.domain.factions;
using lethal.core.domain.tags;

namespace lethal.common.registries.factions;
public sealed class FactionRegistry
{
    private readonly Registry<TagId, FactionDefinition> _all;
    private FactionRegistry(Registry<TagId, FactionDefinition> all) => _all = all;

    public static FactionRegistry Build(IEnumerable<FactionDefinition> definitions)
    {
        var all = Registry<TagId, FactionDefinition>.Build(definitions, d => d.Id, "DefinitionFactions");
        return new FactionRegistry(all);
    }

    public bool Contains(TagId id) => _all.Contains(id);
    public FactionDefinition Get(TagId id) => _all.Get(id);
    public int Count => _all.Count;
}