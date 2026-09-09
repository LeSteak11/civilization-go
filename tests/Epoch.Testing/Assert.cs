using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Epoch.Testing
{
    /// <summary>
    /// Minimal assertion surface with no third-party dependency.
    ///
    /// Why hand-rolled: the authoritative simulation and its conformance suite take no
    /// external package (see nuget.config). Unity's own PlayMode/EditMode tests use
    /// NUnit through the Unity Test Framework; those live under Assets/Tests and are a
    /// separate, thin smoke layer. If the project later wants xUnit here, this file and
    /// TestRunner are the only things that get deleted.
    /// </summary>
    public static class Assert
    {
        public static void True(bool condition, string because)
        {
            if (!condition)
            {
                throw new AssertionException("Expected true: " + because);
            }
        }

        public static void False(bool condition, string because)
        {
            if (condition)
            {
                throw new AssertionException("Expected false: " + because);
            }
        }

        public static void Equal<T>(T expected, T actual, string because)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new AssertionException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Expected <{0}> but was <{1}>: {2}",
                        expected,
                        actual,
                        because));
            }
        }

        public static void NotEqual<T>(T notExpected, T actual, string because)
        {
            if (EqualityComparer<T>.Default.Equals(notExpected, actual))
            {
                throw new AssertionException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Expected anything but <{0}>: {1}",
                        notExpected,
                        because));
            }
        }

        public static void SequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, string because)
        {
            List<T> e = expected.ToList();
            List<T> a = actual.ToList();
            if (!e.SequenceEqual(a))
            {
                throw new AssertionException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Sequences differ.{0}  expected: [{1}]{0}  actual:   [{2}]{0}  {3}",
                        Environment.NewLine,
                        string.Join(", ", e),
                        string.Join(", ", a),
                        because));
            }
        }

        public static void Empty(IEnumerable actual, string because)
        {
            List<object?> items = actual.Cast<object?>().ToList();
            if (items.Count != 0)
            {
                throw new AssertionException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Expected empty but found {0} item(s): [{1}]{2}  {3}",
                        items.Count,
                        string.Join(", ", items),
                        Environment.NewLine,
                        because));
            }
        }

        public static void Throws<TException>(Action action, string because)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }
            catch (Exception ex)
            {
                throw new AssertionException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Expected {0} but got {1}: {2}",
                        typeof(TException).Name,
                        ex.GetType().Name,
                        because));
            }

            throw new AssertionException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Expected {0} but nothing was thrown: {1}",
                    typeof(TException).Name,
                    because));
        }

        /// <summary>
        /// Marks a test as not yet implementable. It fails, loudly, with the milestone
        /// that owns it. The Technical Plan forbids unconditional placeholder
        /// assertions that silently pass (sec.2, "no unconditional placeholder assertion").
        /// </summary>
        public static void PendingMilestone(string milestone, string what) =>
            throw new AssertionException("PENDING " + milestone + ": " + what);
    }
}
