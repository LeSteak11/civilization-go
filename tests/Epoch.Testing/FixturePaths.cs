using System;
using System.IO;

namespace Epoch.Testing
{
    /// <summary>
    /// Locates the repository's shared, engine-neutral fixture directory.
    /// Test projects may touch the filesystem; the Core may not.
    /// </summary>
    public static class FixturePaths
    {
        public static string Root
        {
            get
            {
                DirectoryInfo? dir = new DirectoryInfo(AppContext.BaseDirectory);
                while (dir is not null)
                {
                    string candidate = Path.Combine(dir.FullName, "fixtures");
                    if (Directory.Exists(candidate) && System.IO.File.Exists(Path.Combine(dir.FullName, "EPOCH.sln")))
                    {
                        return candidate;
                    }

                    dir = dir.Parent;
                }

                throw new InvalidOperationException(
                    "Could not locate the repository fixtures directory above " + AppContext.BaseDirectory);
            }
        }

        public static string File(string name) => Path.Combine(Root, name);

        /// <summary>
        /// The repository root, identified by EPOCH.sln. Used to reach the authored
        /// content manifest under _aiinfodocs/data, which is content authority and is
        /// deliberately not duplicated into the fixtures directory.
        /// </summary>
        public static string RepositoryRoot => Directory.GetParent(Root)!.FullName;

        public static string ContentManifest =>
            Path.Combine(RepositoryRoot, "_aiinfodocs", "data", "epoch_v1_content.json");
    }
}
