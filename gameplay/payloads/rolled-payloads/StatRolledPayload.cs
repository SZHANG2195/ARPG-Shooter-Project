using lethal.core.domain.payloads;

namespace lethal.gameplay.payloads.rolled_payloads;
public readonly record struct StatRolledPayload(
    PayloadId Id,
    //anything less than 0 is evaluated against the minimum roll, anything greater than 1 is evaluated against the maximum roll
    double PrimaryRoll,
    double PrimaryValue,
    double SecondaryRoll,
    double SecondaryValue
) : IRolledPayload;
