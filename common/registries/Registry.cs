using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using lethal.core.config;

namespace lethal.common.registries;
public sealed class Registry<TId, TDef> where TId : notnull
{
    private readonly FrozenDictionary<TId, TDef> _byId;
    private readonly IReadOnlyList<TDef> _ordered;
    private readonly ErrorConfig _errorConfig;
    
    private const string Source = nameof(Registry<TId, TDef>);

    private Registry(FrozenDictionary<TId, TDef> byId, IReadOnlyList<TDef> ordered, ErrorConfig errorConfig)
    {
        _byId = byId;
        _ordered = ordered;
        _errorConfig = errorConfig;
    }

    public static Registry<TId, TDef> Build(IEnumerable<TDef> definitions, Func<TDef, TId> idOf, string table, ErrorConfig errorConfig)
    {
        var map = new Dictionary<TId, TDef>();
        var ordered = new List<TDef>();
        foreach (var definition in definitions)
        {
            var id = idOf(definition);
            if (!map.TryAdd(id, definition))
            {
                var message = $"{table}: duplicate id '{id}' detected.";
                errorConfig.ReportError(Source, message);
            }
            ordered.Add(definition);
        }
        return new(map.ToFrozenDictionary(), ordered.ToImmutableArray(), errorConfig);
    }

    public bool Contains(TId id) => _byId.ContainsKey(id);
    public bool TryGet(TId id, out TDef definition) => _byId.TryGetValue(id, out definition!);
    public TDef Get(TId id) => _byId.TryGetValue(id, out var d) ? d : throw new KeyNotFoundException($"{typeof(TDef).Name} '{id}' not found.");
    public IReadOnlyList<TDef> All => _ordered;
    public int Count => _ordered.Count;
}