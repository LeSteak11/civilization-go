using System.Collections.Generic;
using Epoch.Core.Domain;

namespace Epoch.Core.Systems
{
    /// <summary>
    /// Step 8 of the canonical turn, Core Spec sec.8.2, [Lock 11].
    ///
    /// Movement is **simultaneous** for both sides and processed **one tile-step at a
    /// time**. Blocking is re-checked before each step against **pre-step occupancy**,
    /// which is the subtle part: two units advancing toward each other across an empty
    /// tile both enter it (Q9, GT-05), and that tile becomes a combat site. Units never
    /// pass through or swap past an enemy.
    ///
    /// COAST grants a second step (M12); a unit blocked on step 1 forfeits step 2.
    /// **No combat occurs between steps** (M13) — every clash waits for step 10.
    ///
    /// The engaged-unit guard: a unit already sharing a tile with an enemy does not move.
    /// This is the defect the repaired Node oracle calls out in its own audit
    /// ("moveBoth now prevents a unit already co-occupying a tile with an enemy from
    /// moving") and the Technical Plan sec.6 requires of MovementSystem.
    /// </summary>
    public static class MovementSystem
    {
        public static RunState Resolve(RunState state, List<UnitMove> moves)
        {
            List<UnitInstance> player = new List<UnitInstance>(state.Player.Units);
            List<UnitInstance> snapshot = new List<UnitInstance>(state.Snapshot.Units);

            // Every lane takes its own number of steps; COAST takes two.
            int maxSteps = 1;
            for (int i = 0; i < state.Lanes.Count; i++)
            {
                if (state.Lanes[i].PushDistance > maxSteps)
                {
                    maxSteps = state.Lanes[i].PushDistance;
                }
            }

            for (int step = 0; step < maxSteps; step++)
            {
                StepOnce(state, player, snapshot, step, moves);
            }

            return state with
            {
                Player = state.Player with { Units = player },
                Snapshot = state.Snapshot with { Units = snapshot },
            };
        }

        private static void StepOnce(
            RunState state,
            List<UnitInstance> player,
            List<UnitInstance> snapshot,
            int step,
            List<UnitMove> moves)
        {
            // Occupancy is sampled ONCE, before any unit in this step moves. Every
            // blocking decision in this step reads this snapshot, never a partially
            // updated board -- that is what makes the step simultaneous.
            HashSet<int> playerOccupied = Occupancy(player);
            HashSet<int> snapshotOccupied = Occupancy(snapshot);

            // Both calls read the frozen sets above. Neither writes to them: a side that
            // updated occupancy as it moved would let its own moves block the other side,
            // which is the difference between simultaneous movement and sequential.
            MoveSide(state, player, snapshotOccupied, step, +1, moves);
            MoveSide(state, snapshot, playerOccupied, step, -1, moves);
        }

        private static void MoveSide(
            RunState state,
            List<UnitInstance> units,
            HashSet<int> enemyOccupied,
            int step,
            int direction,
            List<UnitMove> moves)
        {
            for (int i = 0; i < units.Count; i++)
            {
                UnitInstance unit = units[i];
                LaneState lane = state.Lane(unit.LaneId);

                // A lane only takes as many steps as its push distance allows.
                if (step >= lane.PushDistance)
                {
                    continue;
                }

                // Engaged-unit guard: already sharing this tile with an enemy.
                if (enemyOccupied.Contains(Key(unit.LaneId, unit.TileIndex)))
                {
                    continue;
                }

                int target = unit.TileIndex + direction;

                // M11: a unit at the far end tile does not advance further.
                if (target < 1 || target > RunState.TilesPerLane)
                {
                    continue;
                }

                // M9/M10: never step onto a tile an enemy held at the start of this step,
                // which also makes a swap impossible in both directions.
                if (enemyOccupied.Contains(Key(unit.LaneId, target)))
                {
                    continue;
                }

                units[i] = unit.WithTile(target);
                moves.Add(new UnitMove(unit.InstanceId, unit.Owner, unit.LaneId, unit.TileIndex, target));
            }
        }

        private static HashSet<int> Occupancy(List<UnitInstance> units)
        {
            HashSet<int> occupied = new HashSet<int>();
            for (int i = 0; i < units.Count; i++)
            {
                occupied.Add(Key(units[i].LaneId, units[i].TileIndex));
            }

            return occupied;
        }

        private static int Key(LaneId lane, int tile) => ((int)lane * 16) + tile;
    }

    /// <summary>One tile-step actually taken, for the replay event stream.</summary>
    public readonly record struct UnitMove(StableId UnitId, Side Owner, LaneId Lane, int FromTile, int ToTile);
}
