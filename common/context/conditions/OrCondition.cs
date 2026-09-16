using System.Collections.Generic;
using System.Linq;

namespace lethal.common.context.conditions;
public class OrCondition : ICondition
{
    public required List<ICondition> Conditions { get; init; }
    public bool Evaluate(ConditionContext context) => Conditions.Any(c => c.Evaluate(context));
}
