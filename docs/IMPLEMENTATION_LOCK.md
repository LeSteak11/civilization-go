# EPOCH V1 — Implementation Lock

Records the engineering decisions the Technical Implementation Plan sec.16 requires
before coding. **These are not balance decisions.** No rule, Age table, content effect or
balance value is changed by anything in this file.

Status key: **DEFAULTED** = Claude chose a reversible default so M0 could proceed;
owner may overturn at any time. **OPEN** = still needs an owner answer, and the milestone
that will be blocked by it is named.

| # | Decision | Status | Value | Rationale / cost of changing later |
|---|---|---|---|---|
| 1 | Unity LTS version and licensing | **OWNER-RESOLVED** | Unity `6000.3.23f1`; existing local license | Owner explicitly pinned this editor for M4. `ProjectVersion.txt` records the exact version and revision. |
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

### Specification discrepancy found during M1 (RESOLVED by owner ruling, M2)

**Core Spec sec.10.2, vector `OFF-19`.** The row is labelled "master `1`", but its expected
address seeds derive from the card stream `910A2DEC89025CC1`, which the `STR-00` row gives
for master **`0`**. The two rows cross-check each other and the label is the outlier; the
implementation reproduces all three addresses and all three `OFF-19-OUT` outputs from
master `0`. Taken as a typo in the label, with the numbers treated as normative. The
algorithm is unambiguous either way, so nothing was blocked.

**Owner ruling (M2):** corrected in the Core Spec from master `1` to master `0`. The
address seeds and `OFF-19-OUT` values are unchanged and remain normative. Recorded as a
documentation-label correction in Core Spec sec.17.4; `rulesVersion` stays
`1.2.0-v1-final`.

## Decisions made during M2

| # | Decision | Value | Rationale |
|---|---|---|---|
| O | Hand-rolled strict JSON reader | `Epoch.Content.Json` | `System.Text.Json` is not in the netstandard2.1 surface, and decision (B) forbids NuGet packages in the simulation assemblies - verified by compiling a probe against `Epoch.Content`. Writing the reader also makes "strict" enforceable rather than aspirational: duplicate keys, trailing commas, comments, leading zeros, raw control characters and unknown members are all rejected, where a permissive general-purpose parser would accept most of them. **Tradeoff:** ~450 lines to maintain. If the owner prefers a serializer, adding `System.Text.Json` is a contained swap that requires reopening decision (B). |
| P | Numbers never become floating point | `JsonValue.AsHundredths` | The magnitude lexeme is converted to fixed-point hundredths by integer arithmetic, so a content file cannot introduce a float into the authoritative path even by authoring one. More than two decimal places is refused rather than rounded: a magnitude the engine cannot represent exactly is a balance value nobody authored. |
| Q | The content hash covers meaning, not bytes | `ContentHasher.Canonicalize` | Reformatting the manifest, reordering an entry's members or editing a designer note must not change the hash, because none of them change a match. Anything that does change a match - a magnitude, an Age range, a unit class, or the declaration order offer generation indexes into - must. Hashing raw file bytes would invalidate every stored fixture on a whitespace edit and tell a designer their comment broke determinism. |
| R | The effect catalog lists **handlers**, not combinations | `EffectCatalog` | A row carries the set of scope values it accepts, because lane-scoped income is one code path parameterised by modifier rather than three paths. Reverse coverage then asserts every *handler* is exercised - a handler nothing authors is an untested code path claiming support - without forcing content to author every parameter value of every row. |
| S | LANE- and SIDE-scoped `effectivePower` apply to the lane total, not per unit | `EffectSources.LanePowerEffects` | The authored rules text is explicit: "add N to the owner's effective lane Power" and "multiply the owner's effective lane Power by 1.10 ... before counter and damage resolution". Applied per unit these would be multiplied by the stack size and then scaled by 1.00/0.75/0.50. They are applied once to the side's post-stacking total, after the dominant class is chosen (a lane-wide bonus belongs to no class) and before the counter at C4 - exactly where the rules text puts them. |
| T | A structure's effect applies once **per structure**, in its own lane | `EffectSources.LanePowerEffects` | `BUILD_GARRISON_POST` grants Power "there", and structures have no cap per lane or per run [Lock 9], so two Garrison Posts in one lane grant it twice. Each instance is its own effect source. |
| U | Income is floored at zero | `IncomeSystem.Compute` | `PERK_CONV_RIVER_INSIGHT` subtracts 1 Growth income. Invariant PS-1 says a resource is never driven negative. With base income of 3 Growth / 2 Insight the floor is unreachable in V1 content; it exists so PS-1 holds by construction rather than by arithmetic coincidence, and it never clamps a positive value. |
| V | Decision (L) confirmed by the owner | `EffectSources.UnitPowerEffects` | `UNIT_CLASS`-scoped Power effects apply per unit **before** stack ordering and the stack multipliers, and may therefore change stack rank and the dominant class selected at C3. |

