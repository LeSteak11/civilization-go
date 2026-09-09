namespace Epoch.Core
{
    // Types the Core contracts reference but that are owned by later milestones.
    // Declared here so the M0 contract surface compiles and the architecture guard
    // has a real assembly to inspect. Each is replaced, not extended, at its milestone.

    /// <summary>Owned by Epoch.Content at M2; the Core only ever sees a validated, frozen set.</summary>
    public sealed record ValidatedContentSetRef(string ContentVersion, string ContentHash);

    /// <summary>Owned by Epoch.Application at M3 (Core Spec sec.5.14).</summary>
    public sealed record OpponentSnapshotRef(string SnapshotId, string RulesVersion, string ContentVersion);

    /// <summary>The versioned 81-entry integer damage table, delta in [-40, +40]. Populated at M1 (Core Spec sec.9.3).</summary>
    public sealed record DamageTableRef(string TableVersion);
}
