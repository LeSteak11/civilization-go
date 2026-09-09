using Epoch.Core.Numerics;

namespace Epoch.Core.Domain
{
    /// <summary>
    /// Core Spec sec.5.8.
    ///
    /// Invariant UI-1: <see cref="BasePower"/> is fixed at training time and is never
    /// retroactively updated by Age advancement — MR sec.2.2's argument that older units
    /// stay playable depends on it.
    /// </summary>
    public sealed record UnitInstance(
        StableId InstanceId,
        Side Owner,
        StableId CardId,
        UnitClass UnitClass,
        int BasePower,
        int Hp,
        LaneId LaneId,
        int TileIndex,
        bool HasReach,
        int TrainedOnTurn,
        int? ReachGuardUsedInEngagement)
    {
        /// <summary>[Lock 5].</summary>
        public const int MaxHp = 100;

        public bool IsAlive => Hp > 0;

        public FixedValue BasePowerFixed => FixedValue.FromInt(BasePower);

        public UnitInstance WithHp(int hp) => this with { Hp = hp };

        public UnitInstance WithTile(int tileIndex) => this with { TileIndex = tileIndex };
    }
}
