using System.Collections.Immutable;

namespace lethal.core.domain.payloads;
public interface IDefaultDisablingPayload
{
    public ImmutableArray<PayloadId> DisabledDefaultIds { get; }
}
