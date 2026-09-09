# EPOCH V1 Technical Implementation Plan

**Document purpose:** final pre-development map for an intentionally ugly, fully playable mobile-first vertical slice  
**Rules authority:** `EPOCH_Core_Gameplay_Spec_v1.md`, rules `1.2.0-v1-final`  
**Content authority:** `data/epoch_v1_content.json`, content `1.0.1-v1`  
**Status:** ready for implementation after the owner decisions in §17.5; no production code is created by this document

---

## 0. Executive summary

Build the V1 vertical slice in Unity with C#, keeping all gameplay in a deterministic, engine-free Core and treating the repaired Node simulator as a conformance oracle rather than runtime code. The implementation should prioritize a complete, replayable 24-turn portrait-mobile match with placeholder visuals, strict JSON validation, exhaustive automated rules checks, and enough instrumentation to evaluate the repaired balance findings before any rule or content tuning.

## 1. Product and scope lock

EPOCH is “Civilization Go”: Civilization-inspired Ages, economy, military counters, and build diversity compressed into a fast, accessible mobile session. Monopoly GO influences accessibility, feedback cadence, asynchronous structure, and meta-game clarity only. Dice and board-walking mechanics are absent unless separately approved.

The first playable is one complete 24-turn match between PLAYER and a local deterministic Snapshot or bot. It includes three shared offers per turn; BUILD, TRAIN, ADVANCE, KEYSTONE, and forced PASS; Growth and Insight; four Ages; three five-tile lanes; RIVER, HIGHLAND, and COAST; movement; combat on every co-occupied tile; the counter triangle; soft stacking; REACH; contested ownership and scoring; a turn-24 result; seeded replay; same-seed restart; JSON content loading; and human-readable debug state.

The slice explicitly excludes final artwork, polished UI, final animation/audio, monetization, gacha, stores, LiveOps, accounts, social-video export, production backend infrastructure, and meta-game implementation. No excluded system may become a dependency of a playable match.

### 1.1 Authority and change control

Use sources in this order:

1. Core Gameplay Specification: authoritative V1 simulation behavior.
2. JSON content manifest: authoritative prototype content data.
3. Markdown Content Manifest: human-readable content rationale and validation notes.
4. Balance Report: observational evidence only.
5. Master Reference: authority where the Core Spec does not supersede it.
6. Hooked Player Playbook: supporting research only.

The repaired Node simulator is a reference oracle and fixture generator, not production runtime code. Its earlier result set is invalid. The repaired findings—11.42% PASS, 30% ties, average score about 6, large ending resources, and stronger military heuristics—justify instrumentation and experiments, not automatic rules/content changes.

---

## 2. Engine recommendation

### 2.1 Primary: Unity with C#

Use the current Unity LTS release approved at project kickoff, C#, portrait orientation, and a minimal 2D scene. Put the deterministic simulation in ordinary .NET assemblies with no `UnityEngine` reference. Unity is the pragmatic primary because it combines mature iOS/Android export, a well-established automated-test workflow, broad mobile profiling support, straightforward JSON/file integration, and the largest future integration surface for monetization SDKs, analytics, backend SDKs, and asset tooling. Those future integrations are not part of V1, but choosing Unity avoids an engine migration if the prototype succeeds.

Claude Code or another coding agent can maintain either engine if the repository is text-first. Unity is manageable when scenes and prefabs stay thin, gameplay lives in C# assemblies, generated project files are ignored, and serialized assets are minimized. The recommended architecture deliberately avoids putting domain behavior in `MonoBehaviour` classes.

### 2.2 Fallback: Godot with C#

Godot with C# is a credible fallback if open-source licensing, smaller editor/runtime footprint, and text-friendly scenes are higher priorities than SDK breadth. Keep the same pure C# Core/Content/Application boundaries. Before selecting it, prove the intended C# mobile export path on both Android and iOS using the exact Godot/.NET version and build host. Future commercial SDKs and vendor integrations are less consistently turnkey than Unity and may require native bridging.

### 2.3 Comparison

