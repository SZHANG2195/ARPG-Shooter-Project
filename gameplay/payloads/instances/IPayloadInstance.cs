using lethal.core.domain.payloads;
using lethal.core.domain.tags;
using lethal.gameplay.payloads.sources;

namespace lethal.gameplay.payloads.instances;
public interface IPayloadInstance
{
    PayloadId PayloadId { get; }
    PayloadSourceHandle SourceHandle { get; }
    TagSet? SourceTags { get; }
}
