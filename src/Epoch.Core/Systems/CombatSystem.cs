using System;
using System.Collections.Generic;
using Epoch.Core.Combat;
using Epoch.Core.Domain;
using Epoch.Core.Effects;
using Epoch.Core.Numerics;

namespace Epoch.Core.Systems
{
    /// <summary>
    /// Steps 10-11 of the canonical turn, Core Spec sec.9.
    ///
    /// Combat resolves in **every** tile containing units of both sides - all five tiles
    /// of all three lanes, not only tile 3. (The repaired oracle records enumerating only
    /// tile 3 as a defect; the Technical Plan sec.6 requires ResolveAllCoOccupiedTiles.)
    /// REACH support and the HIGHLAND bonus remain tile-3 scoped.
    ///
    /// Both sides' damage is computed from the identical pre-damage state and applied
    /// together [Lock 6]; destroyed units are removed only after every lane has resolved,
    /// so a unit destroyed this turn still dealt its damage this turn (Q6).
    /// </summary>
    public static class CombatSystem
    {
        /// <summary>Soft stacking, MR sec.2.3. Applied in this order to the M22 stack ranking.</summary>
        private static readonly int[] StackMultipliersHundredths = { 100, 75, 50 };

        /// <summary>+40% Power, applied before the damage curve (MR sec.2.3).</summary>
        private const int CounterBonusHundredths = 140;

        /// <summary>HIGHLAND: defender gets +3 Power (MR sec.1.3), scoped to tile 3 [Lock 15].</summary>
        private const int HighlandBonus = 3;

        private static readonly FixedValue DeltaClampMin = FixedValue.FromInt(DamageTable.DeltaMin);

        private static readonly FixedValue DeltaClampMax = FixedValue.FromInt(DamageTable.DeltaMax);

        public static RunState Resolve(RunState state, List<CombatOutcome> outcomes, List<StableId> destroyed)
        {
            // Damage accumulates against the pre-damage state. No site sees another
            // site's damage, which is [Lock 6] made structural rather than remembered.
            Dictionary<string, int> damageByUnit = new Dictionary<string, int>(StringComparer.Ordinal);

            // R4: which REACH units spent their first-clash protection, and in which
            // engagement. Recorded here and written into state below, so that a second
            // clash in the SAME engagement finds them already spent (TV-13b).
            Dictionary<string, int> guardsConsumed = new Dictionary<string, int>(StringComparer.Ordinal);

            for (int l = 0; l < state.Lanes.Count; l++)
            {
                LaneState lane = state.Lanes[l];
                for (int tile = 1; tile <= RunState.TilesPerLane; tile++)
                {
                    List<UnitInstance> playerAt = UnitsAt(state.Player, lane.Id, tile);
                    List<UnitInstance> snapshotAt = UnitsAt(state.Snapshot, lane.Id, tile);

                    // C1: a combat site is a tile holding units of BOTH sides.
                    if (playerAt.Count == 0 || snapshotAt.Count == 0)
                    {
                        continue;
                    }

                    ResolveSite(state, lane, tile, playerAt, snapshotAt, damageByUnit, guardsConsumed, outcomes);
                }
            }

            // C9/C11/C12: apply all damage, then remove the dead, after every lane resolved.
            PlayerState player = ApplyDamage(state.Player, damageByUnit, guardsConsumed, destroyed);
            PlayerState snapshot = ApplyDamage(state.Snapshot, damageByUnit, guardsConsumed, destroyed);

            return state with { Player = player, Snapshot = snapshot };
        }