### Effects that M1 loaded but never executed (found by the M2 coverage gate)

M1 gathered effects from perks only, using a single side-wide scope context. Five authored
effects across four cards therefore matched nothing: they loaded, read as if they did
something, and did nothing.

| Card | Effect | Why it was dead |
|---|---|---|
| `PERK_ECO_RIVER_GROWTH` | `growthPerTurn` LANE/RIVER | income gathered with no lane context |
| `PERK_UTIL_COAST_GROWTH` | `growthPerTurn` LANE/COAST | as above |
| `PERK_UTIL_HIGHLAND_INSIGHT` | `insightPerTurn` LANE/HIGHLAND | as above |
| `PERK_CONV_RIVER_INSIGHT` | `growthPerTurn` and `insightPerTurn` LANE/RIVER | as above (two effects) |
| `BUILD_GARRISON_POST` | `effectivePower` LANE/TARGET_LANE | structure-borne effects were never gathered at all |

This is the failure mode the Technical Plan sec.14 calls "effect silently ignored", and it
is invisible in play, in a replay and in a balance report. It is now a build-time gate:
content that authors an effect the engine does not implement is refused, and the run is
never created.

### Golden fixture regeneration (owner-approved, one time)

`fixtures/golden/m1_replay_hashes.json` was regenerated once at M2. Two changes moved every
value, and the fixture records both in its own `regenerationReason`:

1. `contentHash` replaced the M1 placeholder `sha256:pending-m2` with the canonical hash
   `sha256:41504936cd39a5c1fe1f5e466d77ce1a84dafa4f149b1e43741d6070657ed49a`, and
   `contentPoolHash` is part of authoritative state.
2. The five previously-dead effects above now execute, which changes match outcomes.

No rule, Age table, content effect or balance value was changed.

### M2 work deliberately not done

At the owner's direction, the M2 **test** suite was not written. The mechanisms exist and
are exercised by every test that runs a match, but the following have no dedicated
coverage and should be added before or alongside M3:

- negative validation cases (malformed, unknown-member, stale-version, authored-cost,
  uncovered-effect documents are all *rejected* by code that no test exercises);
- explicit decision (L) tests showing a `UNIT_CLASS` Power effect changing stack rank and
  the dominant class at C3;
- behavioural tests for the five effects repaired above.

## Decisions made during M4

| # | Decision | Value | Rationale |
|---|---|---|---|
| W | Unity consumes prebuilt simulation assemblies | `Assets/Plugins/Epoch/*.dll`, built from the existing `netstandard2.1` projects | Unity 6000.3's source compiler is C# 9 while the authoritative Core uses C# 10 record structs. Referencing the already-intended netstandard assemblies preserves one gameplay implementation and avoids rewriting domain types for Unity. |
| X | Presentation scene is constructed procedurally | One empty `PrototypeMatch.unity` scene plus `EpochMatchController` | Keeps serialized UI state minimal and reviewable. The controller owns only Unity view/input/animation state; every gameplay value comes from `PlayableMatchSession` and Core events. |
| Y | Mid-animation resume behavior | Render the already-resolved post-turn state | Matches Technical Plan sec.8 and decision 7: resolution precedes animation, so closing/skipping presentation cannot change or partially apply gameplay. |

The mobile OS floors in decision 2 remain genuinely open and no Android/iOS build was produced in M4. The owner-scoped M4 deliverable is the playable Unity Editor experience; physical-device certification remains blocked until those target floors/devices are selected.
