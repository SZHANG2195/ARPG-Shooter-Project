using lethal.core.domain.payloads.enums;
using lethal.gameplay.payloads.enums;

public static class PayloadSourceKinds
{
    public static PayloadTargetDomain? DomainOf(this SourceKind kind) => kind switch
    {
        SourceKind.Equipment   => PayloadTargetDomain.Equipment,
        SourceKind.Augment     => PayloadTargetDomain.Augment,
        SourceKind.Chip        => PayloadTargetDomain.Chip,
        SourceKind.PassiveNode => PayloadTargetDomain.PassiveTree,
        SourceKind.World       => PayloadTargetDomain.World,
        _                      => null 
    };
}
