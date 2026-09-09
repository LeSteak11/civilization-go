using System;
using System.Collections.Generic;

namespace Epoch.Architecture.Tests
{
    /// <summary>
    /// The dependency rule, mechanised (Technical Plan sec.3.1, sec.9.1 "Architecture test",
    /// sec.14 "Engine APIs leak into Core").
    ///
    /// The deterministic Core knows no engine, filesystem, network, wall clock, locale,
    /// logging sink, native RNG or presentation class. This list is that sentence in
    /// executable form. Adding an entry is cheap; removing one requires an owner decision,
    /// because every entry here is a documented determinism threat.
    /// </summary>
    internal static class ForbiddenSurface
    {
        /// <summary>Assemblies the Core may reference. Anything else fails the build.</summary>
        internal static readonly HashSet<string> AllowedAssemblyReferences =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "netstandard",
                "System.Runtime",
            };


        /// <summary>
        /// Compiler-emitted attributes that land in System.Diagnostics whatever the source
        /// says. They carry no runtime behaviour and no determinism risk, so they are
        /// exempted by name rather than by weakening the namespace rule.
        /// </summary>
        internal static readonly HashSet<string> BenignTypes =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "System.Diagnostics.DebuggableAttribute",
                "System.Diagnostics.DebuggableAttribute+DebuggingModes",
                "System.Diagnostics.DebuggerBrowsableAttribute",
                "System.Diagnostics.DebuggerBrowsableState",
                "System.Diagnostics.DebuggerDisplayAttribute",
                "System.Diagnostics.DebuggerHiddenAttribute",
                "System.Diagnostics.DebuggerNonUserCodeAttribute",
                "System.Diagnostics.DebuggerStepThroughAttribute",
                "System.Diagnostics.CodeAnalysis.NullableAttribute",
                "System.Diagnostics.CodeAnalysis.NullableContextAttribute",
            };

        /// <summary>Namespace prefixes no authoritative assembly may reference.</summary>
        internal static readonly string[] ForbiddenNamespacePrefixes =
        {
            "UnityEngine",           // engine
            "UnityEditor",           // engine
            "System.IO",             // filesystem
            "System.Net",            // network
            "System.Threading",      // scheduling non-determinism
            "System.Diagnostics",    // wall clock, process, logging sink
            "System.Security",       // hashing belongs to Content/Application
            "System.Timers",         // wall clock
            "Microsoft.Extensions",  // logging / DI host concerns
        };

        /// <summary>Individual types that are forbidden even though their namespace is not.</summary>
        internal static readonly HashSet<string> ForbiddenTypes =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "System.Random",          // native RNG is forbidden [Lock 19]
                "System.DateTime",        // wall clock
                "System.DateTimeOffset",  // wall clock
                "System.TimeZoneInfo",    // locale / clock
                "System.Environment",     // host state
                "System.Console",         // logging sink
                "System.Guid",            // Guid.NewGuid is non-deterministic
                "System.Math",            // native exp() is forbidden in resolution [Lock 7]
                "System.MathF",           // as above, and float
            };

        /// <summary>
        /// No float anywhere in the authoritative path. Power is fixed-point hundredths
        /// (Core Spec sec.10.4, option (a); Technical Plan sec.3.2).
        /// </summary>
        internal static readonly HashSet<string> ForbiddenValueTypes =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "System.Single",
                "System.Double",
                "System.Decimal",
            };
    }
}
