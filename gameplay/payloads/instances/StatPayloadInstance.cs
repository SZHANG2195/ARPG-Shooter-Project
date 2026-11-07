using lethal.core.domain.payloads;
using lethal.core.domain.tags;
using lethal.gameplay.payloads.sources;

namespace lethal.gameplay.payloads.instances;
public readonly record struct StatPayloadInstance(
    PayloadId PayloadId,
    double Primary,
    double? Secondary,
    PayloadSourceHandle SourceHandle,
    TagSet? SourceTags
) : IPayloadInstance;