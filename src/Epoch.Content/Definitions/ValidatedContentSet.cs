namespace Epoch.Content.Definitions
{
    /// <summary>
    /// The frozen, validated content pool for the life of a run (Technical Plan sec.3.3).
    /// Loading, strict validation, canonical normalization and hashing land at M2;
    /// M0 declares the boundary only.
    /// </summary>
    public sealed record ValidatedContentSet(
        string SchemaVersion,
        string ContentVersion,
        string CompatibleRulesVersion,
        string ContentHash);

    /// <summary>The interface handed inward to the Core. It may not touch the filesystem.</summary>
    public interface ContentRepository
    {
        ValidatedContentSet Load(string contentVersion);
    }
}
