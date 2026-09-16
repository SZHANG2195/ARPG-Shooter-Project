namespace lethal.common.context.conditions;

public class LeafCondition : ICondition
{
    public required string ConditionType { get; init; }
    public string? Parameter { get; init; }

    public bool Evaluate(ConditionContext context) => ConditionEvaluatorRegistry.Evaluate(context, ConditionType, Parameter);

}
