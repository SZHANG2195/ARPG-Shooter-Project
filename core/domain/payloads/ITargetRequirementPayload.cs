using lethal.core.domain.tags;

namespace lethal.core.domain.payloads;
public interface ITargetRequirementPayload
{
    public TagFilter TargetRequirementTags { get; }
}
