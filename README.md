# EPOCH

"Civilization Go" — a 24-turn, roughly three-minute asynchronous mobile strategy match.

This repository is at **M0: project skeleton and automated tests**. There is no playable
game yet, and by design there is no gameplay logic — M1 builds the deterministic headless
match.

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
src/Epoch.Application   run orchestration, Snapshot, replay, persistence (M3)
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
