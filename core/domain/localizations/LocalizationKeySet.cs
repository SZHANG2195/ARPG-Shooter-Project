using System;
using System.Collections.Generic;

namespace lethal.core.domain.localizations;

public sealed class LocalizationKeySet : ILocalizationKeys
{
    public const string DataOnlyMarker = "[DATA ONLY]";

    private readonly HashSet<LocalizationKey> _dataOnly = new();

    public void MarkDataOnly(LocalizationKey key) => _dataOnly.Add(key);

    public bool IsRenderable(LocalizationKey key) => !_dataOnly.Contains(key);

    public static bool IsDataOnlyMarker(string en) =>
        string.Equals(en.Trim(), DataOnlyMarker, StringComparison.OrdinalIgnoreCase);
}
