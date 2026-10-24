using System;
using System.Collections.Generic;
using lethal.core.domain.payloads.enums;

namespace lethal.core.domain.payloads;
public static class PayloadBehaviorMap
{
    private static readonly Dictionary<PayloadBehaviorTypes, (Type Record, PayloadBehaviorGroup Group)> Entries = new()
    {
        [PayloadBehaviorTypes.StatStaticPayload]       = (typeof(StatStaticPayload),       PayloadBehaviorGroup.Stat),
        [PayloadBehaviorTypes.StatOverridePayload]     = (typeof(StatOverridePayload),     PayloadBehaviorGroup.Stat),
        [PayloadBehaviorTypes.StatContributionPayload] = (typeof(StatContributionPayload), PayloadBehaviorGroup.Stat),
        [PayloadBehaviorTypes.StatTranslationPayload]  = (typeof(StatTranslationPayload),  PayloadBehaviorGroup.Stat),
        [PayloadBehaviorTypes.StatMirrorPayload]       = (typeof(StatMirrorPayload),       PayloadBehaviorGroup.Stat),
        [PayloadBehaviorTypes.StatScaledPayload]       = (typeof(StatScaledPayload),       PayloadBehaviorGroup.Stat),
        [PayloadBehaviorTypes.EffectivenessPayload]    = (typeof(EffectivenessPayload),    PayloadBehaviorGroup.Effectiveness),
    };

    public static Type RecordTypeFor(PayloadBehaviorTypes behavior) => Lookup(behavior).Record;
    public static PayloadBehaviorGroup GroupFor(PayloadBehaviorTypes behavior) => Lookup(behavior).Group;

    private static (Type Record, PayloadBehaviorGroup Group) Lookup(PayloadBehaviorTypes behavior) =>
        Entries.TryGetValue(behavior, out var entry)
            ? entry
            : throw new InvalidOperationException($"No payload record is mapped for behavior '{behavior}'.");

    public static void AssertComplete()
    {
        foreach (var value in Enum.GetValues<PayloadBehaviorTypes>())
            if (!Entries.ContainsKey(value))
                throw new InvalidOperationException($"PayloadBehaviorMap is missing an entry for '{value}'.");
    }
}