| Criterion | Unity + C# | Godot + C# |
|---|---|---|
| Android/iOS deployment | Mature, heavily documented pipelines; iOS still requires Apple tooling/signing | Supported, but C# mobile export/version constraints require an early device spike |
| Coding-agent maintainability | Strong if gameplay is pure C# and scenes remain thin | Strong; text scenes are attractive, but engine/API examples are less uniform |
| Deterministic simulation | Excellent in an engine-free .NET assembly | Excellent in an engine-free .NET assembly |
| Mobile performance | More than sufficient for this small 2D state; mature profiler | More than sufficient; smaller baseline is attractive |
| Automated tests | Mature EditMode/PlayMode ecosystem plus plain .NET tests | Good unit-test options; less standardized cross-team convention |
| JSON workflow | Straightforward via `System.Text.Json` in non-Unity assemblies or a pinned serializer | Straightforward with .NET APIs |
| Future monetization SDKs | Broadest vendor support | Often possible, more native/plugin work likely |
| Future backend integration | Broad vendor SDK ecosystem | HTTP/.NET integration is good; vendor-specific SDK coverage varies |
| AI-generated assets | Engine-neutral imports; extensive tooling ecosystem | Engine-neutral imports; simple asset pipeline |
| Long-term maintainability | Strong with assembly boundaries; project serialization needs discipline | Strong for small teams; mobile C# compatibility must stay pinned |

**Decision:** Unity with C# is the primary recommendation. Godot with C# is the fallback after a time-boxed device-export spike. The decision is based on deployment, testing, integration breadth, and maintainability—not graphical fidelity.

