namespace lethal.common.context.evaluators;
public class AlwaysFalseTestEvaluator : IConditionEvaluator
{
    public bool Evaluate(ConditionContext context, string? parameter) => false;
}
