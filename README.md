# EPOCH

"Civilization Go" — a 24-turn, roughly three-minute asynchronous mobile strategy match.

This repository is at **M1: deterministic headless match**. A complete 24-turn match
resolves deterministically with no engine, no UI and no I/O in the authoritative path.
There is no playable game yet — M2 adds strict content loading, M3 Snapshot and replay,
M4 the first UI.

## Authority order

Read in this order; earlier wins:

1. `_aiinfodocs/EPOCH_Core_Gameplay_Spec_v1.md` — authoritative V1 simulation behaviour (rules `1.2.0-v1-final`)
2. `_aiinfodocs/data/epoch_v1_content.json` — authoritative prototype content (`1.0.1-v1`)
3. `_aiinfodocs/EPOCH_V1_Content_Manifest.md` — human-readable content rationale
4. `_aiinfodocs/EPOCH_V1_Balance_Report.md` — **observational evidence only**
5. `_aiinfodocs/EPOCH-master-reference.md` — authority where the Core Spec does not supersede it
6. `_aiinfodocs/hooked-player-playbook.md` — supporting research only

The Node simulator under `_aiinfodocs/simulation` is a **reference oracle**, never runtime
code. Where oracle and specification disagree, the specification wins.

## Layout

```
src/Epoch.Core          deterministic simulation - no engine, filesystem, network, clock, or native RNG
src/Epoch.Content       strict JSON loading, validation, canonical hashing (M2)
src/Epoch.Application   run orchestration, state hashing, headless match; Snapshot and replay land at M3
src/Epoch.Debug         inspection and telemetry, outside the authoritative path (M5)
tests/                  one console suite per concern; see run-tests.sh
fixtures/               engine-neutral normative fixtures shared with the Node oracle
docs/                   implementation decisions
```

Dependencies point inward only. `Epoch.Core` references nothing but `netstandard`, and a
test enforces that by reading the compiled assembly's metadata.

## Requirements

.NET SDK 8.0. Nothing else — the solution takes no NuGet package.

## Running the tests

```bash
./run-tests.sh          # macOS / Linux
./run-tests.ps1         # Windows
```

Every suite is a console app that exits non-zero on failure. There is no test explorer
integration; see `docs/IMPLEMENTATION_LOCK.md` decision (C) for why, and what it would
cost to change.

## What M0 guarantees

- `Epoch.Core` builds with no engine reference and no floating-point in its public surface.
- The 81-entry damage table reproduces every reference value the specification prints.
- The Age table, offer weights and PRNG vectors are imported and checked against the spec.
- The 48-entry content manifest passes an independent integrity audit.
- The Node oracle is pinned by SHA-256, so future comparison fixtures are traceable.

## What M1 adds

A complete 24-turn headless match, and the tests that make it a contract:

- **PCG32 / SplitMix64** with indexed offer addressing, reproducing every normative PRNG
  vector in Core Spec §10.2. No language- or engine-native RNG anywhere.
- **No float in the authoritative path.** Power is fixed-point hundredths; Δ Power is
  clamped and rounded half away from zero, then indexes the checked-in integer damage
  table. `System.Math` is banned from the Core outright.
- **The canonical turn order**: card → income → movement → combat → ownership → score →
  persist → Age check, with income and HIGHLAND both reading the previous turn's holder.
- **Combat at every co-occupied tile**, with soft stacking, the counter triangle, HIGHLAND,
  mirrored REACH support and protection, simultaneous damage and front-to-back overflow.
- **All fourteen combat test vectors** (TV-01 … TV-14) and the golden scenarios that
  depend on them.
- **Determinism**: identical inputs reproduce every per-turn state hash and the replay
  hash, in-process and — via committed golden fixtures — across processes and builds.

The Core still holds no gameplay *content* rules: costs, Power and tiers are read from the
locked Age table, and card definitions arrive as validated immutable values.

### Regenerating the golden fixtures

```bash
dotnet run --project tests/Epoch.Application.Tests -- --emit-goldens
```

Never do this to make a failing test pass. A change to `fixtures/golden/` means a rules or
content change and needs saying so out loud — the same rule the oracle fixtures carry.
