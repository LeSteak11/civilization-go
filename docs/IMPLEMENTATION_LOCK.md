# EPOCH V1 — Implementation Lock

Records the engineering decisions the Technical Implementation Plan sec.16 requires
before coding. **These are not balance decisions.** No rule, Age table, content effect or
balance value is changed by anything in this file.

Status key: **DEFAULTED** = Claude chose a reversible default so M0 could proceed;
owner may overturn at any time. **OPEN** = still needs an owner answer, and the milestone
that will be blocked by it is named.

| # | Decision | Status | Value | Rationale / cost of changing later |
|---|---|---|---|---|
| 1 | Unity LTS version and licensing | **OPEN** — blocks M4 | — | Not needed for M0–M3, which are pure C#. Pin before any scene work. |
| 2 | Android/iOS floors and CI devices | **OPEN** — blocks M4/M6 | — | Affects nothing before a device build exists. |
| 3 | Canonical state serialization + hash | **DEFAULTED** | Canonical UTF-8 JSON + SHA-256 | The plan's own recommendation. Inspectable by a designer reading a replay; changing it later invalidates stored fixture hashes only, not code. |
| 4 | Fixed-point Power unit | **DEFAULTED** | Integer hundredths (`FixedValue.Hundredths`) | Core Spec sec.10.4 option (a), the safer of the two. Removes float from the authoritative path entirely; the architecture guard ARCH-03 enforces it. |
| 5 | Choice-window timeout | **DEFAULTED** | No timeout for local V1 | Plan recommendation. A timeout must never create a voluntary PASS. |
| 6 | Initial Snapshot source | **DEFAULTED** | Both — scripted fixtures for conformance, deterministic local bot for playtesting | Plan recommendation. Decided for real at M3. |
| 7 | Checkpoint policy | **DEFAULTED** | After every completed turn | Plan recommendation. Mid-animation resume behaviour is settled at M4. |
| 8 | Telemetry format and privacy | **DEFAULTED** | Export-on-demand JSONL, no identity data | Plan recommendation. Implemented at M5. |
| 9 | Internal build distribution | **OPEN** — blocks M6 | — | — |
| 10 | Performance budget | **OPEN** — measured first | — | The plan is explicit: measure before choosing a number. |

## Decisions made during M0 that the plan did not anticipate

| # | Decision | Value | Rationale |
|---|---|---|---|
| A | Simulation assemblies target `netstandard2.1` | Core / Content / Application / Debug | Unity's scripting runtime consumes netstandard2.1 assemblies directly. Targeting `net8.0` would have forced a rewrite at M4. Tests target `net8.0`. |
| B | Zero third-party packages | `nuget.config` clears all package sources | The deterministic Core takes no dependency it does not control, and CI needs no package feed. A stray `PackageReference` now fails loudly at restore instead of silently entering the authoritative simulation. |
| C | Hand-rolled test harness instead of xUnit/NUnit | `tests/Epoch.Testing` | Follows from (B). ~200 lines: an attribute, an assertion class, a deterministic reflection runner. **Tradeoff:** no IDE test explorer integration, no parallel execution, no parameterised-case sugar. If the owner prefers tooling over zero-dependency, deleting `Epoch.Testing` and adding xUnit is a contained change — the test bodies themselves barely move. Unity-side PlayMode tests will use NUnit via the Unity Test Framework regardless; that is a separate thin smoke layer. |
| D | Interface names follow the plan verbatim | `TurnResolver`, not `ITurnResolver` | The Technical Plan sec.6 snippets omit the `I` prefix. Matching them exactly keeps code and authority document greppable against each other. This departs from normal C# convention; say the word and it changes in one pass. |
| E | Empty suites fail | `TestRunner` returns 1 on zero discovered cases | A suite that is wired but empty must never report green. The plan calls out "no unconditional placeholder assertion" as a repaired defect in the Node simulator; this is the same failure mode. |
| F | `Assert.PendingMilestone` fails loudly | — | Same reasoning as (E). A test for unbuilt behaviour is a failing test, not a skipped one. |
