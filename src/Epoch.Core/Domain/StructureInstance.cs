using System.Collections.Generic;
using Epoch.Core.Effects;

namespace Epoch.Core.Domain
{
    /// <summary>
    /// Core Spec sec.5.9.
    ///
    /// Invariant SI-1: immutable after creation. Structures cannot be attacked and are
    /// never destroyed [Lock 9].
    /// Invariant SI-3: they do not occupy tiles and never block movement.
    /// </summary>
    public sealed record StructureInstance(
        StableId InstanceId,
        Side Owner,
        StableId CardId,
        int Tier,
        ResourceType YieldType,
        int YieldAmount,
        LaneId LaneId,
        int BuiltOnTurn,
        IReadOnlyList<ActiveEffect> Effects)
    {
        /// <summary>
        /// cost / yieldAmount, recorded for balance telemetry only and **never** used for
        /// runtime legality (V1 Build Economy Exception X5, Core Spec sec.6.7).
        /// Held in hundredths so the advisory figure stays float-free.
        /// </summary>
        public int PaybackTurnsHundredths { get; init; }
    }
}
