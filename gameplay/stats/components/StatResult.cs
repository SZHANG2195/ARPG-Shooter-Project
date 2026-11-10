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
		double AddedFlat = 0,
		double AdditiveMultiplier = 0,
		double MultiplicativeMultiplier = 1)
	{
    	public static StatResult Seed(double baseValue) => new(
        	BaseValue: baseValue,
        	FlatTotalValue: baseValue,
			DerivedPreConversionValue: baseValue,
        	PreMultiplierValue: baseValue,
        	PostMultiplierValue: baseValue,
        	ResolvedValue: baseValue,
        	FinalValue: baseValue,
        	AddedFlat: 0,
        	AdditiveMultiplier: 0,
        	MultiplicativeMultiplier: 1
    	);
		public StatResult ApplyFlatValue(double addedFlatValue)
		{
			double newFlatTotal = BaseValue + addedFlatValue;

			return this with
			{
				AddedFlat = addedFlatValue,
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

		public StatResult ApplyConvertedValue(double convertedFlatValue)
		{
			double newConvertedValue = DerivedPreConversionValue + convertedFlatValue;

			return this with
			{
				PreMultiplierValue = newConvertedValue,
				PostMultiplierValue = newConvertedValue,
				ResolvedValue = newConvertedValue,
				FinalValue = newConvertedValue
			};
		}

		public StatResult ApplyMultiplierValue(double additiveMultiplierValue, double multiplicativeMultiplierValue)
		{
			double newPostMultiplierValue = PreMultiplierValue * (1.0 + additiveMultiplierValue) * multiplicativeMultiplierValue;

			return this with
			{
				AdditiveMultiplier = additiveMultiplierValue,
				MultiplicativeMultiplier = multiplicativeMultiplierValue,
				PostMultiplierValue = newPostMultiplierValue,
				ResolvedValue = newPostMultiplierValue,
				FinalValue = newPostMultiplierValue
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