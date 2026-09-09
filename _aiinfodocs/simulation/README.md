# EPOCH V1 headless balance validator

Disposable Node.js analysis tooling for the V1 rules/content lock. It is not production game code and has no mobile, UI, backend, monetization, or asset dependencies.

## Run

From the repository root:

```powershell
node _aiinfodocs/simulation/simulate.js
```

The repaired runner executes 49 concrete conformance checks: every named Core Spec scenario plus movement-engagement, non-tile-3 combat, combat-site enumeration, simultaneous damage, diagnostic no-overflow, addressability, damage-table, and provenance checks. It also validates all 35 authored effects against an explicit set of 29 implemented effect sources, repeats the same 1,000 matches, checks RNG isolation, and performs 150 normalized authoritative side-swap comparisons. Balance output is written only if every gate passes.

The baseline uses 250 seeds for each of the 21 unordered policy pairings and runs both orientations: 10,500 complete matches. Each sensitivity has its own matching baseline cohort using exactly the same three policy pairings, seeds 1–200, and both orientations. The 12 matched-baseline/variant pairs add 28,800 matches, for 39,300 analyzed matches. Determinism checks execute another 2,000 executions (the same 1,000 inputs twice). No policy reads future offers.

Outputs are written to `results/` and the report is written to `_aiinfodocs/EPOCH_V1_Balance_Report.md`.

## Scope and limitations

The runner implements the authoritative 24-turn economy, shared indexed offers, per-side legality/PASS, Age costs, structures, three unit classes, movement, contested ownership/scoring, stack order, counter triangle, HIGHLAND, COAST, RIVER, REACH protection, simultaneous damage, overflow allocation, perks, and Keystones. Diagnostic sensitivity flags never mutate the locked baseline.

The simulator intentionally excludes presentation timing, replay UI, network snapshots, commerce, and production persistence. Policy decisions are deterministic functions of visible current state plus a dedicated non-authoritative policy RNG seeded from the match/policy identity. That RNG cannot be reached by offer or lane generation.

## Repair audit

- `moveBoth` now prevents a unit already co-occupying a tile with an enemy from moving.
- `combatSites` enumerates every co-occupied tile in all lanes; `resolveCombatSite` resolves each site, while REACH and HIGHLAND remain tile-3 scoped.
- `powerAtSite` mirrors the REACH support tile correctly: tile 2 for PLAYER and tile 4 for SNAPSHOT.
- `assertPairedSideSwap` compares normalized full authoritative side states after swapping policies.
- `validateEffectCoverage` fails on missing, stale, unsupported, or mismatched authored effects.
- `goldenTests` contains no unconditional placeholder assertion.
- Sensitivity rows are emitted beside `MATCHED_BASELINE_*` rows from identical cohorts.
