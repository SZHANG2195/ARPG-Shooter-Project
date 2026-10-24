using System;
using System.Collections.Generic;
using lethal.core.domain.localizations;

namespace lethal.core.domain.localization;
public sealed class LocalizationKeySet : ILocalizationKeys
{
    public const string DataOnlyMarker = "[DATA ONLY]";

    private readonly HashSet<LocalizationKey> _all = new();
    private readonly HashSet<LocalizationKey> _dataOnly = new();

    public bool Add(LocalizationKey key, bool dataOnly)
    {
        if (!_all.Add(key)) return false;     // duplicate key
        if (dataOnly) _dataOnly.Add(key);
        return true;
    }

    public bool Contains(LocalizationKey key) => _all.Contains(key);
    public bool IsRenderable(LocalizationKey key) => _all.Contains(key) && !_dataOnly.Contains(key);

    public static bool IsDataOnlyMarker(string en) =>
        string.Equals(en.Trim(), DataOnlyMarker, StringComparison.OrdinalIgnoreCase);
}
