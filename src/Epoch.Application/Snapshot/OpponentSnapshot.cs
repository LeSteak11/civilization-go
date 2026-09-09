using System;
using System.Collections.Generic;
using Epoch.Core.Content;
using Epoch.Core.Domain;

namespace Epoch.Application.Snapshot
{
    /// <summary>
    /// A recorded opponent (Core Spec sec.5.14).
    ///
    /// It stores only the seed and the 24 selections. No board state, no unit positions,
    /// no resources - all of that is recomputed by replaying the selections against the
    /// seed (sec.10.5), which is what makes a shareable seed code work at all.
    /// </summary>
    public sealed record OpponentSnapshot(
        string SnapshotId,
        string RulesVersion,
        string ContentVersion,
        SeedCode Seed,
        IReadOnlyList<Selection> Selections,
        int RecordedScore);

    /// <summary>Why a snapshot cannot be used for a run.</summary>
    public enum SnapshotRejection
    {
        None,
        RulesVersionMismatch,
        ContentVersionMismatch,
        WrongSelectionCount,
    }

    /// <summary>
    /// Version guarding and replay, [Lock 18].
    ///
    /// A snapshot whose rulesVersion or contentVersion differs from the run is **not
    /// selectable**: the run is never created. There is no migration path and old
    /// snapshots are archived rather than upgraded, so refusing is the whole behaviour.
    /// </summary>
    public static class SnapshotLibrary
    {
        public static SnapshotRejection CheckSelectable(
            OpponentSnapshot snapshot,
            string rulesVersion,
            ValidatedContentSet content)
        {
            if (!string.Equals(snapshot.RulesVersion, rulesVersion, StringComparison.Ordinal))
            {
                return SnapshotRejection.RulesVersionMismatch;
            }

            if (!string.Equals(snapshot.ContentVersion, content.ContentVersion, StringComparison.Ordinal))
            {
                return SnapshotRejection.ContentVersionMismatch;
            }

            return snapshot.Selections.Count != RunState.TurnsPerRun
                ? SnapshotRejection.WrongSelectionCount
                : SnapshotRejection.None;
        }

        public static bool IsSelectable(OpponentSnapshot snapshot, string rulesVersion, ValidatedContentSet content) =>
            CheckSelectable(snapshot, rulesVersion, content) == SnapshotRejection.None;
    }
}
