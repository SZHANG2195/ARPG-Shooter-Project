namespace lethal.common.context.evaluators;
public interface IConditionEvaluator
{
    bool Evaluate(ConditionContext context, string? parameter);
}
