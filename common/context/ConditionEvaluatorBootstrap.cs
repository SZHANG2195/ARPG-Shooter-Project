using lethal.common.context.evaluators;

namespace lethal.common.conditions;

public static class ConditionEvaluatorBootstrap
{
    public static void RegisterAll()
    {
        ConditionEvaluatorRegistry.Register("is_full_life", new IsFullLifeEvaluator());
        ConditionEvaluatorRegistry.Register("target_life_below", new TargetLifeBelowEvaluator());
        ConditionEvaluatorRegistry.Register("always_false_test", new AlwaysFalseTestEvaluator());
    }
}
