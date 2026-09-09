using Epoch.Core.Content;

namespace Epoch.Content.Definitions
{
    /// <summary>
    /// The interface handed inward to the Core (Technical Plan sec.3.3).
    ///
    /// The implementation may touch the filesystem or Unity's asset APIs; what it returns
    /// may not. That is the whole point of the boundary: a
    /// <see cref="ValidatedContentSet"/> is an immutable value, frozen for the life of a
    /// run, and the Core never learns where it came from.
    ///
    /// The set itself is defined in <c>Epoch.Core.Content</c> rather than here, because
    /// the Core must read card definitions and cannot reference this assembly
    /// (Implementation Lock decision G).
    /// </summary>
    public interface ContentRepository
    {
        ValidatedContentSet Load(string contentVersion);
    }
}
