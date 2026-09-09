using System.IO;
using Epoch.Content.Loading;
using Epoch.Core.Content;
using Epoch.Core.StateMachine;

namespace Epoch.Testing
{
    /// <summary>
    /// Loads the authored manifest for tests, through the **production** loader.
    ///
    /// At M1 this class parsed the JSON itself, because the strict loader did not exist
    /// yet. It no longer does: everything except locating the file on disk is
    /// <see cref="ContentLoader"/>, so every test that runs a match is also a test that
    /// production loading, validation, effect coverage and canonical hashing all pass on
    /// the real content. Reading the file is the host's job, which here is the test.
    ///
    /// The result is cached because loading validates the whole pool and hashes it, and
    /// the content is immutable for the life of the process.
    /// </summary>
    public static class ContentFixtureLoader
    {
        private static ValidatedContentSet? _cached;

        public static ValidatedContentSet Load() => _cached ??= LoadFrom(FixturePaths.ContentManifest);

        public static ValidatedContentSet LoadFrom(string path) =>
            ContentLoader.Load(File.ReadAllText(path), RunFactory.RulesVersion);
    }
}
