using lethal.core.domain.localizations;

namespace lethal.core.domain.localization;

public interface ILocalizationKeys
{
    bool Contains(LocalizationKey key);
    bool IsRenderable(LocalizationKey key);   // false for [DATA ONLY] entries
}
