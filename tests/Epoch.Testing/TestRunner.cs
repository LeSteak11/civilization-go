using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Epoch.Testing
{
    /// <summary>
    /// Deterministic reflection-based runner. Discovery order is stable and ordinal so
    /// that two runs of the same suite print the same lines in the same order.
    /// Returns a process exit code: 0 all passed, 1 one or more failed.
    /// </summary>
    public static class TestRunner
    {
        public static int Run(string suiteName, Assembly assembly)
        {
            List<(TestCaseAttribute Meta, MethodInfo Method)> cases = assembly
                .GetTypes()
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
                .Select(m => (Meta: m.GetCustomAttribute<TestCaseAttribute>(), Method: m))
                .Where(x => x.Meta is not null)
                .Select(x => (Meta: x.Meta!, x.Method))
                .OrderBy(x => x.Method.DeclaringType!.FullName, StringComparer.Ordinal)
                .ThenBy(x => x.Meta.Id, StringComparer.Ordinal)
                .ThenBy(x => x.Method.Name, StringComparer.Ordinal)
                .ToList();

            Console.WriteLine("=== " + suiteName + " (" + cases.Count.ToString(CultureInfo.InvariantCulture) + " cases) ===");

            int passed = 0;
            List<string> failures = new List<string>();
            Stopwatch clock = Stopwatch.StartNew();

            foreach ((TestCaseAttribute meta, MethodInfo method) in cases)
            {
                string label = meta.Id + "  " + meta.Name;
                try
                {
                    method.Invoke(null, null);
                    passed++;
                    Console.WriteLine("  PASS  " + label);
                }
                catch (TargetInvocationException tie)
                {
                    Exception inner = tie.InnerException ?? tie;
                    Console.WriteLine("  FAIL  " + label);
                    Console.WriteLine("        " + inner.Message.Replace(Environment.NewLine, Environment.NewLine + "        ", StringComparison.Ordinal));
                    failures.Add(label + " :: " + inner.Message);
                }
            }

            clock.Stop();

            Console.WriteLine(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "--- {0}: {1} passed, {2} failed, {3} ms ---",
                    suiteName,
                    passed,
                    failures.Count,
                    clock.ElapsedMilliseconds));

            if (cases.Count == 0)
            {
                Console.WriteLine("EMPTY SUITE - failing so an unwired suite can never look green.");
                return 1;
            }

            return failures.Count == 0 ? 0 : 1;
        }
    }
}
