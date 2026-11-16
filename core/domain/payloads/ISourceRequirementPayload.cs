using lethal.core.domain.tags;

namespace lethal.core.domain.payloads;
public interface ISourceRequirementPayload
{
    public TagFilter SourceRequirementTags { get; }
}