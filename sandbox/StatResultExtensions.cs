using Godot;
using lethal.gameplay.stats.components;

public static class StatResultExtensions
{
    public static void PrintDebug(this StatsComponent.StatResult result, string statName = "Stat")
    {
        GD.Print($"=== [StatResult Debug: {statName}] ===");
        GD.Print($"  Base Value:                     {result.BaseValue}");
        GD.Print($"  Flat Total Value:               {result.FlatTotalValue}");
        GD.Print($"  Derived Pre-Conversion:         {result.DerivedPreConversionValue}");
        GD.Print($"  Pre-Multiplier Value:           {result.PreMultiplierValue}");
        GD.Print($"  Post-Multiplier Value:          {result.PostMultiplierValue}");
        GD.Print($"  Resolved Value:                 {result.ResolvedValue}");
        GD.Print($"  Final Value:                    {result.FinalValue}");
        GD.Print($"  Raw Added Flat:                 {result.RawAddedFlat}");
        GD.Print($"  Raw Additive Multiplier:        {result.RawAdditiveMultiplier}");
        GD.Print($"  Raw Multiplicative Multiplier:  {result.RawMultiplicativeMultiplier}");
        GD.Print($"  Total Additive Multiplier:      {result.TotalAdditiveMultiplier}");
        GD.Print($"  Total Multiplicative Multiplier:{result.TotalMultiplicativeMultiplier}");
        GD.Print($"  Total Conversion Drain Ratio:   {result.TotalConversionDrainRatio}");
        GD.Print($"  Conversion Normalization Ratio: {result.ConversionNormalizationRatio}");
        GD.Print($"========================================");
    }
}