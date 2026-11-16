namespace lethal.gameplay.stats.components;
public sealed partial class StatsComponent
{
	public readonly record struct StatResult(
		double BaseValue, 
		double FlatTotalValue,
		double DerivedPreConversionValue,
		double PreMultiplierValue,
		double PostMultiplierValue,
		double ResolvedValue,
		double FinalValue,
		double RawAddedFlat = 0.0,
		double RawAdditiveMultiplier = 0.0,
		double RawMultiplicativeMultiplier = 0.0,
		double TotalAdditiveMultiplier = 0.0,
		double TotalMultiplicativeMultiplier = 0.0,
		double TotalConversionDrainRatio = 0.0,
		double ConversionNormalizationRatio = 1.0)
	{
    	public static StatResult Seed(double baseValue) => new(
        	BaseValue: baseValue,
        	FlatTotalValue: baseValue,
			DerivedPreConversionValue: baseValue,
        	PreMultiplierValue: baseValue,
        	PostMultiplierValue: baseValue,
        	ResolvedValue: baseValue,
        	FinalValue: baseValue
    	);
		public StatResult ApplyFlatValue(double addedFlatValue)
		{
			double newFlatTotal = BaseValue + addedFlatValue;

			return this with
			{
				RawAddedFlat = addedFlatValue,
				FlatTotalValue = newFlatTotal,
				DerivedPreConversionValue = newFlatTotal,
				PreMultiplierValue = newFlatTotal,
				PostMultiplierValue = newFlatTotal,
				ResolvedValue = newFlatTotal,
				FinalValue = newFlatTotal
			};
		}

		public StatResult ApplyFlatOverrideValue(double flatOverrideValue)
		{
			return this with
    		{
        		FlatTotalValue = flatOverrideValue,
        		DerivedPreConversionValue = flatOverrideValue,
        		PreMultiplierValue = flatOverrideValue,
        		PostMultiplierValue = flatOverrideValue,
        		ResolvedValue = flatOverrideValue,
        		FinalValue = flatOverrideValue
    		};
		}

		public StatResult ApplyDerivedPreConversionValue(double addedDerivedFlatValue)
		{
			double newDerivedPreConversionValue = FlatTotalValue + addedDerivedFlatValue;

			return this with
			{
				DerivedPreConversionValue = newDerivedPreConversionValue,
				PreMultiplierValue = newDerivedPreConversionValue,
				PostMultiplierValue = newDerivedPreConversionValue,
				ResolvedValue = newDerivedPreConversionValue,
				FinalValue = newDerivedPreConversionValue
			};
		}

		public StatResult ApplyConversionSubtractedValue(double conversionSubtractedFlatValue, double conversionDrainRatio, double conversionNormalizationRatio)
		{
			double newConvertedSubtractedValue = DerivedPreConversionValue + conversionSubtractedFlatValue;

			return this with
			{
				TotalConversionDrainRatio = conversionDrainRatio,
				ConversionNormalizationRatio = conversionNormalizationRatio,
				PreMultiplierValue = newConvertedSubtractedValue,
				PostMultiplierValue = newConvertedSubtractedValue,
				ResolvedValue = newConvertedSubtractedValue,
				FinalValue = newConvertedSubtractedValue
			};
		}

		public StatResult ApplyConversionGainedValue(double conversionGainedFlatValue)
		{
			double newConvertedGainedValue = PreMultiplierValue + conversionGainedFlatValue;

			return this with
			{
				PreMultiplierValue = newConvertedGainedValue,
				PostMultiplierValue = newConvertedGainedValue,
				ResolvedValue = newConvertedGainedValue,
				FinalValue = newConvertedGainedValue
			};
		}

		public StatResult ApplyRawMultiplierValue(double rawAdditiveMultiplierValue, double rawMultiplicativeMultiplierValue, double scopedMultiplicativeMultiplierValue)
		{
			double totalRawMultiplicativeMultiplierValue = (1.0 + rawMultiplicativeMultiplierValue) * (1.0 + scopedMultiplicativeMultiplierValue);
			return this with
			{
				RawAdditiveMultiplier = rawAdditiveMultiplierValue,
				RawMultiplicativeMultiplier = totalRawMultiplicativeMultiplierValue
			};
		}

		public StatResult ApplyMirroredMultiplierValue(double mirroredAdditiveMultiplierValue, double mirroredMultiplicativeMultiplierValue)
		{
			double newMirroredMultiplierValue = RawAdditiveMultiplier + mirroredAdditiveMultiplierValue;
			double newMirroredMultiplicativeMultiplierValue = (1.0 + RawMultiplicativeMultiplier) * (1.0 + mirroredMultiplicativeMultiplierValue) - 1.0;
			double newPostMultiplierValue = PreMultiplierValue * (1.0 + newMirroredMultiplierValue) * (1.0 + newMirroredMultiplicativeMultiplierValue);

			return this with
			{
				TotalAdditiveMultiplier = newMirroredMultiplierValue,
        		TotalMultiplicativeMultiplier = newMirroredMultiplicativeMultiplierValue,
				PostMultiplierValue = newPostMultiplierValue,
				ResolvedValue = newPostMultiplierValue,
				FinalValue = newPostMultiplierValue
			};
		}

		public StatResult ApplyTargetScopedValue(double scopedDelta)
		{
			double newScopedPostMultiplierValue = PostMultiplierValue + scopedDelta;

			return this with
			{
				PostMultiplierValue = newScopedPostMultiplierValue,
				ResolvedValue = newScopedPostMultiplierValue,
				FinalValue = newScopedPostMultiplierValue
			};
		}

		public StatResult ApplyDerivedPostMultiplierValue(double derivedPostMultiplierValue)
		{
			double newDerivedPostMultiplierValue = PostMultiplierValue + derivedPostMultiplierValue;

			return this with
			{
				ResolvedValue = newDerivedPostMultiplierValue,
				FinalValue = newDerivedPostMultiplierValue
			};
		}

		public StatResult ApplyDerivedResolvedValue(double derivedFinalValue)
		{
			double newDerivedFinalValue = ResolvedValue + derivedFinalValue;

			return this with
			{
				FinalValue = newDerivedFinalValue
			};
		}

		public StatResult ApplyFinalOverrideValue(double finalOverrideValue)
		{
			return this with
    		{
        		FinalValue = finalOverrideValue
    		};
		}
	}
}