        private static void ResolveSite(
            RunState state,
            LaneState lane,
            int tile,
            List<UnitInstance> playerAt,
            List<UnitInstance> snapshotAt,
            Dictionary<string, int> damageByUnit,
            Dictionary<string, int> guardsConsumed,
            List<CombatOutcome> outcomes)
        {
            bool isContestedTile = tile == LaneState.ContestedTile;

            SideCombatants player = BuildCombatants(state, Side.PLAYER, lane, tile, playerAt, isContestedTile);
            SideCombatants snapshot = BuildCombatants(state, Side.SNAPSHOT, lane, tile, snapshotAt, isContestedTile);

            // C4: counter applied ONCE, between the two dominant classes [Lock 16].
            bool playerCounters = Counters(player.DominantClass, snapshot.DominantClass);
            bool snapshotCounters = Counters(snapshot.DominantClass, player.DominantClass);

            FixedValue playerPower = player.Power;
            FixedValue snapshotPower = snapshot.Power;

            if (playerCounters)
            {
                playerPower = playerPower.ScaleByHundredths(CounterBonusHundredths);
            }

            if (snapshotCounters)
            {
                snapshotPower = snapshotPower.ScaleByHundredths(CounterBonusHundredths);
            }

            // C5: HIGHLAND +3 to the side that exclusively held tile 3 LAST turn.
            // Null (disputed or unoccupied) means neither side gets it (EC-14, TV-10b).
            if (isContestedTile && lane.Modifier == LaneModifier.HIGHLAND)
            {
                if (lane.Tile3HolderPrevTurn == Side.PLAYER)
                {
                    playerPower += FixedValue.FromInt(HighlandBonus);
                }
                else if (lane.Tile3HolderPrevTurn == Side.SNAPSHOT)
                {
                    snapshotPower += FixedValue.FromInt(HighlandBonus);
                }
            }

            // C7: clamp, THEN round half away from zero. Rounding Delta rather than
            // damage is what keeps the authoritative path free of float [Lock 7].
            int playerDelta = (playerPower - snapshotPower)
                .Clamp(DeltaClampMin, DeltaClampMax)
                .RoundHalfAwayFromZeroToInt();
            int snapshotDelta = (snapshotPower - playerPower)
                .Clamp(DeltaClampMin, DeltaClampMax)
                .RoundHalfAwayFromZeroToInt();

            // C8: integer table reads, never a computation.
            int playerDamage = DamageTable.ForDelta(playerDelta);
            int snapshotDamage = DamageTable.ForDelta(snapshotDelta);

            // C10: allocate front-to-back with overflow, omitting REACH-protected units.
            Allocate(snapshotDamage, player, damageByUnit);
            Allocate(playerDamage, snapshot, damageByUnit);

            // R2/R4: every REACH supporter that joined this clash with an unspent guard
            // has now spent it. RC-1: at most once per engagement, never once per turn.
            MarkGuardsConsumed(player, guardsConsumed);
            MarkGuardsConsumed(snapshot, guardsConsumed);

            outcomes.Add(new CombatOutcome(
                lane.Id,
                tile,
                playerPower,
                snapshotPower,
                playerDelta,
                playerDamage,
                snapshotDamage,
                player.DominantClass,
                snapshot.DominantClass,
                playerCounters,
                snapshotCounters,
                player.ReachSupporters.Count,
                snapshot.ReachSupporters.Count));
        }

