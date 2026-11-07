using System;

namespace lethal.core.config;
public readonly record struct ErrorConfig(
    Action<string> OnError,
    Action<string> OnWarning,
    bool StrictValidation = false
)
{
    public void ReportError(string source, string message)
    {
        string text = $"[{source}]: {message}";
        if (StrictValidation) OnError(text);
        else OnWarning(text);
    }
}
