using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Epoch.Testing;

namespace Epoch.Architecture.Tests
{
    /// <summary>
    /// These tests read Epoch.Core.dll as metadata rather than trusting the csproj,
    /// so a transitive or accidental dependency is caught even if nobody edits the
    /// project file. This suite is the gate that keeps the Core portable to Unity,
    /// to a headless CI runner, and to the Node-oracle comparison unchanged.
    /// </summary>
    public static class CoreIsolationTests
    {
        private const string CoreAssemblyFile = "Epoch.Core.dll";

        [TestCase("ARCH-01", "Epoch.Core references only netstandard")]
        public static void CoreReferencesOnlyAllowedAssemblies()
        {
            List<string> referenced = ReadReferencedAssemblies(CorePath());

            List<string> disallowed = referenced
                .Where(name => !ForbiddenSurface.AllowedAssemblyReferences.Contains(name))
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();

            Assert.Empty(disallowed, "Epoch.Core must reference only " + string.Join(", ", ForbiddenSurface.AllowedAssemblyReferences));
        }

        [TestCase("ARCH-02", "Epoch.Core references no forbidden namespace or type")]
        public static void CoreTouchesNoForbiddenSurface()
        {
            List<string> typeRefs = ReadTypeReferences(CorePath());

            List<string> violations = typeRefs
                .Where(IsForbidden)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();

            Assert.Empty(violations, "the deterministic Core may not touch engine, filesystem, network, clock, RNG or logging APIs");
        }

        [TestCase("ARCH-03", "Epoch.Core exposes no floating-point in its public surface")]
        public static void CorePublicSurfaceIsFloatFree()
        {
            Assembly core = typeof(Epoch.Core.Domain.RunState).Assembly;
            List<string> violations = new List<string>();

            foreach (Type type in core.GetExportedTypes())
            {
                foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                {
                    Flag(violations, type, property.Name, property.PropertyType);
                }

                foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                {
                    Flag(violations, type, field.Name, field.FieldType);
                }

                foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                {
                    Flag(violations, type, method.Name + "()", method.ReturnType);
                    foreach (ParameterInfo parameter in method.GetParameters())
                    {
                        Flag(violations, type, method.Name + "(" + parameter.Name + ")", parameter.ParameterType);
                    }
                }
            }

            Assert.Empty(
                violations.Distinct(StringComparer.Ordinal).OrderBy(v => v, StringComparer.Ordinal),
                "Power and every magnitude are fixed-point integers; float must not reach the authoritative path");
        }

        private static void Flag(List<string> violations, Type owner, string member, Type candidate)
        {
            Type probe = candidate.IsByRef || candidate.IsArray
                ? candidate.GetElementType() ?? candidate
                : candidate;

            if (probe.FullName is string name && ForbiddenSurface.ForbiddenValueTypes.Contains(name))
            {
                violations.Add(owner.FullName + "." + member + " : " + name);
            }
        }

        private static bool IsForbidden(string fullTypeName)
        {
            if (ForbiddenSurface.BenignTypes.Contains(fullTypeName))
            {
                return false;
            }

            if (ForbiddenSurface.ForbiddenTypes.Contains(fullTypeName))
            {
                return true;
            }

            foreach (string prefix in ForbiddenSurface.ForbiddenNamespacePrefixes)
            {
                if (fullTypeName.StartsWith(prefix + ".", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static string CorePath()
        {
            string path = Path.Combine(AppContext.BaseDirectory, CoreAssemblyFile);
            Assert.True(File.Exists(path), CoreAssemblyFile + " must sit beside the test binary; found no file at " + path);
            return path;
        }

        private static List<string> ReadReferencedAssemblies(string assemblyPath)
        {
            using FileStream stream = File.OpenRead(assemblyPath);
            using PEReader pe = new PEReader(stream);
            MetadataReader reader = pe.GetMetadataReader();

            return reader.AssemblyReferences
                .Select(handle => reader.GetString(reader.GetAssemblyReference(handle).Name))
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();
        }

        private static List<string> ReadTypeReferences(string assemblyPath)
        {
            using FileStream stream = File.OpenRead(assemblyPath);
            using PEReader pe = new PEReader(stream);
            MetadataReader reader = pe.GetMetadataReader();

            List<string> names = new List<string>();
            foreach (TypeReferenceHandle handle in reader.TypeReferences)
            {
                TypeReference reference = reader.GetTypeReference(handle);
                string ns = reader.GetString(reference.Namespace);
                string name = reader.GetString(reference.Name);
                names.Add(string.IsNullOrEmpty(ns) ? name : ns + "." + name);
            }

            return names;
        }
    }
}
