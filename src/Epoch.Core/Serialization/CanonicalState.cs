using System;
using System.Collections.Generic;
using System.Text;
using Epoch.Core.Domain;
using Epoch.Core.Numerics;

namespace Epoch.Core.Serialization
{
    /// <summary>
    /// Canonical text for the authoritative state (Implementation Lock decision 3:
    /// canonical UTF-8 JSON plus SHA-256).
    ///
    /// The Core produces the **bytes**; it never hashes them. System.Security is a
    /// forbidden namespace here precisely so that hashing stays in Content/Application,
    /// and the architecture guard enforces it. <c>Epoch.Application.StateHasher</c> takes
    /// this string and digests it.
    ///
    /// Every rule below exists to make the output byte-identical on any platform:
    /// fixed field order, ordinal-sorted collections, no locale-sensitive formatting, and
    /// no floating point anywhere. Presentation and debug data never appear.
    /// </summary>
    public static class CanonicalState
    {
        public static string Write(RunState state)
        {
            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            StringBuilder text = new StringBuilder(1024);
            text.Append("{\"rulesVersion\":\"").Append(state.RulesVersion).Append('"');
            text.Append(",\"contentVersion\":\"").Append(state.ContentVersion).Append('"');
            text.Append(",\"contentPoolHash\":\"").Append(state.ContentPoolHash).Append('"');
            text.Append(",\"seed\":\"").Append(state.Seed.MasterSeed.Text).Append('"');
            text.Append(",\"turn\":").Append(FixedValue.IntToString(state.Turn));
            text.Append(",\"age\":").Append(FixedValue.IntToString(state.Age));
            text.Append(",\"phase\":\"").Append(state.Phase.ToString()).Append('"');

            text.Append(",\"lanes\":[");
            for (int i = 0; i < state.Lanes.Count; i++)
            {
                LaneState lane = state.Lanes[i];
                if (i > 0)
                {
                    text.Append(',');
                }

                text.Append("{\"id\":\"").Append(lane.Id.ToString()).Append('"');
                text.Append(",\"modifier\":\"").Append(lane.Modifier.ToString()).Append('"');
                text.Append(",\"ownedBy\":").Append(SideOrNull(lane.OwnedBy));
                text.Append(",\"prevHolder\":").Append(SideOrNull(lane.Tile3HolderPrevTurn));
                text.Append(",\"engagementId\":").Append(FixedValue.IntToString(lane.EngagementId));
                text.Append('}');
            }

            text.Append(']');

            text.Append(",\"player\":");
            WriteSide(text, state.Player);
            text.Append(",\"snapshot\":");
            WriteSide(text, state.Snapshot);

            text.Append(",\"result\":");
            if (state.Result is null)
            {
                text.Append("null");
            }
            else
            {
                MatchResult result = state.Result;
                text.Append("{\"playerScore\":").Append(FixedValue.IntToString(result.PlayerScore));
                text.Append(",\"snapshotScore\":").Append(FixedValue.IntToString(result.SnapshotScore));
                text.Append(",\"outcome\":\"").Append(result.Outcome.ToString()).Append('"');
                text.Append(",\"finalTurn\":").Append(FixedValue.IntToString(result.FinalTurn));
                text.Append('}');
            }

            text.Append('}');
            return text.ToString();
        }

        private static void WriteSide(StringBuilder text, PlayerState side)
        {
            text.Append("{\"side\":\"").Append(side.Side.ToString()).Append('"');
            text.Append(",\"growth\":").Append(FixedValue.IntToString(side.Growth));
            text.Append(",\"insight\":").Append(FixedValue.IntToString(side.Insight));
            text.Append(",\"score\":").Append(FixedValue.IntToString(side.Score));
            text.Append(",\"keystoneTaken\":").Append(side.KeystoneTaken ? "true" : "false");

            // Collections are sorted by stable id so that insertion order can never
            // leak into the hash.
            List<UnitInstance> units = new List<UnitInstance>(side.Units);
            units.Sort((a, b) => a.InstanceId.CompareTo(b.InstanceId));
            text.Append(",\"units\":[");
            for (int i = 0; i < units.Count; i++)
            {
                UnitInstance unit = units[i];
                if (i > 0)
                {
                    text.Append(',');
                }

                text.Append("{\"id\":\"").Append(unit.InstanceId.Value).Append('"');
                text.Append(",\"card\":\"").Append(unit.CardId.Value).Append('"');
                text.Append(",\"class\":\"").Append(unit.UnitClass.ToString()).Append('"');
                text.Append(",\"power\":").Append(FixedValue.IntToString(unit.BasePower));
                text.Append(",\"hp\":").Append(FixedValue.IntToString(unit.Hp));
                text.Append(",\"lane\":\"").Append(unit.LaneId.ToString()).Append('"');
                text.Append(",\"tile\":").Append(FixedValue.IntToString(unit.TileIndex));
                text.Append(",\"reach\":").Append(unit.HasReach ? "true" : "false");
                text.Append(",\"trained\":").Append(FixedValue.IntToString(unit.TrainedOnTurn));
                text.Append(",\"guard\":").Append(
                    unit.ReachGuardUsedInEngagement is null
                        ? "null"
                        : FixedValue.IntToString(unit.ReachGuardUsedInEngagement.Value));
                text.Append('}');
            }

            text.Append(']');

            List<StructureInstance> structures = new List<StructureInstance>(side.Structures);
            structures.Sort((a, b) => a.InstanceId.CompareTo(b.InstanceId));
            text.Append(",\"structures\":[");
            for (int i = 0; i < structures.Count; i++)
            {
                StructureInstance structure = structures[i];
                if (i > 0)
                {
                    text.Append(',');
                }

                text.Append("{\"id\":\"").Append(structure.InstanceId.Value).Append('"');
                text.Append(",\"card\":\"").Append(structure.CardId.Value).Append('"');
                text.Append(",\"tier\":").Append(FixedValue.IntToString(structure.Tier));
                text.Append(",\"yieldType\":\"").Append(structure.YieldType.ToString()).Append('"');
                text.Append(",\"yield\":").Append(FixedValue.IntToString(structure.YieldAmount));
                text.Append(",\"lane\":\"").Append(structure.LaneId.ToString()).Append('"');
                text.Append(",\"built\":").Append(FixedValue.IntToString(structure.BuiltOnTurn));
                text.Append('}');
            }

            text.Append(']');

            List<string> perkIds = new List<string>(side.Perks.Count);
            for (int i = 0; i < side.Perks.Count; i++)
            {
                perkIds.Add(side.Perks[i].CardId.Value);
            }

            perkIds.Sort(StringComparer.Ordinal);
            text.Append(",\"perks\":[");
            for (int i = 0; i < perkIds.Count; i++)
            {
                if (i > 0)
                {
                    text.Append(',');
                }

                text.Append('"').Append(perkIds[i]).Append('"');
            }

            text.Append("]}");
        }

        private static string SideOrNull(Side? side) =>
            side is null ? "null" : "\"" + side.Value.ToString() + "\"";
    }
}
