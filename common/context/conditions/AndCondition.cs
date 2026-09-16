using System.Collections.Generic;
using System.Linq;

namespace lethal.common.context.conditions;
public class AndCondition : ICondition
{
    public required List<ICondition> Conditions { get; init; }
    public bool Evaluate(ConditionContext context) => Conditions.All(c => c.Evaluate(context));

}
