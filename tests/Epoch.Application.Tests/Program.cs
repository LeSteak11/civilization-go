using System.Reflection;
using Epoch.Application.Tests;
using Epoch.Testing;

// "--emit-goldens" regenerates fixtures/golden/m1_replay_hashes.json and exits.
//
// Regeneration is deliberately a separate, explicit command rather than something the
// test does when it fails. Expected fixtures are never updated automatically: a change
// here requires a documented rules or content version change, exactly as the oracle
// fixture contract states (fixtures/oracle/README.md rule 4).
if (args.Length > 0 && args[0] == "--emit-goldens")
{
    GoldenReplayTests.EmitGoldens();
    return 0;
}

return TestRunner.Run("Epoch.Application.Tests", Assembly.GetExecutingAssembly());
