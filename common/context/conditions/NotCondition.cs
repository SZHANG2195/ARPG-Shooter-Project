namespace lethal.common.context.conditions;
public class NotCondition : ICondition
{
    public required ICondition Inner { get; init; }
    public bool Evaluate(ConditionContext context) => !Inner.Evaluate(context);
}
