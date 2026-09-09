using System;

namespace Epoch.Testing
{
    /// <summary>
    /// Marks a public static parameterless method as a test case.
    /// Discovery is by reflection over the entry assembly, in a deterministic order
    /// (declaring type name, then method name, both ordinal).
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class TestCaseAttribute : Attribute
    {
        public TestCaseAttribute(string id, string name)
        {
            Id = id;
            Name = name;
        }

        /// <summary>Spec identifier where one exists, e.g. "GT-15", "TV-09", "ARCH-01".</summary>
        public string Id { get; }

        public string Name { get; }
    }
}
