using lethal.core.domain.context;

namespace lethal.core.domain.payloads;
public interface IContextualPayload
{
    ContextId? ContextId { get; }
}
