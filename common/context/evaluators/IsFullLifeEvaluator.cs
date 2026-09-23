using lethal.core.persistence.generated;

namespace lethal.common.context.evaluators;
public partial class IsFullLifeEvaluator : IConditionEvaluator
{
	public bool Evaluate(ConditionContext context, string? parameter)
    {
        if (context.Source == null) return false;
        float? percentage = context.Source.GetResourcePoolPercentage(Stats.MaxHealth);

        if (percentage == null) return false;
        return percentage.Value >= 1.0f;
    }
}