        /// <summary>
        /// C2/C3/C6: the participating units, their stack-ordered effective power, and the
        /// dominant class.
        ///
        /// A REACH unit on its own side's support tile (2 for PLAYER, 4 for SNAPSHOT -
        /// mirrored, per the oracle's own repair note) joins a tile-3 clash, but only when
        /// a living friendly frontline unit is already on tile 3 [Lock 17 R3]. Without one
        /// it contributes nothing and is not protected (TV-13c, EC-16).
        /// </summary>
        private static SideCombatants BuildCombatants(
            RunState state,
            Side side,
            LaneState lane,
            int tile,
            List<UnitInstance> frontline,
            bool isContestedTile)
        {
            PlayerState player = state.SideState(side);

            List<UnitInstance> reach = new List<UnitInstance>();
            if (isContestedTile && frontline.Count > 0)
            {
                List<UnitInstance> support = UnitsAt(player, lane.Id, player.ReachSupportTile);
                for (int i = 0; i < support.Count; i++)
                {
                    if (support[i].HasReach)
                    {
                        reach.Add(support[i]);
                    }
                }
            }

            List<UnitInstance> ranked = new List<UnitInstance>(frontline);
            ranked.AddRange(reach);

            // Per-unit Power effects are applied FIRST, then the stack is ranked on the
            // result. M22's "descending effective pre-stacking Power" means Power before
            // the stack multipliers, which is the modified value - and the owner-confirmed
            // decision (L) says these effects "may therefore change stack rank and the
            // dominant class selected at C3". Ranking on raw basePower would honour the
            // second half of that ruling and quietly drop the first.
            Dictionary<string, FixedValue> prestack = new Dictionary<string, FixedValue>(StringComparer.Ordinal);
            for (int i = 0; i < ranked.Count; i++)
            {
                prestack[ranked[i].InstanceId.Value] = UnitPowerWithEffects(player, ranked[i]);
            }

            Comparison<UnitInstance> byPrestackPower = (a, b) => ComparePrestack(a, b, prestack);
            ranked.Sort(byPrestackPower);

            FixedValue total = FixedValue.Zero;
            Dictionary<UnitClass, FixedValue> byClass = new Dictionary<UnitClass, FixedValue>();
            Dictionary<string, FixedValue> contribution = new Dictionary<string, FixedValue>(StringComparer.Ordinal);

            for (int i = 0; i < ranked.Count; i++)
            {
                UnitInstance unit = ranked[i];
                int multiplier = i < StackMultipliersHundredths.Length
                    ? StackMultipliersHundredths[i]
                    : 0;

                FixedValue basePower = prestack[unit.InstanceId.Value];
                FixedValue effective = basePower.ScaleByHundredths(multiplier);

                total += effective;
                contribution[unit.InstanceId.Value] = effective;
                byClass[unit.UnitClass] = byClass.TryGetValue(unit.UnitClass, out FixedValue running)
                    ? running + effective
                    : effective;
            }

            UnitClass? dominant = DominantClass(ranked, byClass, contribution);

            // LANE-scoped Power is one addition to the side's post-stacking total, not a
            // per-unit bonus: the authored rules text says "add N to the owner's effective
            // lane Power". It lands after the dominant class is chosen -- a lane-wide bonus
            // belongs to no class -- and before the counter at C4.
            List<ActiveEffect> lanePower = EffectSources.LanePowerEffects(player, lane);
            if (lanePower.Count > 0)
            {
                total = EffectResolution.Resolve(total, lanePower);
            }

            // Allocation order: frontline first in stack order, then REACH supporters
            // (TV-13b: "the front-line tile-3 unit remains first, then the now-eligible
            // tile-2 REACH unit").
            List<UnitInstance> frontlineOrdered = new List<UnitInstance>(frontline);
            frontlineOrdered.Sort(byPrestackPower);
            List<UnitInstance> reachOrdered = new List<UnitInstance>(reach);
            reachOrdered.Sort(byPrestackPower);

            return new SideCombatants(side, total, dominant, frontlineOrdered, reachOrdered, lane.EngagementId);
        }

        /// <summary>
        /// Per-unit ON_COMBAT_PRE effectivePower effects. Applying them before stacking is
        /// what lets a class-scoped perk change which class is dominant, which is the only
        /// reading under which UNIT_CLASS scope is meaningful at C3.
        /// </summary>
        private static FixedValue UnitPowerWithEffects(PlayerState player, UnitInstance unit)
        {
            List<ActiveEffect> effects = EffectSources.UnitPowerEffects(player, unit);

            return effects.Count == 0
                ? unit.BasePowerFixed
                : EffectResolution.Resolve(unit.BasePowerFixed, effects);
        }

