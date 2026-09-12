using Godot;
using System;

namespace lethal.core.localizations;
public class LocalizationManager
{
	public static string Localize(string localizationKey)
	{
		if (string.IsNullOrWhiteSpace(localizationKey))
		{
			return string.Empty;
		}

		return TranslationServer.Translate(localizationKey);
	}
}
