using System.Collections.Generic;

namespace lethal.core.config;
public class GameDataConfig
{
	public static readonly Dictionary<string, string> SheetUrls = new()
	{
		{"Localizations.csv", "https://docs.google.com/spreadsheets/d/1PsuPcV05J_fzQYaKp1-B4onTNdjodW-JGStyU0Wy0Jg/export?format=csv&gid=610263680"},
		{"StatDefinitions.csv", "https://docs.google.com/spreadsheets/d/1PsuPcV05J_fzQYaKp1-B4onTNdjodW-JGStyU0Wy0Jg/export?format=csv&gid=1604597693"},
		{"TagEntity.csv", "https://docs.google.com/spreadsheets/d/1PsuPcV05J_fzQYaKp1-B4onTNdjodW-JGStyU0Wy0Jg/export?format=csv&gid=472060416"},
		{"CharacterDefinitions.csv", "https://docs.google.com/spreadsheets/d/1PsuPcV05J_fzQYaKp1-B4onTNdjodW-JGStyU0Wy0Jg/export?format=csv&gid=1371248349"},
		{"CharacterBaseStats.csv", "https://docs.google.com/spreadsheets/d/1PsuPcV05J_fzQYaKp1-B4onTNdjodW-JGStyU0Wy0Jg/export?format=csv&gid=448510062"},
		{"CharacterStartingResources.csv", "https://docs.google.com/spreadsheets/d/1PsuPcV05J_fzQYaKp1-B4onTNdjodW-JGStyU0Wy0Jg/export?format=csv&gid=671148286"},
	};
}