        /// <summary>
        /// C3 [Lock 16]: the class contributing the most post-stacking, pre-counter Power.
        /// Ties break by strongest contributing unit, then earliest trainedOnTurn, then
        /// stable instanceId.
        /// </summary>
        private static UnitClass? DominantClass(
            List<UnitInstance> ranked,
            Dictionary<UnitClass, FixedValue> byClass,
            Dictionary<string, FixedValue> contribution)
        {
            if (ranked.Count == 0)
            {
                return null;
            }

            UnitClass? best = null;
            FixedValue bestTotal = FixedValue.Zero;
            UnitInstance? bestUnit = null;

            // Iterate the ranked list, not the dictionary: never enumerate a hash map
            // where order could affect state (Technical Plan sec.3.2).
            for (int i = 0; i < ranked.Count; i++)
            {
                UnitClass unitClass = ranked[i].UnitClass;
                FixedValue classTotal = byClass[unitClass];

                if (best is null || classTotal > bestTotal)
                {
                    best = unitClass;
                    bestTotal = classTotal;
                    bestUnit = StrongestOf(ranked, unitClass, contribution);
                    continue;
                }

                if (unitClass == best.Value || classTotal != bestTotal)
                {
                    continue;
                }

                UnitInstance challenger = StrongestOf(ranked, unitClass, contribution)!;
                if (CompareStackOrder(challenger, bestUnit!) < 0)
                {
                    best = unitClass;
                    bestUnit = challenger;
                }
            }

            return best;
        }

        private static UnitInstance? StrongestOf(
            List<UnitInstance> ranked,
            UnitClass unitClass,
            Dictionary<string, FixedValue> contribution)
        {
            UnitInstance? best = null;
            for (int i = 0; i < ranked.Count; i++)
            {
                if (ranked[i].UnitClass != unitClass)
                {
                    continue;
                }

                if (best is null)
                {
                    best = ranked[i];
                    continue;
                }

                FixedValue candidate = contribution[ranked[i].InstanceId.Value];
                FixedValue incumbent = contribution[best.InstanceId.Value];
                if (candidate > incumbent ||
                    (candidate == incumbent && CompareStackOrder(ranked[i], best) < 0))
                {
                    best = ranked[i];
                }
            }

            return best;
        }

        /// <summary>MR sec.2.3: SWORD beats SPEAR; SPEAR beats HORSE; HORSE beats SWORD.</summary>
        internal static bool Counters(UnitClass? attacker, UnitClass? defender)
        {
            if (attacker is null || defender is null)
            {
                return false;
            }

            switch (attacker.Value)
            {
                case UnitClass.SWORD:
                    return defender.Value == UnitClass.SPEAR;
                case UnitClass.SPEAR:
                    return defender.Value == UnitClass.HORSE;
                case UnitClass.HORSE:
                    return defender.Value == UnitClass.SWORD;
                default:
                    return false;
            }
        }

        /// <summary>
        /// C10 [Final Lock 1]: front-to-back with overflow. A REACH unit whose guard is
        /// still unused for this engagement is omitted entirely for this clash, and any
        /// damage that would have reached it is simply not allocated (GT-21: "10 damage
        /// remains unallocated").
        /// </summary>
        private static void Allocate(int damage, SideCombatants side, Dictionary<string, int> damageByUnit)
        {
            if (damage <= 0)
            {
                return;
            }

            int remaining = AllocateTo(side.Frontline, damage, damageByUnit, onlyGuardSpent: false, side.EngagementId);
            AllocateTo(side.ReachSupporters, remaining, damageByUnit, onlyGuardSpent: true, side.EngagementId);
        }

        private static int AllocateTo(
            List<UnitInstance> units,
            int remaining,
            Dictionary<string, int> damageByUnit,
            bool onlyGuardSpent,
            int engagementId)
        {
            for (int i = 0; i < units.Count && remaining > 0; i++)
            {
                UnitInstance unit = units[i];

                // R2/R4: protection is consumed once per engagement, not once per turn.
                // A REACH supporter is a valid target only once its guard has already been
                // spent in THIS engagement.
                if (onlyGuardSpent && unit.ReachGuardUsedInEngagement != engagementId)
                {
                    continue;
                }

                string key = unit.InstanceId.Value;
                damageByUnit.TryGetValue(key, out int already);
                int capacity = unit.Hp - already;
                if (capacity <= 0)
                {
                    continue;
                }

                int applied = remaining < capacity ? remaining : capacity;
                damageByUnit[key] = already + applied;
                remaining -= applied;
            }

            return remaining;
        }

