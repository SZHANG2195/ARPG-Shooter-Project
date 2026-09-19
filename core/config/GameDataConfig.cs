using System.Collections.Generic;

namespace lethal.core.config;
public class GameDataConfig
{
	public static readonly Dictionary<string, string> SheetUrls = new()
	{
		{"StatDefinitions.csv", "https://docs.google.com/spreadsheets/d/1BVYSrX-1SZVfNtBe4aZiadmvpUSkr2CVkCDatfYzZM8/export?format=csv&gid=1755736745"},
		{"TagEntity.csv", "https://docs.google.com/spreadsheets/d/1PFgYAlrkky01w6p1JgrO-s-iBWwqfoEWX_7eTKFmlQU/export?format=csv&gid=1569023865"},
		{"Localizations.csv", "https://docs.google.com/spreadsheets/d/1RA8IUZWhM-Zi9TMD38t8_0hHAMxaZu7_kuOcukHLVwM/export?format=csv&gid=0"},
		{"CharacterDefinitions.csv", "https://docs.google.com/spreadsheets/d/1OQvfRj8u4imAM3M6GP81XfBjlf8pnDNoMZoo4F22lF8/export?format=csv&gid=0"},
		{"CharacterBaseStats.csv", "https://docs.google.com/spreadsheets/d/1SiXQQABG2iE57NwEGcTvflmlyrXuDS8uiwgjXkMvN8c/export?format=csv&gid=0"},
		{"CharacterStartingResources.csv", "https://docs.google.com/spreadsheets/d/1QIhQajjJ7PElTASH0R_yJcoRVjRobj7f5hj_mgjfGco/export?format=csv&gid=0"},
	};
}
