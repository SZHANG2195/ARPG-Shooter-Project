using lethal.core.persistence.generated;

namespace lethal.common.context.evaluators;
public class TargetLifeBelowEvaluator : IConditionEvaluator
{
    public bool Evaluate(ConditionContext context, string? parameter)
    {
        if (context.Target == null) return false;
        float? percentage = context.Target.GetResourcePoolPercentage(Stats.MaxHealth);

        if (percentage == null) return false;
        return percentage <= float.Parse(parameter ?? "0.5");
    }
}