        private static void MarkGuardsConsumed(SideCombatants side, Dictionary<string, int> guardsConsumed)
        {
            for (int i = 0; i < side.ReachSupporters.Count; i++)
            {
                UnitInstance supporter = side.ReachSupporters[i];
                if (supporter.ReachGuardUsedInEngagement != side.EngagementId)
                {
                    guardsConsumed[supporter.InstanceId.Value] = side.EngagementId;
                }
            }
        }

        private static PlayerState ApplyDamage(
            PlayerState side,
            Dictionary<string, int> damageByUnit,
            Dictionary<string, int> guardsConsumed,
            List<StableId> destroyed)
        {
            List<UnitInstance> survivors = new List<UnitInstance>(side.Units.Count);
            for (int i = 0; i < side.Units.Count; i++)
            {
                UnitInstance unit = side.Units[i];
                if (guardsConsumed.TryGetValue(unit.InstanceId.Value, out int engagementId))
                {
                    unit = unit with { ReachGuardUsedInEngagement = engagementId };
                }

                if (damageByUnit.TryGetValue(unit.InstanceId.Value, out int damage) && damage > 0)
                {
                    unit = unit.WithHp(unit.Hp - damage);
                }

                if (unit.IsAlive)
                {
                    survivors.Add(unit);
                }
                else
                {
                    destroyed.Add(unit.InstanceId);
                }
            }

            return side with { Units = survivors };
        }

        /// <summary>
        /// M22 [Final Lock 1]: descending pre-stacking Power, then earliest
        /// trainedOnTurn, then ascending stable instanceId. Total by construction, so
        /// List.Sort's instability cannot leak into the result.
        /// </summary>
        internal static int CompareStackOrder(UnitInstance a, UnitInstance b) =>
            CompareRanked(b.BasePower.CompareTo(a.BasePower), a, b);

        private static int ComparePrestack(
            UnitInstance a,
            UnitInstance b,
            Dictionary<string, FixedValue> prestack) =>
            CompareRanked(
                prestack[b.InstanceId.Value].CompareTo(prestack[a.InstanceId.Value]), a, b);

        private static int CompareRanked(int byPower, UnitInstance a, UnitInstance b)
        {
            if (byPower != 0)
            {
                return byPower;
            }

            int byTurn = a.TrainedOnTurn.CompareTo(b.TrainedOnTurn);
            return byTurn != 0 ? byTurn : a.InstanceId.CompareTo(b.InstanceId);
        }

        private static List<UnitInstance> UnitsAt(PlayerState side, LaneId lane, int tile)
        {
            List<UnitInstance> found = new List<UnitInstance>();
            for (int i = 0; i < side.Units.Count; i++)
            {
                UnitInstance unit = side.Units[i];
                if (unit.LaneId == lane && unit.TileIndex == tile && unit.IsAlive)
                {
                    found.Add(unit);
                }
            }

            return found;
        }

        private readonly struct SideCombatants
        {
            internal SideCombatants(
                Side side,
                FixedValue power,
                UnitClass? dominantClass,
                List<UnitInstance> frontline,
                List<UnitInstance> reachSupporters,
                int engagementId)
            {
                Side = side;
                Power = power;
                DominantClass = dominantClass;
                Frontline = frontline;
                ReachSupporters = reachSupporters;
                EngagementId = engagementId;
            }

            internal Side Side { get; }

            internal FixedValue Power { get; }

            internal UnitClass? DominantClass { get; }

            internal List<UnitInstance> Frontline { get; }

            internal List<UnitInstance> ReachSupporters { get; }

            internal int EngagementId { get; }
        }
    }

    /// <summary>One resolved combat site, for the replay event stream and telemetry.</summary>
    public sealed record CombatOutcome(
        LaneId Lane,
        int Tile,
        FixedValue PlayerPower,
        FixedValue SnapshotPower,
        int PlayerDelta,
        int PlayerDamageDealt,
        int SnapshotDamageDealt,
        UnitClass? PlayerDominantClass,
        UnitClass? SnapshotDominantClass,
        bool PlayerCountered,
        bool SnapshotCountered,
        int PlayerReachSupporters,
        int SnapshotReachSupporters);
}
