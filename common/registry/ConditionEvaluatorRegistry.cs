using Godot;
using lethal.common.context;
using lethal.common.context.evaluators;
using System;
using System.Collections.Generic;

public static class ConditionEvaluatorRegistry
{
    private static readonly Dictionary<string, IConditionEvaluator> _evaluators = new();

    public static void Register(string conditionType, IConditionEvaluator evaluator)
    {
        if (_evaluators.ContainsKey(conditionType))
        {
            GD.PrintErr($"[ConditionEvaluatorRegistry] Warning: '{conditionType}' is already registered — overwriting.");
        }

        _evaluators[conditionType] = evaluator;
    }

    public static bool Evaluate(ConditionContext context, string conditionType, string? parameter)
    {
        if (_evaluators.TryGetValue(conditionType, out var evaluator))
        {
            return evaluator.Evaluate(context, parameter);
        }

        string errorMsg = $"[ConditionEvaluatorRegistry] Error: Unknown condition type '{conditionType}'!";
        #if DEBUG
        throw new InvalidOperationException(errorMsg);
        #else
        GD.PrintErr(errorMsg);
        return false;
        #endif
    }
}
