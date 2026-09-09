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

## Decisions made during M1

No rule, Age table, content effect or balance value is changed by any of these. Each is
an engineering choice the Technical Plan left to implementation.

| # | Decision | Value | Rationale |
|---|---|---|---|
| G | Content definition types live in `Epoch.Core`, not `Epoch.Content` | `Epoch.Core.Content.CardDefinition` / `ValidatedContentSet` | The plan's sec.5 tree puts `CardDefinition.cs` under `Epoch.Content`, but the Core must read card definitions and "Core references none of them" (sec.3.1). Putting the immutable value types in Core and leaving loading, strict validation and hashing in `Epoch.Content` satisfies both, and matches sec.3.1's own wording: "Content definitions cross into Core only as validated immutable values." |
| H | Canonical state serialization is in Core; hashing is in Application | `Core.Serialization.CanonicalState` produces text, `Application.Runs.StateHasher` digests it | `System.Security` is on the Core's forbidden-namespace list, so a Core type that held a hash would be one the Core could never populate. `CanonicalTurnRecord` therefore carries canonical bytes rather than a digest. |
| I | Card pool ordering is the content file's declaration order | builds, then trains, then perks, then keystones | Offer generation selects by index into the eligible subset, so declaration order is part of the deterministic contract. Reordering the manifest changes every offer sequence and requires a `contentVersion` change. Matches the pinned oracle's `allCards` construction. |
| J | The weighted type draw is `NextBounded(100)` with cumulative BUILD, TRAIN, ADVANCE | — | Core Spec sec.6.0 fixes the weights and the indexed address but not the draw mechanism, which nonetheless changes output. The pinned oracle (`simulate.js:85`) uses exactly this, so production and oracle agree by construction rather than by luck. |
| K | Systems are static, not injected services | `OfferGeneration`, `IncomeSystem`, `MovementSystem`, `CombatSystem`, `ScoringSystem`, ... | Each is a pure function of authoritative state; an instance would carry no field except one a determinism bug could hide in. `DeterministicRng` stays an interface because "no native RNG" [Lock 19] is a rule about a capability. The plan's sec.6 interface names are kept on the concrete entry points. |
| L | Per-unit `effectivePower` effects apply before stacking | — | Content authors `UNIT_CLASS`-scoped `ON_COMBAT_PRE` effects. C3 picks the dominant class from post-stacking, pre-counter Power, so a class-scoped bonus can only be meaningful if it lands before that. **Worth an owner confirmation at M2**, when effect coverage is proved exhaustively; no M1 acceptance test depends on it. |
| M | M1 loads content through a test-only fixture loader | `tests/Epoch.Testing/ContentFixtureLoader.cs` | M1 needs the real 48-entry pool to run matches; M2 owns strict deserialization, exhaustive validation, effect-coverage proof and the canonical content hash. The loader deliberately does not validate - rejecting malformed content is M2's job, and pretending otherwise here would hide that work. `ContentHash` is the placeholder `sha256:pending-m2`. |
| N | Complete-run goldens use two different legal-choice policies | PLAYER first-legal, SNAPSHOT last-legal | Two identical policies mirror exactly: both sides reach tile 3 on the same turn with equal Power, every contested tile is disputed, and every match ends 0-0. That is correct for identical play but would leave the goldens silent about scoring, ownership persistence and contested income. GT-15c asserts the fixture contains scoring runs so it can never go quietly vacuous. |

### Specification discrepancy found during M1 (needs an owner ruling)

**Core Spec sec.10.2, vector `OFF-19`.** The row is labelled "master `1`", but its expected
address seeds derive from the card stream `910A2DEC89025CC1`, which the `STR-00` row gives
for master **`0`**. The two rows cross-check each other and the label is the outlier; the
implementation reproduces all three addresses and all three `OFF-19-OUT` outputs from
master `0`. Taken as a typo in the label, with the numbers treated as normative. The
algorithm is unambiguous either way, so nothing is blocked - but the document should be
corrected so a future implementer does not re-derive this.