Official implementation references to verify again when pinning versions: [Unity Android build process](https://docs.unity3d.com/Manual/android-BuildProcess.html), [Unity iOS build process](https://docs.unity3d.com/Manual/ios-BuildProcess.html), [Unity Test Framework](https://docs.unity3d.com/Packages/com.unity.test-framework@1.3/manual/index.html), [Godot Android export](https://docs.godotengine.org/en/stable/tutorials/export/exporting_for_android.html), [Godot iOS export](https://docs.godotengine.org/en/stable/tutorials/export/exporting_for_ios.html), and [Godot C#/.NET](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/index.html).

---

## 3. Architecture

### 3.1 Dependency rule

Dependencies point inward only:

```text
Presentation ──commands──> Application ──calls──> Deterministic Core
      │                         │                       ▲
      │ renders                 ├──> Content Layer ────┤
      │ events                  ├──> Replay/Snapshot    │
      └<────────────────────────┘                       │
Test/Debug ────────────────────────────────────────────┘
```

The Core knows no engine, filesystem, network, wall clock, locale, logging sink, or presentation class. Content definitions cross into Core only as validated immutable values. Presentation never mutates `RunState`; it submits commands and renders returned state/events.

### 3.2 Deterministic Core

Responsibilities:

- Own domain enums/value types and the authoritative `RunState` described in Core Spec §5.
- Validate a turn command against the current state and shared hand.
- Resolve card effects, payment, income, movement, every co-occupied combat site, ownership, scoring, Age transition, and match completion in the Core Spec order.
- Use PCG32/SplitMix64 exactly as Core Spec §10.2 specifies.
- Use the checked-in 81-entry integer damage table. No authoritative `exp()` or engine/native RNG.
- Return a new authoritative state and an ordered immutable event list.

Implementation constraints:

- Prefer immutable records/value objects and explicit state-copy builders. If profiling later requires controlled mutation, confine it inside `TurnResolver` and never expose partially resolved state.
- Use fixed-width unsigned integers with explicit unchecked wrap where the PRNG requires modulo overflow.
- Represent Power in fixed-point hundredths or another documented integer unit through clamp/round/table lookup.
- Stable sorting always includes the complete approved tie chain.
- Never enumerate hash maps where order could affect state or events; sort stable IDs first.

### 3.3 Content Layer

Responsibilities:

- Load the shipped JSON bytes outside the Core.
- Strictly deserialize `schemaVersion`, `contentVersion`, `compatibleRulesVersion`, Age table, weights, and all 48 entries.
- Validate enums, unique IDs, effect-source references, explicit priorities, Age eligibility, costs-by-rule rather than authored costs, effect coverage, and prototype provenance.
- Produce canonical normalized bytes and a content hash using the owner-selected hash algorithm.
- Freeze a `ValidatedContentSet` for the life of a run.

The loader may use filesystem/Unity APIs. The resulting repository interface passed inward may not.

### 3.4 Application Layer

Responsibilities:

- Create a run from rules version, content version/hash, seed, and Snapshot identity.
- Ask the Snapshot provider for that turn’s command.
- Accept one human command, coordinate both sides, invoke the Core once, store events/commands, and publish a presentation model.
- Pause between turns, restart from seed, replay commands, fast-forward, resume from a durable checkpoint, and verify final hashes.
- Reject concurrent/double input while a turn is resolving or animating.

Application logic owns orchestration and persistence policy, never gameplay outcomes.

### 3.5 Presentation Layer

One portrait scene renders state and consumes events. It contains placeholder panels, text, rectangles/circles, and buttons. It owns animation pacing only. Animation cancellation or fast-forward cannot affect state because resolution finishes before animation begins.

### 3.6 Test and Debug Layer

This layer owns conformance fixtures, Node-oracle comparison, seed entry, step/fast-forward controls, state/event inspectors, telemetry export, and developer assertions. Debug instrumentation must not enter state hashes or consume authoritative RNG.

---

## 4. Exact data flow

Canonical flow: `Player input → validated command → deterministic turn resolution → new RunState → replay event → presentation update`.

```text
1. Player input is captured by Presentation as player intent.
2. Application converts intent to CardSelectionCommand.
3. Application obtains Snapshot CardSelectionCommand for the same TurnState/offers.
4. TurnResolver validates both commands against the immutable pre-turn RunState.
5. Invalid human command returns rejection with no mutation; invalid matched-version Snapshot command is a determinism error.
6. TurnResolver executes the canonical turn pipeline and immediately returns:
      TurnResolution { nextState, orderedEvents, commandRecord }
7. ReplayRecorder appends canonical commands plus verification hashes.
8. Application atomically replaces current RunState with nextState.
9. Presentation renders nextState and asynchronously animates orderedEvents.
10. When animation completes or is skipped, Application opens the next choice window.
```

Snapshot choices enter at step 3. They receive the same authoritative offers and their own visible/legal state. They never regenerate or filter the hand.

### 4.1 Data classification

| Classification | Examples | Persistence/hash rule |
|---|---|---|
| Authoritative | Seed/streams, turn, Age, resources, score, structures, units/HP/positions, perks, Keystone flag, lane modifiers/holder/engagement, commands | Canonically serialized; included in authoritative hash as defined by fixture contract |
| Derived | Resolved costs, affordability, legal targets, stack rank, effective Power, income preview, outcome projection | Recomputed; not accepted as input/source of truth |
| Presentation-only | Colors, tween progress, selection highlight, formatted text, particle choice, sound | Never serialized into replay or state hash |
| Debug-only | Inspector expansion, breakpoints, telemetry warnings, sanity assertions, animation speed, oracle diff | May be exported separately; never changes resolution or authoritative RNG |

---

## 5. Proposed Unity project structure

Do not create these files until implementation begins.

```text
EPOCH/
├─ Assets/
│  ├─ Epoch/
│  │  ├─ Content/
│  │  │  └─ Imported/epoch_v1_content.json
│  │  ├─ Presentation/
│  │  │  ├─ Scenes/PrototypeMatch.unity
│  │  │  ├─ Prefabs/CardButton.prefab, LaneView.prefab, DebugPanel.prefab
│  │  │  ├─ Views/MatchView.cs, LaneView.cs, CardHandView.cs, ResultView.cs
│  │  │  ├─ Presenters/MatchPresenter.cs, EventAnimationPlayer.cs
│  │  │  └─ Theme/PrototypeTheme.asset
│  │  └─ Bootstrap/EpochBootstrap.cs
│  └─ Tests/PlayMode/PrototypeSmokeTests.cs
├─ Packages/
├─ ProjectSettings/
├─ src/
│  ├─ Epoch.Core/
│  │  ├─ Domain/Enums.cs, Ids.cs, RunState.cs, TurnState.cs
│  │  ├─ Commands/TurnCommand.cs, CardSelectionCommand.cs
│  │  ├─ StateMachine/RunPhase.cs, TurnResolver.cs
│  │  ├─ Systems/OfferGenerator.cs, IncomeSystem.cs, MovementSystem.cs
│  │  ├─ Systems/CombatSystem.cs, ScoringSystem.cs, EffectResolver.cs
│  │  ├─ Rng/Pcg32.cs, SplitMix64.cs, IndexedRng.cs
│  │  ├─ Combat/DamageTable.cs, FixedPower.cs
│  │  ├─ Events/SimulationEvent.cs, TurnResolution.cs
│  │  └─ Epoch.Core.csproj
│  ├─ Epoch.Content/
│  │  ├─ Definitions/CardDefinition.cs, PerkDefinition.cs, ActiveEffect.cs
│  │  ├─ Definitions/ValidatedContentSet.cs
│  │  ├─ Loading/JsonContentLoader.cs, ContentValidator.cs
│  │  ├─ Hashing/CanonicalContentSerializer.cs, ContentHasher.cs
│  │  └─ Epoch.Content.csproj
│  ├─ Epoch.Application/
│  │  ├─ Runs/RunCoordinator.cs, RunInitializer.cs
│  │  ├─ Snapshot/SnapshotProvider.cs, LocalBotSnapshotProvider.cs
│  │  ├─ Replay/ReplayRecorder.cs, ReplayVerifier.cs, ReplayDocument.cs
│  │  ├─ Persistence/RunCheckpointStore.cs
│  │  └─ Epoch.Application.csproj
│  └─ Epoch.Debug/
│     ├─ Telemetry/PlaytestEvent.cs, JsonlTelemetryExporter.cs
│     ├─ Inspection/StateFormatter.cs, StateDiff.cs
│     └─ Epoch.Debug.csproj
├─ tests/
│  ├─ Epoch.Core.Tests/Systems/, Golden/, Determinism/, Replay/
│  ├─ Epoch.Content.Tests/Validation/, Fixtures/
│  ├─ Epoch.Application.Tests/Snapshot/, RestartResume/
│  ├─ Epoch.Oracle.Tests/Fixtures/, OracleComparisonTests.cs
│  └─ Epoch.Performance.Tests/HeadlessThroughputTests.cs
├─ tools/
│  └─ OracleFixtureExporter/README.md
└─ _aiinfodocs/ (source specifications and disposable Node oracle)
```

Use assembly-definition files mirroring these boundaries when Unity integration begins. Unity assemblies may reference Application/Content/Core; Core references none of them.

---

## 6. Implementation contracts

These boundaries supplement rather than duplicate Core Spec §5.

```csharp
// Aggregate defined by Core Spec §5.2–§5.9. No engine types.
public sealed record RunState(/* authoritative fields per Core Spec */);

public abstract record TurnCommand(int Turn, Side Side);
public sealed record CardSelectionCommand(
    int Turn, Side Side, int OfferIndex, LaneId? TargetLane
) : TurnCommand(Turn, Side); // offerIndex -1 is PASS only when forced

public interface RunInitializer {
    RunState Create(ValidatedContentSet content, SeedCode seed, OpponentSnapshot snapshot);
}

public interface TurnResolver {
    ValidationResult Validate(RunState state, TurnCommand player, TurnCommand snapshot);
    TurnResolution Resolve(RunState state, TurnCommand player, TurnCommand snapshot,
                           ValidatedContentSet content);
}

public sealed record TurnResolution(
    RunState NextState,
    IReadOnlyList<SimulationEvent> Events,
    CanonicalTurnRecord CommandRecord);
```

```csharp
public interface OfferGenerator {
    CardOfferSet Generate(SeedState seed, int turn, ValidatedContentSet content);
}

public interface IncomeSystem {
    SystemResult Apply(RunState preState, Side side, EffectContext effects);
}

public interface MovementSystem {
    SystemResult ResolveSimultaneous(RunState preMovement);
}

public interface CombatSystem {
    SystemResult ResolveAllCoOccupiedTiles(RunState preDamage, DamageTable table);
}

public interface ScoringSystem {
    SystemResult ResolveOwnershipAndScore(RunState postCombat);
}

public interface EffectResolver {
    FixedValue Resolve(EffectQuery query, IReadOnlyList<ActiveEffect> applicable);
}
```

`EffectResolver` collects applicable effects; partitions ADD before MULTIPLY; sorts inside each partition by priority, source-type order, and stable source ID; resolves; clamps; and rounds exactly once using the target-stat rule. It emits debug order information without including it in gameplay state.

```csharp
public interface SnapshotProvider {
    OpponentSnapshot Load(SnapshotId id, VersionPair required);
    CardSelectionCommand GetCommand(OpponentSnapshot snapshot, TurnState turn);
}

public interface ReplayRecorder {
    void Begin(RunIdentity run);
    void Append(CanonicalTurnRecord command, HashDigest stateHash);
    ReplayDocument Seal(MatchResult result);
}

public interface ContentRepository {
    ValidatedContentSet Load(ContentVersion version);
}

public interface DeterministicRng {
    uint NextUInt32();
    uint NextBounded(uint exclusiveUpperBound); // rejection sampling
}
```

`RunInitializer` verifies rules/content versions and hash before creating state. `OfferGenerator` uses indexed `(turn, slot, redrawAttempt)` addresses; it never shares consumption with presentation, policies, or logging. `MovementSystem` resolves stepwise simultaneous movement, prevents pass/swap, checks blocking before each step, and prevents a unit already sharing a tile with an enemy from moving. `CombatSystem` enumerates all five tiles of all three lanes and resolves every co-occupied site from one pre-damage snapshot; REACH support/protection and HIGHLAND remain scoped exactly as the Core Spec defines.

---

## 7. Minimal playable portrait UI

One screen must let a non-developer finish a match:

- Header: turn `n/24`, Age name, seed, PLAYER score, Snapshot score.
- Resource row: authoritative Growth and Insight for each side; optional derived next-income preview clearly marked.
- Board: three labeled columns, five visible tiles each, lane modifier label, contested-tile highlight, placeholder unit tokens with class/HP/REACH marks, lane-scoped structure list.
- Hand: three large card buttons showing name, short effect, resolved cost, and legal/illegal state. Illegal cards remain visible with a concise reason.
- Target mode: tapping BUILD/TRAIN highlights legal lanes; tapping a lane submits the command; cancel returns to the unchanged hand.
- PASS: not a voluntary button. Show an automatic PASS message only when no offer is legal.
- Resolution: input locks, Core resolves immediately, then event animations play.
- Footer/debug affordance: animation speed, skip, step, fast-forward, state panel.
- Result overlay: score, outcome, seed, restart-same-seed, replay, and telemetry export.

No UI class stores resources, units, perks, score, legality, or turn state independently. View models are disposable projections of `RunState`.

---

## 8. Animation and pacing contract

`TurnResolver` completes immediately and returns ordered events. `EventAnimationPlayer` may animate:

1. Card selected.
2. Resource payment.
3. Structure or unit placement / perk acquired.
4. Income.
5. Movement steps.
6. Combat damage.
7. Unit death.
8. Contested ownership.
9. Score.
10. Age transition.
11. Final result.

Animations read event payloads and the resolved state. They never call a gameplay system. Skip completes all visuals and renders `NextState`; 2×/4× changes durations only; debug instant mode uses zero duration. Closing/reopening during animation restores the authoritative post-resolution state and can safely replay or skip remaining presentation events.

---

## 9. Test strategy

### 9.1 Test pyramid

- Unit tests: every validation gate, RNG vector, bounded draw, content rule, effect order, income component, movement collision, all-tile combat, counter, stack ordering, overflow, REACH engagement, ownership, scoring, and Age transition.
- Golden scenarios: port the 49 simulator conformance tests (the repaired checks) where applicable. Replace JavaScript assertions with shared JSON fixtures and precise state/event expectations.
- Complete-run goldens: fixed content bytes, seed, player commands, Snapshot commands, final state, event hashes, and result.
- Determinism: run identical inputs in fresh processes and compare canonical bytes; include 32/64-bit, editor/device, Android/iOS where available.
- Replay/restart: recorded commands reproduce every checkpoint/final hash; same-seed restart resets commands and reproduces offers/layout.
- Side swap: paired policies/command fixtures produce normalized mirrored authoritative states and equal/opposite score differentials.
- Content validation: all 48 entries load, every authored effect is executable, versions match, IDs/references are unique, and forbidden authored costs fail.
- Boundary tests: invalid index, wrong target, unaffordable choice, duplicate perk, second Keystone, full lanes, forced PASS, and matched-version illegal Snapshot command.
- Architecture test: Core assembly has no Unity/API/filesystem/network/time/RNG references.
- Performance: resolve 1,000 headless matches within an owner-approved CI/device budget with zero allocations regressions in the per-turn hot path after correctness is stable.

### 9.2 Node-oracle comparison

Create shared, engine-neutral JSON fixtures rather than calling Node from gameplay code:

```text
fixture input  = rulesVersion + exact content bytes/hash + seed
               + 24 PLAYER commands + 24 SNAPSHOT commands
fixture output = lane modifiers + 72 offers + per-turn canonical states
               + ordered events + final result + canonical hashes
```

Workflow:

1. Pin a repaired Node-oracle commit/hash and fixture schema.
2. Export a compact suite covering every mechanic plus 100–1,000 full seeded runs.
3. Load the same fixtures in `Epoch.Oracle.Tests`.
4. Compare field-by-field first, then canonical bytes/hashes.
5. On mismatch, report the first turn/phase/field divergence. Never update expected fixtures automatically.
6. A fixture change requires a documented rules/content version change or an approved oracle defect correction.

Production correctness is established by the specification plus independent agreement—not by copying the Node implementation line-for-line.

---

## 10. Debug and playtest instrumentation

### 10.1 Runtime debug panel

Provide development-build controls for seed entry, restart, step one event, step one turn, fast-forward to turn, finish match, animation speed, show derived legality, show stack/effect order, show PRNG addresses (never mutable), and copy canonical state JSON/hash.

Expose tunable content values and active configuration without allowing mid-run mutation. Display rules version, content version/hash, HP, Age costs, offer weights, BUILD cutoff, REACH mode, overflow mode, and diagnostic resource thresholds.

### 10.2 Exported playtest record

One local JSONL/JSON export per match captures:

- Session/build identifier and platform, excluding personal/account data.
- Rules/content versions and content hash.
- Seed and lane-modifier assignment.
- Every shared hand in slot order.
- Each side’s legal options and rejection reasons.
- Every submitted/accepted command and target.
- PASS events.
- Resources before/payment/income/after.
- Structure/unit/perk/Keystone changes.
- Unit positions and HP before/after movement and combat.
- Combat sites, stack order, dominant classes, counter, REACH, damage, overflow, and deaths.
- Contested holders, score changes, and final result.
- Wall-clock match and presentation duration as debug telemetry only.

Telemetry must be buffered outside Core. Logging failure cannot block or change a match.

### 10.3 Balance findings to preserve as hypotheses

Make the following measurable without encoding a fix:

- PASS: repaired baseline 11.42%.
- Ties: 30%.
- Average score: about 6 per side.
- Ending Growth/Insight: high relative to spending.
- Military-oriented heuristics outperform economic-oriented heuristics.
- All-tile combat materially changed combat rate and REACH observations in the repaired analysis.

Age tables and approved mechanics stay locked. Allowed prototype tuning occurs in versioned content/configuration and requires owner approval.

---

## 11. Milestones

### M0 — Project skeleton and automated tests

- Deliverables: Unity project pinned to LTS; pure C# solution/assemblies; CI; formatting/analyzers; Core dependency guard; imported spec fixtures; empty interfaces.
- Dependencies: engine/version choice, supported .NET profile, canonical serialization/hash choice.
- Acceptance tests: Core builds without Unity references; unit-test command runs locally/CI; Android/iOS placeholder project opens.
- Exit criteria: clean clone builds tests with documented commands.
- Relative complexity: **S**.
- Risks: Unity project serialization noise; accidental engine dependency in Core.

### M1 — Deterministic command-line/headless match

- Deliverables: domain state, PCG32/SplitMix64, state machine, commands, income/movement/all-tile combat/scoring, event list, scripted legal commands.
- Dependencies: M0; final decisions on timeout, hash, fixed-point representation.
- Acceptance tests: PRNG vectors; movement engagement guard; every combat site; simultaneous damage; 24-turn headless completion; repeat hash equality.
- Exit criteria: one scripted match completes with no engine APIs and reproducible canonical output.
- Relative complexity: **XL**.
- Risks: ordering drift, integer overflow semantics, REACH engagement reset, mirrored orientation.

### M2 — Content manifest integration

- Deliverables: strict loader/validator, immutable content repository, compatibility guard, canonical content hash, all 48 entries/effects executable.
- Dependencies: M1 interfaces; JSON schema decision.
- Acceptance tests: valid `1.0.1-v1` loads; malformed/unknown/stale content fails; effect coverage is exhaustive.
- Exit criteria: no prototype card/effect is hard-coded in gameplay orchestration.
- Relative complexity: **M**.
- Risks: schema drift and stringly typed target stats.

### M3 — Snapshot and replay

- Deliverables: local scripted/bot Snapshot provider, canonical command log, replay recorder/verifier, restart and checkpoint resume.
- Dependencies: M1–M2; Snapshot policy choice; persistence format.
- Acceptance tests: same-seed offers/layout; full replay hash equality; normalized side-swap fixtures; corrupted/version-mismatched snapshots fail correctly.
- Exit criteria: close/reopen/replay/restart reproduce the expected run.
- Relative complexity: **L**.
- Risks: canonical serialization differences and incomplete checkpoint state.

### M4 — Minimal portrait playable UI

- Deliverables: single match scene, header/resources, 3×5 board, placeholders, hand, legality, lane targeting, forced PASS feedback, event animation, result/restart/replay.
- Dependencies: M3 stable coordinator/events.
- Acceptance tests: human completes 24 turns on target Android and iOS devices without developer intervention; skip animation changes no hash.
- Exit criteria: device playthrough succeeds from launch to result and same-seed restart.
- Relative complexity: **L**.
- Risks: UI accidentally caching state; small-screen readability; input during animation.

### M5 — Debug tooling and telemetry export

- Deliverables: state inspector, seed entry, stepping/fast-forward, effect/stack views, JSONL export, oracle diff utility.
- Dependencies: stable canonical state/event schemas.
- Acceptance tests: exported record reconstructs choices/outcomes; debug toggles do not change hashes or offers.
- Exit criteria: a designer can reproduce a report from seed and commands without an engineer.
- Relative complexity: **M**.
- Risks: debug data contaminating authoritative state; oversized exports.

### M6 — Human playtest build

- Deliverables: signed internal builds, playtest script, known-issues list, export collection workflow, device matrix.
- Dependencies: M4–M5; distribution/signing decisions.
- Acceptance tests: fresh testers complete matches and export valid telemetry; crashes and blocked turns are zero in smoke cohort.
- Exit criteria: enough complete runs across policies/play styles to answer the priority hypotheses.
- Relative complexity: **M**.
- Risks: signing/distribution delays and biased tester instructions.

### M7 — First evidence-based tuning pass

- Deliverables: analysis comparing human telemetry with repaired oracle, proposed versioned content changes, before/after fixtures, owner decision log.
- Dependencies: M6 sample threshold chosen in advance.
- Acceptance tests: every proposal maps to evidence; rules remain unchanged unless explicitly approved; content version/hash advances for accepted changes.
- Exit criteria: owner accepts/rejects each proposal and a new playtest build is reproducible.
- Relative complexity: **M**.
- Risks: overfitting bot data, causal claims from observational selection, changing several variables at once.

---

## 12. Exact build order

1. Pin Unity LTS, package versions, target OS/device floor, and repository settings.
2. Create pure C# Core/Content/Application test projects and Unity adapters.
3. Import normative constants, Age table fixture, damage table, and PRNG vectors.
4. Implement canonical IDs, fixed-point Power, immutable state, commands, events, and serialization.
5. Implement/version-test SplitMix64, PCG32, indexed streams, rejection sampling, and lane shuffle.
6. Implement content schema, strict validation, compatibility checks, and canonical content hash.
7. Implement shared offers and per-side legality/PASS.
8. Implement card payment/application and total ActiveEffect order.
9. Implement income and diagnostic resource telemetry.
10. Implement simultaneous step movement and the already-engaged movement guard.
11. Implement all-tile combat, stacking, counter, HIGHLAND, REACH, simultaneous integer-table damage, and overflow.
12. Implement ownership, scoring, Age transitions, and turn-24 result.
13. Port conformance fixtures and compare with the Node oracle.
14. Implement local Snapshot provider, replay, restart, and resume.
15. Add the single portrait placeholder scene and asynchronous event animations.
16. Add debug inspector, seed tools, fast-forward, export, and performance harness.
17. Produce device playtest builds; only then propose balance changes.

---

## 13. Definition of done

The V1 vertical slice is done only when:

- A new player can complete a 24-turn match on a supported mobile device without developer intervention.
- Identical rules/content bytes, seed, and commands always produce byte-equivalent authoritative results.
- Production outputs agree with approved Node-oracle fixtures, including all-tile combat and side-swapped runs.
- Every approved mechanic and every authored prototype effect is executable and covered.
- Placeholder UI sends commands and renders state but owns no gameplay state.
- A match can be paused, resumed, replayed, and restarted from the same seed.
- Playtest telemetry exports the complete audit record without affecting determinism.
- Balance data can change through validated versioned content/configuration rather than system rewrites.
- No monetization, backend, account, final-art, animation, or audio dependency blocks testing.
- Golden, determinism, replay, content, architecture, and 1,000-match performance suites pass in CI.

---

## 14. Critical technical risks

| Risk | Mitigation / gate |
|---|---|
| Core Spec and oracle divergence | Shared fixtures, first-divergence reports, pinned oracle revision; spec wins |
| Engine APIs leak into Core | Assembly references plus automated forbidden-namespace test |
| Float/platform drift | Fixed-point Power and integer damage table; canonical integer serialization |
| RNG consumption drift | Indexed addresses, dedicated streams, vector tests, no RNG service in Presentation |
| Orientation asymmetry | Normalize mirrored positions and run paired full-state side-swap tests continuously |
| Movement/combat site regression | Tests for engaged movement guard and co-occupied tiles 1–5 in every lane |
| Effect silently ignored | Exhaustive content-to-handler validation fails build on uncovered effect |
| Content/hash drift | Immutable loaded set and version/hash stored with run/replay/snapshot |
| Replay cannot reproduce | Store canonical commands and verify state hash after every turn/event boundary |
| UI double-submit | Coordinator state gate and idempotent command identity |
| Debug/logging changes result | Debug layer outside Core; RNG-isolation and hash-equivalence tests |
| Balance logic becomes code | Keep tunable magnitudes/eligibility in validated content/config; owner-approved versions |

---

## 15. Recommended architecture summary

Build a pure deterministic C# simulation library first, surrounded by a strict JSON Content layer and a thin Application coordinator. Unity supplies only mobile lifecycle, input, placeholder rendering, and event animation. Snapshot/replay are command sources and records, not alternative simulation paths. Tests and debug tooling consume the same public Core boundary as the application. The repaired Node simulator remains an independent oracle through shared fixtures.

---

## 16. Owner decisions required before coding

These decisions should be recorded in a short implementation lock. They do not authorize balance changes.

1. Exact Unity LTS/editor version and whether Unity Personal licensing is acceptable for the prototype.
2. Minimum Android API/device class and minimum iOS version/device class; physical devices available for CI/smoke testing.
3. Canonical authoritative state serialization and hash algorithm (recommend canonical UTF-8 JSON for inspectability plus SHA-256 for V1 fixtures).
4. Fixed-point Power unit (recommend hundredths) and the explicit rounding rule for any future fractional income effect; current prototype income multipliers remain integral.
5. Choice-window timeout policy: no timeout for local V1 is recommended; if a timeout exists, define whether it waits or submits only an already legal selection. It must never create voluntary PASS.
6. Initial Snapshot source: fixed scripted fixture, deterministic local bot policy, or both. Recommend both, with fixtures for conformance and bot for playtesting.
7. Checkpoint policy: after every completed turn is recommended; clarify whether mid-animation app closure resumes presentation or renders the resolved state immediately.
8. Local telemetry format/location and tester-consent/privacy expectations. Recommend export-on-demand JSONL with no identity data.
9. Internal build distribution/signing route for Android and iOS.
10. Performance acceptance budget for 1,000 headless matches and target device-frame budget. Measure before selecting numbers.

No rule/content tuning is required to start M0–M3. PASS, tie rate, score range, resource surplus, and policy performance remain playtest questions for M7.

---

## 17. Final handoff

### 17.1 Recommended engine

**Unity with C#**, using a pinned LTS release. **Fallback:** Godot with C# after a successful two-platform export spike.

### 17.2 Architecture summary

Pure engine-free Deterministic Core → validated immutable Content → orchestration/replay/Snapshot Application → command-only placeholder Presentation, surrounded by independent Test/Debug tooling and Node-oracle fixture comparison.

### 17.3 Exact build order

Execute M0 through M7 without overlap that pulls UI ahead of Core: skeleton/tests → headless deterministic match → content → Snapshot/replay → portrait UI → debug/telemetry → human build → evidence-based tuning.

### 17.4 Critical technical risks

The highest risks are specification/oracle drift, platform numerical drift, RNG consumption drift, orientation asymmetry, incomplete all-tile combat, silently unsupported effects, UI-owned state, and noncanonical replay serialization. Each has a mandatory automated gate in §§9 and 14.

### 17.5 Decisions the owner must make before coding

Approve the engine/version, target mobile floors/devices, canonical serialization/hash, fixed-point representation, timeout policy, initial Snapshot source, checkpoint/resume behavior, telemetry privacy/export, internal distribution route, and performance budgets listed in §16.

### 17.6 Copy-paste implementation kickoff prompt

```text
Implement EPOCH milestone M0, then M1, from
_aiinfodocs/EPOCH_V1_Technical_Implementation_Plan.md.

Authoritative priority:
1. _aiinfodocs/EPOCH_Core_Gameplay_Spec_v1.md
2. _aiinfodocs/data/epoch_v1_content.json
3. _aiinfodocs/EPOCH_V1_Content_Manifest.md
4. _aiinfodocs/EPOCH_V1_Balance_Report.md as evidence only

Use Unity with C# at the owner-approved pinned LTS version. Create an engine-free
Epoch.Core assembly and automated tests before any playable UI. Do not implement
monetization, backend, accounts, final art/audio, LiveOps, gacha, storefronts, or
social export. Do not change rules, Age tables, content effects, or balance values.

For M0: create the project/assembly/test skeleton, dependency guards, CI commands,
and shared fixture location. For M1: implement one complete 24-turn headless match,
including PCG32/SplitMix64 vectors, indexed shared offers, command validation, forced
PASS, income, simultaneous step movement with the engaged-unit guard, combat on every
co-occupied tile, soft stacking, counters, HIGHLAND, mirrored REACH support/protection,
simultaneous integer-table damage with overflow, ownership, scoring, Age transitions,
ordered events, and turn-24 result.

Port the repaired conformance fixtures and compare canonical production output with
the Node oracle. Keep Core free of UnityEngine, filesystem, network, timestamps,
logging, native RNG, and authoritative floating-point damage. Stop and report any
genuine specification ambiguity instead of inventing a rule. At each milestone,
run tests, report exact files changed, show acceptance evidence, and do not begin the
next milestone until its exit criteria pass.
```
