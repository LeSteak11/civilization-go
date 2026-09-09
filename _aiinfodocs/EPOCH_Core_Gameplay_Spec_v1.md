# EPOCH — Core Gameplay Specification v1

**Document ID:** `EPOCH_Core_Gameplay_Spec_v1`
**Rules version:** `1.2.0-v1-final`
**Content version:** `1.0.0-v1`
**Date:** 8 September 2026 (revised — final V1 rules lock applied)
**Status:** Core simulation fully specified, deterministic, and implementation-complete. No blocking rule decision remains. See §14–§17.

---

## 0. How to read this document

### 0.1 Source authority

| Priority | Source | Role |
|---|---|---|
| 1 | Direct instruction from the project owner | Overrides everything |
| 2 | `_aiinfodocs/EPOCH-master-reference.md` (**MR**) | Immutable project Source of Truth |
| 3 | `_aiinfodocs/hooked-player-playbook.md` (**HPP**) | Supporting research only. **May never override or reinterpret an MR rule.** Cited only to justify a PROVISIONAL recommendation. |
| 4 | General knowledge | Last resort, always labelled |

Where MR and HPP conflict, **MR wins**. Where MR is silent, this document records an `OPEN-V1` decision and offers a labelled recommendation. It does not silently invent.

### 0.2 Label taxonomy

Every normative statement in this document carries exactly one label.

| Label | Meaning | Implementer's obligation |
|---|---|---|
| `LOCKED` | Stated explicitly in MR. Cited by section ID. | Implement exactly. Do not vary. |
| `DERIVED` | Not stated verbatim, but follows necessarily from one or more LOCKED rules by arithmetic or logical entailment. Derivation is shown. | Implement as written. Challenge only by disputing the derivation. |
| `LOCKED-V1` | **Owner-approved in a V1 rules lock** (§17). Not from MR, but binding on V1 with the same force. Cited by lock rule number. | Implement exactly. Do not vary. |
| `V1-EXCEPTION` | An owner-approved deliberate departure from an MR rule, scoped to V1 and time-boxed to a stated validation gate. | Implement the exception. Do **not** implement the superseded MR rule. |
| `PROVISIONAL` | MR and the Lock are both silent. This document proposes a rule so that V1 can be built. | **Requires owner approval before coding.** Flag in code with the OPEN-V1 ID. |
| `OPEN-V1` | MR and the Lock are both silent, and the answer materially changes the simulation. | **Blocks implementation of the affected subsystem until resolved.** |

`CONFLICT` marks a place where two MR passages disagree. All three conflicts found in the original audit are resolved by Lock 1 — see §17.2.

### 0.3 Terminology lock

MR terminology is preserved exactly and must not be renamed in code, comments, telemetry, or UI:

`Growth`, `Insight`, `Age`, `Turn`, `Lane`, `Tile`, `Build`, `Train`, `Advance`, `Keystone`, `Perk`, `Snapshot`, `Seed`, `Contested tile`, `Lane modifier`, `Capital`, `REACH`, `Sword`, `Spear`, `Horse`, `Soft stacking`, `Run`.

Forbidden synonyms: *energy*, *mana*, *food*, *gold*, *science*, *hero*, *minion*, *round*, *wave*, *board*, *deck slot*, *AI opponent*, *bot*.

### 0.4 Non-goals of this document

Explicitly out of scope, per instruction: application code; monetization and storefront; commander gacha; social sharing and export; LiveOps; final UI, art, animation or audio; production networking, auth or backend architecture; rebalancing of the combat exponent or Age curves.

---

## 1. Purpose and scope

### 1.1 What this specification owns

This document converts MR's conceptual rules into an exact, deterministic gameplay contract sufficient for a coding agent to implement the EPOCH core simulation without design judgement.

It owns: the run state machine, the canonical turn order of operations, the game-state data model, card action contracts, the economy, board and movement, combat, seed and snapshot determinism, scoring and match completion, edge cases, and golden tests.

### 1.2 Included in playable V1

| # | Capability | MR source |
|---|---|---|
| V1-01 | A complete 24-turn run, single player vs. one Snapshot | MR §1.2, §1.4 |
| V1-02 | Four Ages of six turns each | MR §1.2, §2.2 |
| V1-03 | Three card offers per turn, exactly one selected | MR §1.2 |
| V1-04 | Growth and Insight earned and spent | MR §2.1 |
| V1-05 | BUILD, TRAIN, ADVANCE card resolution; KEYSTONE as an ADVANCE subtype | MR §1.2, §2.2 |
| V1-06 | Three lanes × five tiles, automatic resolution | MR §1.3, §1.2 |
| V1-07 | Unit movement, soft stacking, counter triangle, deterministic combat | MR §2.3 |
| V1-08 | Same seed → identical card offer sequence for both sides | MR §1.4 |
| V1-09 | Snapshot replays a recorded choice list deterministically | MR §1.4 |
| V1-10 | Match reaches turn 24 and produces a reproducible `MatchResult` | MR §1.4 |
| V1-11 | Full state introspection for debugging and balance testing | HPP §15 (instrument every loop stage) |
| V1-12 | Lane modifiers: River, Highland, Coast | MR §1.3 |
| V1-13 | Headless simulation mode capable of 500+ runs for balance validation | MR §10 item 2 |

### 1.3 Deferred until after core validation

| Capability | Reason | MR source |
|---|---|---|
| Commanders and their lateral buffs | Monetization layer; Δ8 budget not yet allocated per-commander | MR §4.5, MR §9.3 |
| Campaign Tokens, ranked/unranked distinction | Economy layer, not simulation | MR §4.4 |
| Replay video export, seed sharing UI, 9:16 render | Presentation and growth layer | MR §6 |
| Elo, ladder seasons, matchmaking bands | MR §9 item 2 marks seasons unspecified | MR §1.4, §9 |
| Real Snapshot sourcing from live players | V1 uses recorded or scripted Snapshots | MR §1.4 |
| Art, animation, the 1.8s Age transition beat | Presentation | MR §7.3 |
| Store, odds disclosure, commerce | Out of scope by instruction | MR §5 |

### 1.4 Explicitly unresolved in the Master Reference

Nine MR items are already marked open in MR §9 and are **not** resolved here: soft vs hard token gate; ladder seasons; Keystone gacha-gating; chest economy; live-ops calendar; store link-out rates; art direction; counter-triangle legibility. Of these only **Keystone gating** touches V1 simulation, and only for content sourcing — the simulation contract in §6.4 is gating-agnostic.

This document adds **30 further OPEN-V1 items** (§14) that MR does not cover and that block exact implementation.

---

## 2. Immutable constants

All values reproduced from MR. `§CONST` refers to MR's machine-readable constants block.

### 2.1 Run structure — all `LOCKED`

| Constant | Value | MR source |
|---|---|---|
| `TURNS_PER_RUN` | 24 | §CONST `epoch_run.turns`, §1.2 |
| `AGE_COUNT` | 4 | §CONST `epoch_run.ages`, §1.2 |
| `TURNS_PER_AGE` | 6 | §CONST `epoch_run.turns_per_age`, §1.2 |
| `CARDS_OFFERED_PER_TURN` | 3 | §CONST `epoch_run.cards_offered_per_turn`, §1.2 |
| `DECISIONS_PER_TURN` | 1 | §CONST `epoch_run.decisions_per_turn`, §1.2 |
| `LANE_COUNT` | 3 | §CONST `epoch_run.lanes`, §1.3 |
| `TILES_PER_LANE` | 5 | §CONST `epoch_run.tiles_per_lane`, §1.3 |
| `MAX_UNITS_PER_LANE` | 3 | §CONST `epoch_run.max_units_per_lane`, §2.3 |
| `STACK_POWER_MULTIPLIERS` | `[1.00, 0.75, 0.50]` | §CONST, §2.3 |
| `TARGET_RUN_SECONDS` | 180 | §CONST — presentation only, no simulation effect |

### 2.2 Economy — `LOCKED`

| Constant | Value | MR source |
|---|---|---|
| `BASE_INCOME_GROWTH` | 3 per turn | §CONST `epoch_run.base_income_per_turn.growth`, §2.1 |
| `BASE_INCOME_INSIGHT` | 2 per turn | §CONST, §2.1 |
| `CONTESTED_TILE_GROWTH` | +1 per turn per held contested tile | §CONST `epoch_run.contested_tile_yield`, §2.1 |
| `CONTESTED_TILE_INSIGHT` | +1 per turn per held contested tile | §CONST, §2.1 |
| `STRUCTURE_YIELD_TIER_1` | +1 | §2.1 |
| `STRUCTURE_YIELD_TIER_2` | +2 (Age 2+) | §2.1 |
| `STRUCTURE_YIELD_TIER_3` | +3 (Age 3+) | §2.1 |
| `STRUCTURE_YIELD_TIER_4` | +4 (Age 4) | §2.1 |
| `BUILD_VIABILITY_RULE` | `cost / yieldPerTurn <= (24 - currentTurn) / 2` | §CONST `epoch_build_rule`, §2.1 — **superseded at runtime by the V1 Build Economy Exception, §6.7** |
| `STARTING_GROWTH` | **8** | `LOCKED-V1` [Lock 3] |
| `STARTING_INSIGHT` | **2** | `LOCKED-V1` [Lock 3] |

`DERIVED`: the opening balance is exactly one Age I BUILD (8 Growth), or one TRAIN (6) with 2 left over, and exactly one ADVANCE (2 Insight). Turn 1 is a real decision from the first tap, never a forced PASS.

### 2.3 Age table — `LOCKED` (MR §2.2, §CONST `epoch_age_table`)

| Age | Name | Turns | Perk cost (Insight) | Unit Power | Unit cost (Growth) | Build cost (Growth) |
|---|---|---|---|---|---|---|
| 1 | Dawn | 1–6 | 2 | 10 | 6 | 8 |
| 2 | Bronze | 7–12 | 3 | 12 | 8 | 11 |
| 3 | Steel | 13–18 | 5 | 14 | 11 | 16 |
| 4 | Modern | 19–24 | 9 | 17 | 16 | 22 |

Ratio invariants (MR §2.2, for balance-test assertions): perk ×1.65, power ×1.19, unit cost ×1.39, build cost ×1.40 geometric mean per Age.

**Do not rebalance these values.** MR §2.2 defends the ×1.19 power step against an explicit alternative and accepts its cost.

### 2.4 Combat — `LOCKED` (MR §2.3, §CONST `epoch_combat`)

| Constant | Value | MR source |
|---|---|---|
| `DAMAGE_FORMULA` | `25 * exp(0.025 * deltaPower)` | §CONST, §2.3 |
| `DELTA_POWER_CLAMP` | ±40 (inclusive) | §CONST, §2.3 |
| `COUNTER_TRIANGLE` | SWORD beats SPEAR; SPEAR beats HORSE; HORSE beats SWORD | §CONST, §2.3 |
| `COUNTER_BONUS` | +40% Power, **applied before the damage curve** | §CONST, §2.3 |
| `REACH` | Contributes Power from tile 2 of the lane without taking return damage on the first clash | §CONST, §2.3 |

Reference exchange ratios (MR §2.3) — implementations must reproduce these to 2 dp:

| Δ Power | 0 | +7 | +8 | +15 | +25 | +40 |
|---|---|---|---|---|---|---|
| Exchange | 1.00 : 1 | 1.42 : 1 | 1.49 : 1 | 2.12 : 1 | 3.49 : 1 | 7.39 : 1 |

| `UNIT_MAX_HP` | **100** | `LOCKED-V1` [Lock 5] |
| `DAMAGE_SIMULTANEITY` | Both sides compute from the same pre-damage state, then apply together | `LOCKED-V1` [Lock 6] |
| `DELTA_ROUNDING` | Round Δ Power to nearest integer, halves away from zero | `LOCKED-V1` [Lock 7] |
| `DAMAGE_SOURCE` | Versioned precomputed integer lookup table over Δ ∈ [−40, +40], 81 entries | `LOCKED-V1` [Lock 7] |
| `NATIVE_EXP_FORBIDDEN` | Platform-native `exp()` must not be called during authoritative match resolution | `LOCKED-V1` [Lock 7] |

`DERIVED` pacing note: at 100 HP and Δ = 0 (25 damage per clash) a unit survives four clashes. A unit trained on turn *n* in a plain lane reaches tile 3 on turn *n*+2, so a unit trained after turn 21 cannot complete an even-power kill unaided. Recorded as an observation for economy simulation, not a rule.

### 2.5 Lane modifiers — `LOCKED` (MR §1.3)

| Modifier | Effect (verbatim) |
|---|---|
| `RIVER` | +1 Growth from Builds placed here |
| `HIGHLAND` | Defender gets +3 Power |
| `COAST` | Units push 2 tiles per turn instead of 1 |

**Assignment — `LOCKED-V1` [Lock 10].** Every run contains exactly one `RIVER`, one `HIGHLAND` and one `COAST`. Their lane positions are shuffled by the dedicated `streamLaneMod` RNG stream (§10.2). Both sides play the same modifier layout — there is one board.

`DERIVED`: exactly 3! = 6 layouts are possible per run, uniformly distributed under a uniform shuffle. Every run therefore offers the same three strategic options in a different arrangement.

`HIGHLAND` "defender" is defined by [Lock 15] — see §9.2 step C4.

### 2.6 Deck weighting by turn band — `LOCKED` (MR §1.6)

| Turns | Age | Build % | Train % | Advance % |
|---|---|---|---|---|
| 1–3 | I | 70 | 20 | 10 |
| 4–6 | I | 45 | 35 | 20 |
| 7–12 | II | 35 | 40 | 25 |
| 13–18 | III | 20 | 45 | 35 |
| 19–21 | IV | 0 | 60 | 40 |
| 22–24 | IV | 0 | 65 | 35 |

**`V1-EXCEPTION` — Build offer window.** The V1 Build Economy Exception (§6.7) restores BUILD to turns 19–20 and closes it on turns 21–24. The **V1 effective table** below supersedes the rows above for turns 19–24:

| Turns | Age | Build % | Train % | Advance % | Source |
|---|---|---|---|---|---|
| 1–3 | I | 70 | 20 | 10 | MR §1.6 |
| 4–6 | I | 45 | 35 | 20 | MR §1.6 |
| 7–12 | II | 35 | 40 | 25 | MR §1.6 |
| 13–18 | III | 20 | 45 | 35 | MR §1.6 |
| 19–20 | IV | 20 | 50 | 30 | `V1-EXCEPTION` §6.7 |
| 21–24 | IV | 0 | 60 | 40 | `LOCKED-V1` [Final Lock 2] |

The 20/50/30 split for turns 19–20 and the 0/60/40 split for turns 21–24 are `LOCKED-V1` [Final Lock 2]. These rows supersede every earlier turn-19–24 weight.

### 2.7 Perk pool shape — `LOCKED` (MR §2.2, §CONST `epoch_perk_pool`)

| Archetype | Pool size | Constraint |
|---|---|---|
| Economic | 16 | — |
| Military | 16 | — |
| Tempo | 12 | — |
| Conversion | 8 | — |
| Keystone | 8 | Age III+ only; **maximum one per run** |
| **Total** | **60** | |

### 2.8 Movement and stacking — `LOCKED` / `DERIVED`

| Constant | Value | Label | Source |
|---|---|---|---|
| Base push distance | 1 tile per turn | `LOCKED` | §1.2 "lanes push forward one tile" |
| Coast push distance | 2 tiles per turn | `LOCKED` | §1.3 |
| Max units per lane | 3 | `LOCKED` | §2.3 |
| Effective lane power | `Σ (power_i × multiplier_i)`, units sorted by descending effective pre-stacking Power, then earliest `trainedOnTurn`, then ascending stable `instanceId` | `LOCKED-V1` [Final Lock 1] | Apply multipliers 1.00, 0.75, 0.50 in that order. |
| Movement simultaneity | Simultaneous, one tile-step at a time, blocking checked before each step | `LOCKED-V1` [Lock 11] | — |
| Pass-through / swap past an enemy | Forbidden | `LOCKED-V1` [Lock 11] | — |
| Combat timing | After **all** movement steps; never between COAST steps | `LOCKED-V1` [Lock 11] | — |
| Player unit entry tile | 1 | `LOCKED-V1` [Lock 8] | — |
| Snapshot unit entry tile | 5 | `LOCKED-V1` [Lock 8] | — |
| Newly trained unit acts same turn | Yes — moves and fights on the turn it is trained | `LOCKED-V1` [Lock 8] | — |
| Capital | Presentation-only; no HP; cannot be attacked; reaching the far end does not end the match | `LOCKED-V1` [Lock 14] | — |

### 2.9 Scoring — `LOCKED-V1` [Lock 4]

| Constant | Value |
|---|---|
| `POINTS_PER_EXCLUSIVE_CONTESTED_TILE` | **1** |
| Evaluation point | After movement **and** combat resolve |
| Points available per turn | 0–3 (one per lane) |
| Disputed contested tile | 0 points to either side |
| Unoccupied contested tile | 0 points to either side |
| Accumulation | Cumulative across all 24 turns; never decreases |
| `MAX_THEORETICAL_SCORE` | 72 (`DERIVED`: 24 × 3 × 1) |
| Victory condition | Higher score at turn 24 |
| Tie condition | Equal scores → `TIE` |

This supersedes the 2-points-per-tile figure recommended in the pre-Lock draft of this document. All worked examples and golden tests below use 1 point.

`DERIVED` observation for economy simulation, not a rule: MR §7.4.2 displays a reference result of 41–38. Under the 1-point rule that implies the winner exclusively held ~1.71 of 3 contested tiles on every one of 24 turns, which is a high sustained board presence. Either the reference numbers are illustrative only, or contested-tile control is expected to be near-permanent for the leading side. Worth measuring in the 500-run simulation before production balance lock.

### 2.10 Determinism — `LOCKED-V1` [Lock 19]

| Constant | Value |
|---|---|
| PRNG class | Versioned, explicitly specified, cross-platform |
| Streams | Independent and **indexed**: card offers, lane modifiers, deterministic tie-breaking |
| Language-native / engine-native RNG | **Forbidden** in authoritative simulation |

The authoritative algorithm is PCG32 XSH-RR 64/32, with SplitMix64 used only for indexed seed derivation, pinned by `rulesVersion = 1.2.0-v1-final` (§10.2). This is `LOCKED-V1` [Final Lock 5].

---

## 3. Formal run state machine

### 3.1 States

| ID | State | Occurs |
|---|---|---|
| `S00` | `RUN_INIT` | Once |
| `S01` | `SEED_INIT` | Once |
| `S02` | `SNAPSHOT_LOAD` | Once |
| `S03` | `LANE_MODIFIER_ASSIGN` | Once |
| `S04` | `RUN_READY` | Once |
| `S10` | `TURN_START` | ×24 |
| `S11` | `CARD_GENERATION` | ×24 |
| `S12` | `CHOICE_WINDOW` | ×24 |
| `S13` | `ACTION_VALIDATION` | ×24 |
| `S14` | `COST_PAYMENT` | ×24 |
| `S15` | `CARD_EFFECT_APPLY` | ×24 |
| `S16` | `INCOME` | ×24 |
| `S17` | `MOVEMENT` | ×24 |
| `S18` | `CONTESTED_RESOLUTION` | ×24 |
| `S19` | `COMBAT` | ×24 |
| `S20` | `SCORE_UPDATE` | ×24 |
| `S21` | `AGE_TRANSITION_CHECK` | ×24 |
| `S22` | `TURN_COMPLETE` | ×24 |
| `S30` | `MATCH_COMPLETE` | Once |
| `S31` | `REPLAY_RECORD_CREATE` | Once |
| `S99` | `RUN_FINALIZED` | Terminal |

`S18` (`CONTESTED_RESOLUTION`) is retained as a distinct state but, under [Lock 4] and [Lock 15], ownership is now computed **once per turn, after combat**, and the result is persisted for the next turn's HIGHLAND check. See §4.3.

### 3.2 State transition table

| From | Event / condition | To | Label |
|---|---|---|---|
| `S00` | `beginRun(rulesVersion, contentVersion, seed, snapshotRef)` | `S01` | `PROVISIONAL` |
| `S01` | RNG streams derived from seed (§10.2) | `S02` | `PROVISIONAL` |
| `S02` | Snapshot loaded and version-validated | `S03` | `LOCKED` (§1.4 requires a snapshot) |
| `S02` | Snapshot `rulesVersion` or `contentVersion` mismatch | Snapshot is **not selectable for the run**; no run is created | `LOCKED-V1` [Lock 18] |
| `S03` | Three lane modifiers assigned from `rngLaneModifier` | `S04` | `LOCKED` (§1.3) / procedure `OPEN-V1-012` |
| `S04` | — | `S10` with `turn = 1` | `DERIVED` |
| `S10` | — | `S11` | `DERIVED` |
| `S11` | 3 offers generated from `rngCardOffer`, identical for both sides | `S12` | `LOCKED` (§1.4 same-seed rule) |
| `S12` | Player submits `selectedOfferIndex` (+ target) | `S13` | `LOCKED` (§1.2 one decision) |
| `S12` | Timeout / no input | `S13`; treated as no selection, resolving to PASS if nothing legal is chosen | `PROVISIONAL` |
| `S13` | Action legal | `S14` | `DERIVED` |
| `S13` | Action illegal | back to `S12`, offer unchanged, no state mutation | `PROVISIONAL` |
| `S13` | No offer is legal for a side | `S16` for that side: **PASS** recorded, no card effect, offers **not** rerolled or replaced | `LOCKED-V1` [Lock 12] |
| `S14` | Resources debited | `S15` | `LOCKED` (§2.1 costs) |
| `S15` | Card effect applied to authoritative state | `S16` | `LOCKED` (§1.2) |
| `S16` | Income credited to both sides | `S17` | `LOCKED` (§1.2 "yields tick" precedes movement) |
| `S17` | All units advance | `S18` | `LOCKED` (§1.2 "lanes push forward one tile") |
| `S18` | (no-op pre-combat; ownership is computed at `S20`) | `S19` | `LOCKED-V1` [Lock 4] |
| `S19` | Combat resolved in every contested tile | `S20` | `LOCKED` (§1.2 "contested tiles fight") |
| `S20` | Ownership computed; `score += 1` per exclusively held contested tile; ownership persisted as `tile3HolderPrevTurn` | `S21` | `LOCKED-V1` [Lock 4], [Lock 15] |
| `S21` | `turn % 6 == 0` and `turn < 24` → Age advances | `S22` | `LOCKED-V1` [Lock 1] — Ages are fixed to turn ranges; ADVANCE never accelerates them [Lock 2] |
| `S21` | otherwise | `S22` | — |
| `S22` | `turn < 24` → `turn += 1` | `S10` | `DERIVED` |
| `S22` | `turn == 24` | `S30` | `LOCKED` (§1.4) |
| `S30` | `MatchResult` computed from score differential | `S31` | `LOCKED` (§1.4) |
| `S31` | `ReplayEvent[]` sealed and hashed | `S99` | `PROVISIONAL` |

**Invariant SM-1:** `S11` through `S22` execute exactly 24 times, in order, with no skips.
**Invariant SM-2:** No state before `S14` mutates authoritative state. `S11`–`S13` are pure.
**Invariant SM-3:** Both sides advance through the same state at the same turn index. Simulation is lockstep, not interleaved.

---

## 4. Canonical order of operations

### 4.1 The authoritative sequence

MR §1.2 states the resolution order verbatim:

> "After the pick the board auto-resolves: yields tick, lanes push forward one tile, contested tiles fight."

This single sentence locks **card → income → movement → combat**. The full expansion:

| Step | Phase | Applies to | Label | Source |
|---|---|---|---|---|
| 1 | Turn begins; `turn`, `age` fixed for this turn | Both | `DERIVED` | §1.2 |
| 2 | Generate 3 offers from `rngCardOffer(turn)` | Shared | `LOCKED` | §1.4 |
| 3 | Player selects; Snapshot's recorded selection read | Both | `LOCKED` | §1.2, §1.4 |
| 4 | Validate both selections | Both | `DERIVED` | — |
| 5 | Pay cost | Both | `DERIVED` | §2.1 |
| 6 | **Apply card effect.** Both sides' effects are applied to the same pre-effect state, then committed together — no side sees the other's effect while resolving its own. | Both | `DERIVED` from [Lock 6] simultaneity | §1.2 |
| 7 | **Income ticks.** Uses `tile3HolderPrevTurn` for contested income. | Both | `LOCKED` | §1.2 |
| 8 | **Movement.** Simultaneous, one tile-step at a time; blocking re-checked before each step; COAST takes a second step. **No combat between steps.** | Both | `LOCKED-V1` [Lock 11] | §1.2 |
| 9 | *(reserved — no ownership computation here)* | — | `LOCKED-V1` [Lock 4] | — |
| 10 | **Combat** in every tile containing units of both sides. Damage computed for both sides from the identical pre-damage state. | Both | `LOCKED` + `LOCKED-V1` [Lock 6] | §1.2 |
| 11 | Apply all damage simultaneously, then remove units at `hp <= 0` — after every lane has resolved. | Both | `LOCKED-V1` [Lock 6] | — |
| 12 | Compute contested ownership: a side **exclusively holds** tile 3 when it has ≥1 living unit there and the opponent has none. | Both | `LOCKED-V1` [Lock 4] | — |
| 13 | **Score update:** `score += 1` per exclusively held contested tile (0–3 per side per turn). | Both | `LOCKED-V1` [Lock 4] | — |
| 14 | Persist ownership as `lane.tile3HolderPrevTurn` for next turn's HIGHLAND and income. | Run | `LOCKED-V1` [Lock 15] | — |
| 15 | Age transition check: `turn % 6 == 0 and turn < 24` → `age += 1`, effective next turn. | Run | `LOCKED-V1` [Lock 1] | §2.2 |
| 16 | Turn complete; emit `ReplayEvent` with `stateHash`. | Run | `PROVISIONAL` | — |

### 4.2 The nine ambiguity questions, answered

| # | Question | Answer | Label |
|---|---|---|---|
| Q1 | Does income occur before or after the selected card resolves? | **After.** MR §1.2 orders "the pick" then "yields tick". A structure placed this turn **does** produce income on the turn it is placed. | `LOCKED` (order) / `DERIVED` (same-turn yield) |
| Q2 | When does movement happen? | Step 8, after income, before combat. | `LOCKED` §1.2 |
| Q3 | When are lane modifiers applied? | RIVER at step 6 (modifies the structure's yield at placement, persisting). COAST at step 8 (grants the second movement step). HIGHLAND at step 10 (combat power, per [Lock 15]). | `LOCKED-V1` [Lock 9], [Lock 11], [Lock 15] |
| Q4 | When are counters calculated? | Step 10, before the damage curve, per MR §2.3 "applied before the damage curve". | `LOCKED` |
| Q5 | Can a newly trained unit move or fight immediately? | **Yes.** Player units enter at tile 1, Snapshot units at tile 5, and both participate in movement and combat on the turn they are trained. | `LOCKED-V1` [Lock 8] |
| Q6 | When is a destroyed unit removed? | Step 11, **after all damage in all lanes is applied**. A unit destroyed this turn still dealt its damage this turn. | `LOCKED-V1` [Lock 6] |
| Q7 | When does contested-tile ownership change? | Computed **once**, at step 12, after combat. Income at step 7 and the HIGHLAND bonus at step 10 both read `tile3HolderPrevTurn`, persisted at step 14 of the previous turn. | `LOCKED-V1` [Lock 4], [Lock 15] |
| Q8 | When does Age advancement become effective? | At step 15, taking effect from the **next** turn. Turn 6 resolves entirely under Age I; turn 7 is the first Age II turn. ADVANCE never accelerates this. | `LOCKED-V1` [Lock 1], [Lock 2] |
| Q9 | What if both sides enter the same tile simultaneously? | Blocking is evaluated against **pre-step occupancy**. Two units advancing toward each other across an empty tile therefore **both enter it**, and that tile becomes a combat site at step 10. A unit never enters a tile that already held an enemy at the start of that step, and units never swap or pass through. | `LOCKED-V1` [Lock 11] |

### 4.3 Ownership is computed once, and read from the previous turn

`LOCKED-V1` [Lock 4], [Lock 15].

Ownership of a lane's tile 3 is computed exactly once per turn, at step 12, after combat. Three consumers read it:

| Consumer | Reads | Step |
|---|---|---|
| Score (this turn) | the value just computed at step 12 | 13 |
| Contested income (next turn) | `tile3HolderPrevTurn` | 7 |
| HIGHLAND +3 (next turn) | `tile3HolderPrevTurn` | 10 |

`lane.tile3HolderPrevTurn` is written at step 14 and is the **only** ownership value visible on the following turn. On turn 1 it is `null` for all lanes, so no contested income and no HIGHLAND bonus apply on turn 1.

**Invariant OW-1:** `lane.ownedBy` is null or a single Side. It is never both, and it never persists from a turn in which the tile was empty.
**Invariant OW-2:** the sum of points awarded across both sides in one turn is ≤ 3.

---

## 5. Game-state data model

Implementation-neutral. Types are abstract: `int` = 32-bit signed integer, `float` = IEEE-754 double, `enum` = closed set, `id` = stable string key.

Field annotations:
- **A** = authoritative (part of the simulation state; must be serialized)
- **D** = derived (recomputable; must never be the source of truth)
- **P** = presentation-only (never affects simulation)

### 5.1 Enums

```
enum Side          { PLAYER, SNAPSHOT }
enum CardType      { BUILD, TRAIN, ADVANCE }
enum PerkArchetype { ECONOMIC, MILITARY, TEMPO, CONVERSION, KEYSTONE }
enum UnitClass     { SWORD, SPEAR, HORSE }
enum LaneModifier  { RIVER, HIGHLAND, COAST }
enum LaneId        { A, B, C }              // A = left, B = centre, C = right   (MR §1.3)
enum ResourceType  { GROWTH, INSIGHT }
enum MatchOutcome  { VICTORY, DEFEAT, TIE }
enum RunPhase      { /* the S-codes of §3.1 */ }
```

### 5.2 `RunState`

```
RunState {
  rulesVersion        : string    A  required   semver, e.g. "1.0.0"
  contentVersion      : string    A  required   semver, e.g. "0.1.0"
  seed                : SeedState A  required
  turn                : int       A  required   range 1..24, default 1
  age                 : int       A  required   range 1..4,  default 1
  phase               : RunPhase  A  required   default RUN_INIT
  lanes               : LaneState[3]      A required
  player              : PlayerState       A required
  snapshot            : PlayerState       A required
  snapshotSource      : OpponentSnapshot  A required
  replay              : ReplayEvent[]     A required   default []
  result              : MatchResult?      A optional    null until MATCH_COMPLETE
  contentPoolHash     : string    A  required   hash of the loaded card/perk pool; determinism guard
}
```

**Invariant RS-1 (`LOCKED-V1` [Lock 1]):** `age == floor((turn - 1) / 6) + 1`, always. Ages are fixed to turn ranges and nothing accelerates them.

### 5.3 `PlayerState`

```
PlayerState {
  side            : Side           A required
  growth          : int            A required   >= 0, default 8            // [Lock 3]
  insight         : int            A required   >= 0, default 2            // [Lock 3]
  score           : int            A required   0..72, default 0           // [Lock 4]
  structures      : StructureInstance[]  A required  default []
  units           : UnitInstance[]       A required  default []
  perks           : PerkDefinition[]     A required  default []
  keystoneTaken   : bool           A required   default false              // MR §2.2 max one
  growthPerTurn   : int            D            recomputed each turn
  insightPerTurn  : int            D            recomputed each turn
  entryTile       : int            A required   1 for PLAYER, 5 for SNAPSHOT   // [Lock 8]
  capitalLane     : LaneId         P optional   presentation-only              // [Lock 14]
  displayName     : string         P optional
}
```

**Invariant PS-1:** `growth >= 0` and `insight >= 0` at all times. Costs are never paid into deficit.
**Invariant PS-2:** `perks.filter(p => p.archetype == KEYSTONE).length <= 1` (MR §2.2).
**Invariant PS-3:** `perks` contains no duplicate `perkId` for a given side (`LOCKED-V1` [Lock 13]).
**Invariant PS-4:** `score` is monotonically non-decreasing and increases by 0..3 per turn (`LOCKED-V1` [Lock 4]).

### 5.4 `TurnState`

```
TurnState {
  turn              : int         A required  1..24
  age               : int         A required  1..4
  offers            : CardOffer[3]        A required
  playerSelection   : Selection?  A optional  null until CHOICE_WINDOW closes
  snapshotSelection : Selection   A required  read from OpponentSnapshot
  events            : ReplayEvent[]        A required
}

Selection {
  offerIndex : int      A required  0..2, or -1 for PASS                    // [Lock 12]
  targetLane : LaneId?  A optional  required for BUILD and TRAIN; forbidden for ADVANCE and PASS   // [Lock 9], [Lock 8]
  // targetTile does not exist in V1: structures are lane-scoped [Lock 9] and
  // units always enter the side's fixed entryTile [Lock 8].
}
```

### 5.5 `AgeState`

```
AgeState {
  index          : int  A required 1..4
  name           : enum { DAWN, BRONZE, STEEL, MODERN }  A required
  firstTurn      : int  A required   { 1, 7, 13, 19 }
  lastTurn       : int  A required   { 6, 12, 18, 24 }
  perkCost       : int  A required   { 2, 3, 5, 9 }      // Insight
  unitPower      : int  A required   { 10, 12, 14, 17 }
  unitCost       : int  A required   { 6, 8, 11, 16 }    // Growth
  buildCost      : int  A required   { 8, 11, 16, 22 }   // Growth
  structureTier  : int  A required   { 1, 2, 3, 4 }; equals the current Age
}
```

`AgeState` is a static lookup table, not mutable state. It is loaded from content and hashed into `contentPoolHash`.

### 5.6 `LaneState`

```
LaneState {
  id             : LaneId        A required
  modifier       : LaneModifier  A required
  tiles          : TileState[5]  A required
  contestedTile      : int    A required  constant 3   (MR §1.3)
  ownedBy            : Side?  A required  null = unowned or disputed; computed at step 12   // [Lock 4]
  tile3HolderPrevTurn: Side?  A required  default null; written at step 14                  // [Lock 15]
  engagementId       : int    A required  default 0; incremented when a tile-3 engagement ends  // [Lock 17]
  playerPower    : float         D           effective, post-stacking
  snapshotPower  : float         D           effective, post-stacking
}
```

**Invariant LS-1:** `tiles.length == 5`.
**Invariant LS-2:** units of a given Side in a lane ≤ 3 (MR §2.3).

### 5.7 `TileState`

```
TileState {
  laneId      : LaneId  A required
  index       : int     A required  1..5   // 1 = player capital end, 5 = snapshot capital end
  units       : UnitInstance[]  A required default []
  // structures do not occupy tiles in V1 [Lock 9]; they live on
  // PlayerState.structures and carry a laneId
  isContested : bool    D  == (index == 3)
  isCapital   : bool    P  == (index == 1 || index == 5) — presentation-only   // [Lock 14]
}
```

### 5.8 `UnitInstance`

```
UnitInstance {
  instanceId   : id          A required  unique within run
  owner        : Side        A required
  cardId       : id          A required  the TRAIN CardDefinition it came from
  unitClass    : UnitClass   A required
  basePower    : int         A required  = AgeState.unitPower at time of training
  hp           : int         A required  0..100, default 100       // [Lock 5]
  maxHp        : int         A required  constant 100               // [Lock 5]
  laneId       : LaneId      A required
  tileIndex    : int         A required  1..5
  hasReach     : bool        A required  default false             // MR §2.3
  trainedOnTurn: int         A required
  reachGuardUsedInEngagement : int?  A optional  the LaneState.engagementId in which this
                                                  unit already consumed its first-clash
                                                  protection; null = unused   // [Lock 17]
  stackRank    : int         D           0..2, index into STACK_POWER_MULTIPLIERS
  effectivePower : float     D           basePower × stackMultiplier × modifiers
}
```

**Invariant UI-1:** `basePower` is fixed at training time and never retroactively updated by Age advancement. (`DERIVED` from MR §2.2's argument that older units stay playable.)

### 5.9 `StructureInstance`

```
StructureInstance {
  instanceId    : id      A required
  owner         : Side    A required
  cardId        : id      A required
  tier          : int     A required  1..4
  yieldType     : ResourceType  A required
  yieldAmount   : int     A required  { 1, 2, 3, 4 } + RIVER bonus
  laneId        : LaneId  A required   structures are lane-scoped   // [Lock 9]
  builtOnTurn   : int     A required
  paybackTurns  : float   D  = cost / yieldAmount — recorded for balance telemetry only,
                             never used for runtime legality   // V1-EXCEPTION §6.7
}
```

**Invariant SI-1:** a StructureInstance is immutable after creation. Structures cannot be attacked and are never destroyed (`LOCKED-V1` [Lock 9]).
**Invariant SI-2:** there is no cap on structures per lane or per run (`LOCKED-V1` [Lock 9]). The 24-turn limit bounds the count at 24.
**Invariant SI-3:** structures do not occupy tiles and never block movement (`LOCKED-V1` [Lock 9]).

### 5.10 `CardDefinition`

```
CardDefinition {
  cardId        : id         A required  stable across content versions
  cardType      : CardType   A required
  minAge        : int        A required  1..4
  maxAge        : int?       A optional  null = no cap
  costResource  : ResourceType  A required   // GROWTH for BUILD/TRAIN, INSIGHT for ADVANCE (MR §1.2)
  // BUILD fields
  // BUILD tier and base yield are resolved from AgeState; neither is selectable or authored on the card
  yieldType     : ResourceType?  A optional
  yieldAmount   : int?       A optional
  // TRAIN fields
  unitClass     : UnitClass? A optional
  grantsReach   : bool       A required default false
  // ADVANCE fields
  perkId        : id?        A optional
  displayName   : string     P required
  displayText   : string     P required
}
```

Cost is **not** stored on the card: it is read from `AgeState` for the current Age (MR §2.2). A BUILD creates a structure whose tier equals the Age in which the BUILD resolves and whose base yield equals that Age's tier yield. Lower tiers are not separately purchasable. Existing structures retain their original tier and yield and never auto-upgrade. `LOCKED-V1` [Final Lock 3].

### 5.11 `CardOffer`

```
CardOffer {
  offerIndex   : int   A required 0..2
  cardId       : id    A required
  resolvedCost : int   D  from AgeState at offer time
  affordable   : bool  D  per side; must be computed per side, not stored globally
  legalTargets : LaneId[]  D  lanes where this card can legally be placed
}
```

**Invariant CO-1:** the three `cardId` values offered on turn *n* are identical for PLAYER and SNAPSHOT (MR §1.4 same-seed rule).

### 5.12 `PerkDefinition`

```
PerkDefinition {
  perkId       : id             A required
  archetype    : PerkArchetype  A required
  minAge       : int            A required   // KEYSTONE => 3 (MR §2.2)
  stackable    : bool           A required   // OPEN-V1-021
  effects      : ActiveEffect[] A required
  displayName  : string         P required
  displayText  : string         P required
}
```

### 5.13 `ActiveEffect`

```
ActiveEffect {
  effectId     : id      A required
  sourceType   : enum { LANE_MODIFIER, STRUCTURE, PERK, COMMANDER }  A required; COMMANDER reserved
  sourceId     : id      A required
  trigger      : enum { ON_INCOME, ON_MOVEMENT, ON_COMBAT_PRE, ON_COMBAT_POST, ON_TURN_END, CONTINUOUS }  A required
  scope        : enum { GLOBAL, LANE, UNIT_CLASS, SIDE }  A required
  scopeValue   : string?  A optional
  operation    : enum { ADD, MULTIPLY }  A required
  targetStat   : string   A required   // e.g. "growthPerTurn", "effectivePower"
  magnitude    : float    A required
  priority     : int      A required   // default ADD 100; default MULTIPLY 200 unless explicitly authored
  appliesFrom  : int      A required   // turn index; effects are not retroactive
}
```

**Ordering rule (`LOCKED-V1` [Final Lock 4]).** Collect all applicable effects, then partition by operation so every ADD resolves before every MULTIPLY. Within each operation partition sort by ascending numeric `priority`, then `sourceType` (`LANE_MODIFIER`, `STRUCTURE`, `PERK`, `COMMANDER`), then ascending stable `sourceId`. Resolve the ADD partition, resolve the MULTIPLY partition, apply stat-specific clamping, then the stat's defined rounding rule. The default priority is 100 for ADD and 200 for MULTIPLY; authored priorities may override it. `sourceId` must be unique within its source namespace. Identical inputs therefore produce an identical total order.

### 5.14 `OpponentSnapshot`

```
OpponentSnapshot {
  snapshotId       : id       A required
  rulesVersion     : string   A required   must equal RunState.rulesVersion
  contentVersion   : string   A required   must equal RunState.contentVersion
  seed             : string   A required   the seed the snapshot was recorded under
  selections       : Selection[24]  A required   one per turn, in order
  recordedScore    : int      A required   for validation only
  recordedAt       : timestamp  P optional
  displayName      : string   P optional
  ladderRating     : int      P optional
}
```

**Invariant OS-1:** replaying `selections` against the same `seed`, `rulesVersion` and `contentVersion` must reproduce `recordedScore` exactly. A mismatch is a determinism failure and must hard-fail in test builds.

**Invariant OS-2 (`LOCKED-V1` [Lock 18]):** a snapshot whose `rulesVersion` or `contentVersion` differs from the run is **not selectable** — the run is never created. If the versions match and a recorded selection is nonetheless illegal, that is a **determinism error**:

| Build type | Behaviour |
|---|---|
| Test / CI | **Fail loudly.** Abort the run and surface the divergent turn index. |
| Production | Record a **PASS** for that turn and emit an error event. Do not silently substitute another card. |

### 5.15 `SeedState`

```
SeedState {
  masterSeed       : string  A required   the shareable code, e.g. "K7-QMRA-92"
  masterSeedInt    : uint64  D            canonical decode of masterSeed
  streamCardOffer  : uint64  A required   derived, see §10.2
  streamLaneMod    : uint64  A required   derived
  streamTieBreak   : uint64  A required   derived
  algorithm        : string  A required   "pcg32-xsh-rr-64-32+splitmix64-v13", pinned by rulesVersion
}
```

### 5.16 `ReplayEvent`

```
ReplayEvent {
  sequence     : int     A required   monotonic from 0
  turn         : int     A required
  phase        : RunPhase A required
  side         : Side?   A optional
  eventType    : enum { OFFER_GENERATED, CARD_SELECTED, COST_PAID, EFFECT_APPLIED,
                        INCOME_GRANTED, UNIT_MOVED, OWNERSHIP_CHANGED, COMBAT_RESOLVED,
                        UNIT_DESTROYED, SCORE_CHANGED, AGE_ADVANCED, TURN_COMPLETED }  A required
  payload      : object  A required   event-specific, schema per eventType
  stateHash    : string  A required   hash of authoritative RunState after this event
}
```

**Invariant RE-1:** replaying all `ReplayEvent`s from `RUN_INIT` reproduces every `stateHash` bit-for-bit. This is the determinism test.

### 5.17 `MatchResult`

```
MatchResult {
  runId            : id       A required
  rulesVersion     : string   A required
  contentVersion   : string   A required
  seed             : string   A required
  snapshotId       : id       A required
  playerScore      : int      A required  0..72   // [Lock 4]
  snapshotScore    : int      A required  0..72   // [Lock 4]
  scoreDifferential: int      D  playerScore - snapshotScore
  outcome          : MatchOutcome  D  > 0 VICTORY, < 0 DEFEAT, == 0 TIE   // [Lock 4] for TIE
  finalTurn        : int      A required        constant 24
  playerSelections : Selection[24]  A required
  replayHash       : string   A required
  divergenceTurns  : int[]    P optional        for the replay screen (MR §7.4.2)
}
```

---

## 6. Card action contracts

### 6.0 Shared offer generation — `LOCKED-V1` [Final Lock 2]

Generate slots 0, 1 and 2 with three independent weighted `CardType` draws using the effective table in §2.6. Duplicate card types are permitted. After each type draw, select uniformly from content entries of that type that are offer-eligible for the current Age. A hand may not contain duplicate `cardId` values: on collision, redraw that slot from the same indexed stream using `redrawAttempt = 1, 2, ...` until its ID is distinct. Content validation must guarantee at least three eligible IDs whenever a type has non-zero weight.

The generated hand is shared and immutable for the turn. Owned perks, resources, lane capacity, prior choices, and other per-side state never affect generation. PLAYER and SNAPSHOT always see identical offers. Legality is evaluated afterward per side; an illegal card remains visible but unselectable. If none are legal, §6.6 PASS applies. The indexed RNG address is defined in §10.2, so collision redraws cannot disturb another turn or slot.

### 6.1 Common validation pipeline

Every selection passes these gates in order. Failing any gate rejects the selection without mutating state.

| Gate | Check | Failure |
|---|---|---|
| G1 | `offerIndex` in 0..2 | `ERR_INVALID_INDEX` |
| G2 | Card's `minAge <= currentAge <= maxAge` | `ERR_AGE_LOCKED` |
| G3 | Side can pay `resolvedCost` in the card's `costResource` | `ERR_UNAFFORDABLE` |
| G4 | Card-type-specific legality (§6.2–6.4) | type-specific |
| G5 | Target present iff required, and within enum range | `ERR_BAD_TARGET` |

### 6.2 BUILD contract

| Rule | Statement | Label | Source |
|---|---|---|---|
| B1 | Cost = `AgeState[age].buildCost`, paid in Growth | `LOCKED` | §2.2, §2.1 |
| B2 | Structure tier = `AgeState[age].structureTier`; base yield is that tier's yield. The card has no tier choice. Existing structures retain their original tier and yield. | `LOCKED-V1` [Final Lock 3] | — |
| B3 | Yield = `STRUCTURE_YIELD_TIER_n`, +1 additional Growth if the target lane's modifier is `RIVER` and the yield type is Growth | `LOCKED` | §2.1, §1.3 |
| B4 | **The viability rule is NOT enforced at runtime.** It is a content-balance target only. A BUILD action is never rejected because of it. | `V1-EXCEPTION` | §6.7 |
| B5 | Requires a `targetLane`. Structures are lane-scoped and occupy no tile. | `LOCKED-V1` [Lock 9] | — |
| B6 | There is no `targetTile`. | `LOCKED-V1` [Lock 9] | — |
| B7 | Structures cannot be attacked or destroyed, and never move. No cap per lane or per run. | `LOCKED-V1` [Lock 9] | — |
| B8 | Structure yield begins on the turn of placement. | `DERIVED` | §1.2 ordering (Q1) |
| B9 | BUILD is offered on turns 1–20 and has 0% offer weight on turns 21–24. | `V1-EXCEPTION` | §6.7 |

The arithmetic that motivated the exception in §6.7 is retained here for traceability. Applying B1, B3 and the MR viability inequality to the locked Age table:

| Age | Build cost | Tier yield | `cost/yield` | Max turn satisfying the inequality | Turns in Age | Would be legal |
|---|---|---|---|---|---|---|
| I | 8 | +1 | 8.00 | 8.0 | 1–6 | all |
| II | 11 | +2 | 5.50 | 13.0 | 7–12 | all |
| III | 16 | +3 | 5.33 | 13.3 | 13–18 | turn 13 only |
| IV | 22 | +4 | 5.50 | 13.0 | 19–24 | none |

Under the MR rule the economy would close at turn 14 while MR §1.6 continues offering BUILD to turn 18 — the contradiction that produced `OPEN-V1-008`. **Resolved by owner decision:** see §6.7.

### 6.3 TRAIN contract

| Rule | Statement | Label | Source |
|---|---|---|---|
| T1 | Cost = `AgeState[age].unitCost`, paid in Growth | `LOCKED` | §2.2 |
| T2 | Unit `basePower` = `AgeState[age].unitPower`, fixed at training | `LOCKED` | §2.2 |
| T3 | Requires a `targetLane` | `LOCKED` | §1.2 "Deploys a unit into a lane" |
| T4 | Placement tile: **1** for PLAYER, **5** for SNAPSHOT | `LOCKED-V1` [Lock 8] | — |
| T5 | Illegal if the target lane already holds 3 units of that side | `LOCKED` | §2.3 |
| T6 | If all three lanes are full, the TRAIN card is unselectable in every lane; if it is the only otherwise-legal offer, PASS applies | `DERIVED` from T5 + [Lock 12] | — |
| T7 | `unitClass` comes from the CardDefinition | `LOCKED` | §2.3 |
| T8 | `hasReach` comes from the CardDefinition | `LOCKED` | §2.3 |
| T9 | A unit trained this turn participates in movement **and** combat this turn | `LOCKED-V1` [Lock 8] | — |
| T10 | A unit enters at full HP (100) | `LOCKED-V1` [Lock 5] | — |
| T11 | Entry tile occupancy is not blocked by enemies — an enemy can never reach a side's own entry tile without first passing tile 3, which blocking prevents | `DERIVED` from [Lock 11] | — |

### 6.4 ADVANCE contract

| Rule | Statement | Label | Source |
|---|---|---|---|
| A1 | Cost = `AgeState[age].perkCost`, paid in Insight | `LOCKED` | §2.2 |
| A2 | Grants the card's `PerkDefinition` to the acquiring side | `LOCKED` | §2.2 |
| A3 | No target required; `targetLane` must be absent | `LOCKED-V1` [Lock 2] | — |
| A4 | Perk effects apply from the current turn forward, never retroactively; effects use the total order in §5.13 | `LOCKED-V1` [Final Lock 4] | — |
| A5 | **Perks are unique per side per run.** A perk already held by that side is unselectable for that side. It remains in the shared offer and stays selectable by the opposing side. | `LOCKED-V1` [Lock 13] | preserves CO-1 |
| A6 | **ADVANCE grants a perk and does not accelerate Age progression.** Ages are fixed to turn ranges. MR §1.2's phrase "pushes Age progress" is thematic language describing the deck's flavour, not a mechanic. | `LOCKED-V1` [Lock 1], [Lock 2] | resolves the former CONFLICT |

### 6.5 KEYSTONE contract

Keystone is an ADVANCE subtype, not a fourth card type (MR §2.2 lists it inside the perk pool). It inherits A1–A4 plus:

| Rule | Statement | Label | Source |
|---|---|---|---|
| K1 | `minAge = 3`. Not offered and not selectable in Ages I–II. | `LOCKED` | §2.2 |
| K2 | Maximum **one** Keystone per side per run. | `LOCKED` | §2.2 |
| K3 | Once `keystoneTaken == true`, further Keystone cards are unselectable for that side. | `DERIVED` from K2 | — |
| K4 | A Keystone card is still **offered** after one is taken; it is merely unselectable for the side that already holds one. Offers are never rerolled or replaced. | `LOCKED-V1` [Lock 12], [Lock 13] | — |

Offers must remain identical for both sides (CO-1 / MR §1.4). **Affordability and legality are evaluated per side**, never by mutating the shared offer.

### 6.6 When no offered card is legal

`LOCKED-V1` [Lock 12]. **The PASS rule:**

1. If at least one offer is legal for a side, that side **must** select a legal one. Voluntary passing is not permitted.
2. If no offer is legal, a **PASS** is recorded. No card effect is applied. No cost is paid.
3. The shared offers are **not** rerolled and **not** replaced. (Rerolling would desynchronise the two sides and break MR §1.4.)
4. Income, movement, combat and scoring proceed normally — steps 7–16 of §4.1 all execute.
5. A PASS is recorded as `Selection { offerIndex: -1, targetLane: null }` in the replay and in any snapshot derived from the run.
6. No compensation, refund or resource grant is given.

`DERIVED` reachability: with the [Lock 3] opening balance of 8 Growth / 2 Insight, turn 1 always has a legal action (ADVANCE costs 2). The earliest realistic PASS is turn 2 after a turn-1 BUILD, if the hand contains no ADVANCE. PASS frequency must be measured in the 500-run simulation (MR §10 item 2); HPP §8 notes that a mechanic players experience as "the game skipped my turn" is a retention risk if it is common.

### 6.7 V1 Build Economy Exception — `V1-EXCEPTION` (owner-approved)

**Status:** owner-approved departure from MR §2.1, scoped to V1, time-boxed to the economy-simulation gate below.

**The conflict.** MR §2.1's payback inequality `cost / yieldPerTurn <= (24 − turn) / 2`, applied to MR §2.2's Build costs and MR §2.1's tier yields, makes BUILD illegal from turn 14 onward (§6.2 table). MR §1.6 simultaneously offers BUILD at 20% weight through turn 18, and MR §2.1's prose says BUILD stops "in the last four turns." The three statements cannot all hold.

**The V1 decision.**

| # | Rule |
|---|---|
| X1 | The payback inequality is treated as a **content-balance target**, not runtime card legality. |
| X2 | A BUILD action is **never rejected** because of the payback formula. Gate G4 does not evaluate it. |
| X3 | BUILD may be offered and selected on **turns 1–20**. |
| X4 | BUILD has **0% offer weight on turns 21–24**. |
| X5 | `StructureInstance.paybackTurns` is still computed and recorded, for balance telemetry only. |

**Consequences to hold in view:**

- A turn-20 Age IV BUILD costs 22 Growth and returns +4/turn over the remaining 4 turns = 16 Growth. It is a **net loss of 6 Growth** and produces no score directly. Whether players correctly identify this as a trap, or experience it as a bad card the game offered them, is exactly what the simulation must answer.
- Removing runtime enforcement means the *only* thing preventing a dominated late BUILD is player judgement. That is a design position, not an oversight.

**Validation gate (required before production balance lock):** run the 500-run economy simulation (MR §10 item 2) and report (a) how often BUILD is taken on turns 14–20, (b) the win-rate delta between runs that take a late BUILD and runs that do not, and (c) whether late BUILD is ever correct. If late BUILD is never correct, the correct fix is a content change — lower late Build costs or raise late tier yields — not the reinstatement of a runtime rejection.

**Supersedes:** `OPEN-V1-008`, `OPEN-V1-009`.

---

## 7. Economy rules

### 7.1 Equations

Let `t` = current turn, `a` = current Age, `S` = a side.

```
E1  startingGrowth   = 8                                         [LOCKED-V1 Lock 3]
E2  startingInsight  = 2                                         [LOCKED-V1 Lock 3]

E3  baseIncomeGrowth  = 3                                        [LOCKED §2.1]
E4  baseIncomeInsight = 2                                        [LOCKED §2.1]

E5  structureIncome(S, r) = Σ over S.structures where yieldType == r of yieldAmount
                             where yieldAmount = TIER_YIELD[tier]
                                               + (1 if lane.modifier == RIVER and r == GROWTH else 0)
                                                                 [LOCKED §2.1, §1.3]

E6  contestedIncome(S, r) = 1 × count(lanes where lane.tile3HolderPrevTurn == S)
                            for r in { GROWTH, INSIGHT }         [LOCKED §2.1; reads
                            the PREVIOUS turn's ownership, per Lock 4 / Lock 15]

E7  perkIncome(S, r)   = Σ ActiveEffect where trigger == ON_INCOME and targetStat matches r
                                                                 [DERIVED §2.2]

E8  totalIncome(S, r)  = E3|E4 + E5 + E6 + E7

E9  growth'  = growth  + totalIncome(S, GROWTH)
E10 insight' = insight + totalIncome(S, INSIGHT)
```

**Payment (step 5 of §4.1):**

```
E11 cost = AgeState[a].buildCost | unitCost | perkCost   by CardType   [LOCKED §2.2]
E12 selection is legal only if S.resource >= cost                       [DERIVED]
E13 S.resource -= cost                                                  [DERIVED]
E14 no refunds exist in V1 — nothing in MR creates a refund path        [PROVISIONAL]
```

**Floors and caps:**

```
E15 resource floor = 0, hard. Costs never drive a resource negative.    [DERIVED from E12]
E16 Growth and Insight have no gameplay cap. Both are non-negative authoritative integers.
    A subtraction producing a negative value is illegal. Use an implementation-safe integer
    representation. Emit a telemetry warning above 999 and, in test builds, assert above
    1,000,000. These diagnostic thresholds never clamp, mutate, or otherwise affect match state.
    `LOCKED-V1` [Final Lock 6].

E18 score(S) += 1 × count(lanes where lane.ownedBy == S)   at step 13   [LOCKED-V1 Lock 4]
    where ownedBy is exclusive occupancy of tile 3 computed at step 12.
    Range per turn: 0..3. Cumulative. Never decreases.
```

**Build viability — advisory only in V1 (`V1-EXCEPTION` §6.7):**

```
E17 paybackTurns = cost / yieldAmount
    Recorded on every StructureInstance for telemetry.
    NOT evaluated as a legality gate. BUILD is legal on turns 1-20 regardless.
```

The RIVER +1 raises `yieldAmount` and therefore lowers `paybackTurns`, which remains true and worth measuring even though it no longer gates anything (see §13, GT-11).

### 7.2 Worked examples

All examples use the [Lock 3] opening balance of **Growth 8, Insight 2**, no perks, and a plain (non-RIVER) lane unless stated.

**Turns 1–3, Age I** (build 8 / unit 6 / perk 2). This is the actual opening sequence, not a hypothetical.

| Turn | Step | Event | Growth | Insight | Score |
|---|---|---|---|---|---|
| 1 | — | run start | 8 | 2 | 0 |
| 1 | 5–6 | pay BUILD 8; tier-1 Growth structure placed in Lane A | 0 | 2 | 0 |
| 1 | 7 | income: 3 base + 1 structure = 4 G; 2 base I. No contested income (turn 1: `tile3HolderPrevTurn` is null) | 4 | 4 | 0 |
| 1 | 13 | no units on any tile 3 | 4 | 4 | 0 |
| 2 | 5–6 | BUILD (8) and TRAIN (6) both unaffordable; pay ADVANCE 2 | 4 | 2 | 0 |
| 2 | 7 | income 4 G, 2 I | 8 | 4 | 0 |
| 3 | 5–6 | pay BUILD 8; second tier-1 structure | 0 | 4 | 0 |
| 3 | 7 | income: 3 + 2 = 5 G; 2 I | 5 | 6 | 0 |

Note turn 2: had the hand contained no ADVANCE, this would have been a **PASS** (§6.6) — the earliest reachable PASS in the game.

**Middle — turn 10, Age II** (build 11 / unit 8 / perk 3). Entering: Growth 9, Insight 12, three tier-1 Growth structures, `tile3HolderPrevTurn` = this side in 1 lane.

| Step | Event | Growth | Insight | Score |
|---|---|---|---|---|
| — | entering turn 10 | 9 | 12 | 6 |
| 5–6 | pay TRAIN 8; unit enters Lane B tile 1 at 100 HP | 1 | 12 | 6 |
| 7 | income: 3 base + 3 structures + 1 contested = 7 G; 2 base + 1 contested = 3 I | 8 | 15 | 6 |
| 8–12 | movement, combat; side ends exclusively holding 2 contested tiles | 8 | 15 | 6 |
| 13 | `score += 2` | 8 | 15 | **8** |

**Late — turn 20, Age IV** (build 22 / unit 16 / perk 9). Entering: Growth 26, Insight 21, five structures (+7 Growth), holding 2 contested tiles last turn.

| Step | Event | Growth | Insight | Score |
|---|---|---|---|---|
| — | entering turn 20 | 26 | 21 | 24 |
| 5–6 | pay ADVANCE 9 (Insight); perk acquired | 26 | 12 | 24 |
| 7 | income: 3 + 7 + 2 = 12 G; 2 + 2 = 4 I | 38 | 16 | 24 |
| 13 | holds 3 contested tiles → `score += 3` | 38 | 16 | **27** |

**A BUILD at turn 20 is legal under the V1 exception** (§6.7): cost 22, yield +4/turn, 4 turns remaining → returns 16 Growth for 22 spent. Legal, and a net loss of 6 Growth. The simulation must confirm whether players read this correctly.

### 7.3 Faucet/sink audit (informational, HPP §8)

| Faucet | Rate at turn 12 (typical) | Sink | Rate |
|---|---|---|---|
| Base Growth | 3/turn | BUILD | 11 |
| Structures | ~3–5/turn | TRAIN | 8 |
| Contested | 0–3/turn | — | — |
| Base Insight | 2/turn | ADVANCE | 3 |
| Contested Insight | 0–3/turn | — | — |

Insight faucet (2–5/turn) against an Age II sink of 3 implies Insight accumulates unless spent nearly every turn — which the §1.6 deck weighting (25% Advance at Age II) does not permit. Because V1 resources have no gameplay cap [Final Lock 6], Insight may pool. **This is a balance observation, not a rule.** It should be measured by the 500-run simulation and reported, not fixed here.

---

## 8. Board, movement, and stacking

### 8.1 Coordinates and orientation

```
Tile index:   1 ───── 2 ───── 3 ───── 4 ───── 5
              │               │               │
        PLAYER end       CONTESTED      SNAPSHOT end
```

| Rule | Statement | Label | Source |
|---|---|---|---|
| M1 | Tiles are indexed 1..5 within a lane. Index 1 is the PLAYER end, index 5 is the SNAPSHOT end. | `LOCKED` | §1.3 "capital at the bottom, opponent snapshot at the top. Tiles 1–2 are yours, 4–5 theirs, tile 3 is the contested middle." |
| M2 | Tile 3 is the only contested tile in a lane. | `LOCKED` | §1.3 |
| M3 | PLAYER units move in increasing tile index; SNAPSHOT units move in decreasing tile index. | `DERIVED` from M1 | — |
| M4 | Lanes are independent. No lateral movement between lanes exists. | `DERIVED` — MR describes lanes as "parallel" with no cross-lane mechanic | §1.3 |
| M5 | **Capitals are presentation-only.** They have no HP, cannot be attacked, occupy no simulation state, and reaching the opposing end does not end the match. Every match runs through turn 24. | `LOCKED-V1` [Lock 14] | §7.4.1 |

### 8.2 Movement

| Rule | Statement | Label | Source |
|---|---|---|---|
| M6 | Every unit advances 1 tile per turn toward the enemy end. | `LOCKED` | §1.2 |
| M7 | In a `COAST` lane, units advance 2 tiles per turn. | `LOCKED` | §1.3 |
| M8 | Movement is **simultaneous** for both sides and processed **one tile-step at a time**. | `LOCKED-V1` [Lock 11] | — |
| M9 | Blocking is re-checked **before each step**. A unit does not move onto a tile occupied by an enemy unit; it stops where it is. | `LOCKED-V1` [Lock 11] | — |
| M10 | Units never pass through or swap past enemy units. | `LOCKED-V1` [Lock 11] | — |
| M11 | A unit at the far end tile (5 for PLAYER, 1 for SNAPSHOT) does not advance further. Nothing happens on arrival. | `LOCKED-V1` [Lock 11], [Lock 14] | — |
| M12 | COAST grants a **second** movement step. Blocking is checked separately before it; a unit blocked on step 1 forfeits step 2. | `LOCKED-V1` [Lock 11] | — |
| M13 | **No combat occurs between COAST steps.** All combat happens after every movement step of every unit has resolved. | `LOCKED-V1` [Lock 11] | — |

### 8.3 Stacking

| Rule | Statement | Label | Source |
|---|---|---|---|
| M20 | Maximum 3 units per side per lane. | `LOCKED` | §2.3 |
| M21 | Effective lane power = `Σ power_i × [1.00, 0.75, 0.50]_i`. | `LOCKED` | §2.3 |
| M22 | Stack ordering for the multiplier is by descending effective pre-stacking Power; ties by earliest `trainedOnTurn`, then ascending stable `instanceId`. Apply 1.00, 0.75, 0.50 in that order. | `LOCKED-V1` [Final Lock 1] | — |
| M23 | The cap is per **lane**, not per tile. Units of one side in a lane may occupy different tiles. | `DERIVED` from [Lock 11] step-wise movement | — |
| M24 | Damage allocation is front-to-back: greatest tile index for PLAYER, lowest for SNAPSHOT; friendly units sharing a tile use M22 order. Overflow carries to the next eligible unit until damage is consumed or none remain. REACH protection can remove a unit from the eligible target list for that clash. | `LOCKED-V1` [Final Lock 1] | Damage between sides remains simultaneous. |

M22 and M24 are the only movement/stacking rules the Lock does not settle. [Lock 16] establishes a tie-break *precedent* — strongest contributing unit, then earliest trained turn, then stable instance ID — for dominant-class selection, and M22 should almost certainly mirror it, but the Lock does not say so and this document will not assume it.

Worked stack power values (for tests):

| Units (basePower) | Effective power |
|---|---|
| [10] | 10.00 |
| [10, 10] | 17.50 |
| [10, 10, 10] | 22.50 |
| [17, 14, 12] | 33.50 |
| [17, 17, 12] | 35.75 |

### 8.4 Contested tile ownership

| Rule | Statement | Label |
|---|---|---|
| M30 | A lane's contested tile is **exclusively held** by a side when that side has ≥1 living unit on tile 3 and the opposing side has none. | `LOCKED-V1` [Lock 4] |
| M31 | If both sides have living units on tile 3, the tile is **disputed**: no points, no contested income, no HIGHLAND bonus. | `LOCKED-V1` [Lock 4], [Lock 15] |
| M32 | If neither side has units on tile 3, the tile is **unoccupied**: no points to either side. Ownership does **not** persist from a previous turn. | `LOCKED-V1` [Lock 4] |
| M33 | Ownership is computed once per turn, at step 12, and persisted to `tile3HolderPrevTurn` at step 14. | `LOCKED-V1` [Lock 4], [Lock 15] |

MR §2.1 says only "Held contested tile: +1 Growth, +1 Insight — the reason to fight at all." [Lock 4] supplies the missing definition of *held* as **exclusive occupancy**, and [Lock 15] reuses it for HIGHLAND.

---

## 9. Combat specification

### 9.1 Formula — `LOCKED` (MR §2.3)

```
rawDelta   = sideEffectivePower − opposingEffectivePower
clamped    = clamp(rawDelta, −40, +40)                      [LOCKED  MR §2.3]
deltaPower = roundHalfAwayFromZero(clamped)   ∈ ℤ ∩ [−40,+40]   [LOCKED-V1 Lock 7]
damage     = DAMAGE_TABLE[deltaPower]                        [LOCKED-V1 Lock 7]
```

where `DAMAGE_TABLE` is a versioned, precomputed 81-entry integer table generated offline from `25 × exp(0.025 × deltaPower)` and shipped with the rules version. **Platform-native `exp()` must not be called during authoritative match resolution** [Lock 7].

Both sides compute damage against each other in the same step, from the identical pre-damage state [Lock 6].

### 9.2 Resolution order within a combat

| Step | Operation | Label |
|---|---|---|
| C1 | Determine which tiles contain units of both sides. Each is a combat site. | `LOCKED` §1.2 |
| C2 | For each side, compute stack-ordered effective power: `Σ basePower_i × stackMult_i` | `LOCKED` §2.3 |
| C3 | **Determine dominant class** for each side: the `UnitClass` contributing the most **post-stacking, pre-counter** effective Power. Ties break by strongest contributing unit, then earliest `trainedOnTurn`, then stable `instanceId`. | `LOCKED-V1` [Lock 16] |
| C4 | **Apply COUNTER once**, using the two dominant classes: if side X's dominant class counters side Y's, multiply X's effective power by 1.40. | `LOCKED` (bonus, pre-curve timing) + `LOCKED-V1` [Lock 16] |
| C5 | **Apply HIGHLAND:** if `lane.modifier == HIGHLAND`, add **+3** to the side recorded in `lane.tile3HolderPrevTurn`. If that value is null (disputed or unoccupied last turn), **neither side** gets the bonus. Applies only to combat occurring on that lane's tile 3. | `LOCKED-V1` [Lock 15] |
| C6 | **Apply REACH** (see §9.2.1). | `LOCKED-V1` [Lock 17] |
| C7 | Compute `rawDelta`, clamp to ±40, round half away from zero | `LOCKED` + `LOCKED-V1` [Lock 7] |
| C8 | Read integer damage for both sides from `DAMAGE_TABLE` | `LOCKED-V1` [Lock 7] |
| C9 | **Apply damage simultaneously** — both sides' damage derives from the same pre-damage state | `LOCKED-V1` [Lock 6] |
| C10 | Allocate damage within each stack per M24, carrying overflow across eligible units | `LOCKED-V1` [Final Lock 1] |
| C11 | Mark units with `hp <= 0` destroyed | `LOCKED-V1` [Lock 5] |
| C12 | Remove all destroyed units, after every lane has resolved | `LOCKED-V1` [Lock 6] |
| C13 | Surviving units do **not** move again this turn | `LOCKED-V1` [Lock 11] |

#### 9.2.1 REACH — `LOCKED-V1` [Lock 17]

| Rule | Statement |
|---|---|
| R1 | A REACH unit standing on **tile 2** of its own half may contribute its Power when friendly frontline units fight on **tile 3**. |
| R2 | On the **first such clash for that engagement**, the REACH unit takes **no return damage**. |
| R3 | The protection requires a **living friendly frontline unit on tile 3**. A REACH unit with no friendly unit on tile 3 contributes nothing and is not protected. |
| R4 | Protection consumption is tracked per unit: `unit.reachGuardUsedInEngagement = lane.engagementId`. |
| R5 | The engagement state resets — `lane.engagementId += 1`, clearing all guards in that lane — **only when the tile-3 engagement ends**, i.e. when tile 3 contains units of at most one side at the end of step 12. |

**Invariant RC-1:** a REACH unit can be protected at most once per engagement, never once per turn.
**Invariant RC-2:** if every friendly tile-3 unit dies in the same clash, the REACH unit was protected for that clash (damage is simultaneous), and the engagement ends at step 12, resetting the guard.

### 9.3 Rounding, minimum and maximum

| Property | Value | Label |
|---|---|---|
| Rounding target | **Δ Power**, not damage. Rounded half away from zero at C7. | `LOCKED-V1` [Lock 7] |
| Damage values | Integers, read from `DAMAGE_TABLE`. Never computed at runtime. | `LOCKED-V1` [Lock 7] |
| Minimum damage | **9** (Δ = −40) | `DERIVED` |
| Maximum damage | **68** (Δ = +40) | `DERIVED` |
| Damage floor override | none — there is no "minimum 1 damage" rule in MR or the Lock | `DERIVED` |
| Table size | 81 entries, Δ ∈ [−40, +40] | `DERIVED` from the clamp |

Non-integer Δ arises from the ×1.40 counter bonus and the 0.75 / 0.50 stack multipliers. Rounding **Δ** rather than damage is what removes float from the authoritative path entirely: the table is generated once, offline, and versioned with the rules. Reference entries:

| Δ | −40 | −25 | −15 | −8 | −7 | −3 | 0 | +3 | +6 | +7 | +8 | +12 | +15 | +17 | +25 | +40 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| damage | 9 | 13 | 17 | 20 | 21 | 23 | 25 | 27 | 29 | 30 | 31 | 34 | 36 | 38 | 47 | 68 |

The full 81-entry table is a content artefact shipped with `rulesVersion` and hashed into `contentPoolHash`.

### 9.4 Test vectors

`UNIT_MAX_HP = 100` [Lock 5]. Δ is rounded **before** the table lookup [Lock 7]; damage values are table reads, not computations.

| # | Scenario | A power | B power | Δ (clamped) | A deals | B deals | Notes |
|---|---|---|---|---|---|---|---|
| TV-01 | Equal power | 10.00 | 10.00 | 0 | 25.000 → **25** | 25.000 → **25** | Mutual attrition; 4 clashes to kill |
| TV-02 | Δ +7 (one Age) | 17.00 | 10.00 | +7 | 29.781 → **30** | 20.986 → **21** | Exchange 1.42:1 — matches MR §2.3 |
| TV-03 | Δ +8 (commander budget) | 18.00 | 10.00 | +8 | 30.535 → **31** | 20.468 → **20** | Exchange 1.49:1 — matches MR §4.5 |
| TV-04 | Δ +15 | 25.00 | 10.00 | +15 | 36.375 → **36** | 17.182 → **17** | Exchange 2.12:1 |
| TV-05 | Δ +25 | 35.00 | 10.00 | +25 | 46.706 → **47** | 13.382 → **13** | Exchange 3.49:1 |
| TV-06 | Δ +40 (at clamp) | 50.00 | 10.00 | +40 | 67.957 → **68** | 9.197 → **9** | Exchange 7.39:1 |
| TV-07 | Δ +60 (beyond clamp) | 70.00 | 10.00 | **+40** | 67.957 → **68** | 9.197 → **9** | Identical to TV-06 — clamp proof |
| TV-08 | Δ −40 (at clamp, reversed) | 10.00 | 50.00 | −40 | 9.197 → **9** | 67.957 → **68** | Symmetry proof |
| TV-09 | Counter advantage | SWORD 14 → ×1.40 = 19.60 | SPEAR 14.00 | +5.6 → **+6** | **29** | **22** | Counter applied pre-curve to the dominant class [Lock 16] |
| TV-10 | Highland, previous holder is B | attacker 14.00 | defender 14 + 3 = 17.00 | −3.0 → **−3** | **23** | **27** | +3 goes to `tile3HolderPrevTurn` = B [Lock 15] |
| TV-10b | Highland, tile 3 was disputed last turn | 14.00 | 14.00 | 0.0 → **0** | **25** | **25** | `tile3HolderPrevTurn` null → **neither** side gets +3 [Lock 15] |
| TV-11 | Multi-unit stacking | [17,14,12] → 33.50 | [17] → 17.00 | +16.5 → **+17** | **38** | **16** | Stack mults 1.00/0.75/0.50 |
| TV-12 | Full 3-stack mirror | [10,10,10] → 22.50 | [10,10,10] → 22.50 | 0.0 → **0** | **25** | **25** | Stacking is symmetric |
| TV-13 | REACH first clash | [SWORD 14 @t3, SPEAR 12 REACH @t2] → 14 + 9 = 23.00 | [SWORD 14] → 14.00 | +9.0 → **+9** | **31** | **20** | B's 20 damage applies **only** to the tile-3 SWORD; the REACH SPEAR is exempt this clash and its guard is consumed [Lock 17 R2, R4] |
| TV-13b | REACH second clash, same engagement | same 23.00 | same 14.00 | **+9** | **31** | **20** | Guard already consumed — the REACH SPEAR is now a valid damage target under M24 [Lock 17 R4] |
| TV-13c | REACH with no friendly frontline | [SPEAR 12 REACH @t2] alone | [SWORD 14] @t3 | — | — | — | REACH contributes **nothing** and is **not** protected; there is no tile-3 clash to join [Lock 17 R3] |
| TV-14 | Counter + Highland stack | SPEAR [12,12] → 21.00 ×1.40 = 29.40 | HORSE [14] +3 Highland = 17.00 | +12.4 → **+12** | **34** | **19** | Resolution order: stack → dominant class → counter → highland → clamp → round → table |

**Vectors TV-01 through TV-08 test MR-defined behaviour only.** TV-09 through TV-14 are fully determined. For TV-11, `[17,14,12]` is also the allocation order when the units share a tile; overflow proceeds 17 → 14 → 12. In TV-13b the front-line tile-3 unit remains first, then the now-eligible tile-2 REACH unit.

### 9.5 Capital interaction

MR §1.4 states results are decided by "score differential at turn 24, **not board wipe** — prevents a losing run from ending early and killing session length."

| Rule | Statement | Label |
|---|---|---|
| C14 | A run **never** terminates before turn 24 for any reason. | `LOCKED` §1.4 |
| C15 | Reaching the enemy capital tile does not end the run. | `DERIVED` from C14 |
| C16 | Capital contact does **nothing**. Capitals have no HP, cannot be attacked, and grant no score. They are presentation-only. | `LOCKED-V1` [Lock 14] |
| C17 | A unit that reaches the far end tile simply stops there and remains a normal unit. | `LOCKED-V1` [Lock 11], [Lock 14] |

MR §7.4.1 shows the capital tile displaying a number (38 in the Age IV reference layout) which MR §7.4.2 shows as equal in kind to the match score (41–38). Under [Lock 14] that number is confirmed as the **score readout**: the capital tile is where the UI renders `PlayerState.score`, and nothing more.

---

## 10. Deterministic seed and snapshot contract

### 10.1 What the seed must control

| Controlled by seed | Rule |
|---|---|
| Card offer sequence (all 24 × 3 offers) | `LOCKED` §1.4 |
| Lane modifier assignment (the shuffle of one RIVER, one HIGHLAND, one COAST) | `LOCKED-V1` [Lock 10] |
| Deterministic tie-breaking, where any survives the explicit tie-break chains | `LOCKED-V1` [Lock 19] |

| **Not** controlled by the seed |
|---|
| Player choices — these are the input being tested |
| Snapshot choices — supplied by `OpponentSnapshot.selections` |
| Presentation randomness (VFX variance, idle animation) — must use a separate, unseeded stream |

### 10.2 Versioned PRNG and indexed stream derivation — `LOCKED-V1` [Lock 19, Final Lock 5]

```
masterSeedInt   = decodeBase32(masterSeed with dashes stripped)
streamCardOffer = splitmix64(masterSeedInt ^ 0x01)
streamLaneMod   = splitmix64(masterSeedInt ^ 0x02)
streamTieBreak  = splitmix64(masterSeedInt ^ 0x03)
```

`rulesVersion = 1.2.0-v1-final` pins **PCG32 XSH-RR 64/32** (LCG multiplier `6364136223846793005`, XSH-RR output) as the sole authoritative generator and the Stafford variant-13 **SplitMix64** finalizer/step (gamma `0x9E3779B97F4A7C15`, multipliers `0xBF58476D1CE4E5B9` and `0x94D049BB133111EB`) solely for seed derivation. Every 32- and 64-bit operation uses unsigned arithmetic modulo 2^32 or 2^64 respectively; right shifts are logical.

Seed text is canonical Crockford Base32 using `0123456789ABCDEFGHJKMNPQRSTVWXYZ`, case-insensitive on input, with optional dashes ignored and `O→0`, `I/L→1`. After normalization it contains 1–13 digits and must decode to an unsigned 64-bit value; overflow is invalid. The leftmost digit is most significant. Where bytes are serialized or hashed, unsigned integers use big-endian/network byte order. Display encoding emits uppercase without aliases.

PCG32 is initialized with the reference two-step sequence: `state=0`; `inc=(initseq<<1)|1`; call `next`; `state += initstate (mod 2^64)`; call `next`. `next` saves `oldstate`, advances `state = oldstate*6364136223846793005 + inc (mod 2^64)`, computes `xorshifted = uint32(((oldstate>>18)^oldstate)>>27)` and `rot = oldstate>>59`, then returns `rotr32(xorshifted, rot)`.

For addressable card offers, derive `addressSeed = splitmix64(streamCardOffer XOR (uint64(turn)<<32) XOR (uint64(offerIndex)<<24) XOR (uint64(redrawAttempt)*0xD1342543DE82EF95))`, then initialize PCG32 with `initstate=addressSeed`, `initseq=0x43415244` (`CARD`). `turn` is 1..24, `offerIndex` is 0..2, and `redrawAttempt` starts at 0. Each address first draws card type, then the card index. Collision retries increment only that slot's `redrawAttempt`. Lane-modifier and tie-break addresses use the same pattern with their dedicated stream seed and documented domain sequence values `0x4C414E45` (`LANE`) and `0x54494542` (`TIEB`).

For an unbiased integer in `[0,bound)`, where `1 <= bound <= 2^32`, compute `threshold = uint32(-bound) mod bound`; draw PCG32 values until `r >= threshold`, then return `r mod bound`. No modulo-only bounded selection is permitted. Language-native and engine-native random functions are forbidden in authoritative simulation.

Golden vectors (decimal PCG outputs; hexadecimal 64-bit SplitMix outputs):

| Vector | Input | Expected output |
|---|---|---|
| SM-00 | `splitmix64(0)` | `E220A8397B1DCDAF` |
| SM-01 | `splitmix64(1)` | `910A2DEC89025CC1` |
| SM-02 | `splitmix64(0123456789ABCDEF)` | `157A3807A48FAA9D` |
| PCG-00 | `initstate=42`, `initseq=54`, first six | `2707161783, 2068313097, 3122475824, 2211639955, 3215226955, 3421331566` |
| STR-00 | master `0` → card/lane/tie | `910A2DEC89025CC1 / 975835DE1C9756CE / 1D0B14E4DB018FED` |
| STR-01 | master `0123456789ABCDEF` → card/lane/tie | `E821EEBBC0778421 / 629DD08FAE280E80 / 9B4C210F98EC07FA` |
| OFF-19 | master `1`, turn 19, slots 0/1/2, redraw 0: address seeds | `933FE6C326E506C1 / 468BE5E4E85FCD29 / F3385872DB25850C` |
| OFF-19-OUT | PCG at those addresses, first output | `4207010209 / 4199241477 / 3060309534` |

These vectors are normative and must pass unchanged on every supported platform.

### 10.3 Determinism across environments

| Threat | Mitigation | Label |
|---|---|---|
| Different devices | Damage from the precomputed integer table (§9.3); native `exp()` forbidden. Floats appear only in intermediate effective-Power arithmetic, which is collapsed to an integer Δ before any table read. | `LOCKED-V1` [Lock 7] |
| Replays | `ReplayEvent.stateHash` after every event; replay asserts equality (RE-1). | `PROVISIONAL` |
| Client restarts | Full `RunState` serializable; resume replays from the last `stateHash`. | `PROVISIONAL` |
| Future balance patches | `rulesVersion` + `contentVersion` are stored in `MatchResult` and `OpponentSnapshot`. **A snapshot whose versions differ is not selectable for a run.** Old snapshots are archived, never migrated. | `LOCKED-V1` [Lock 18] |
| Content pool drift | `contentPoolHash` recorded in `RunState`; mismatch is a hard failure. | `PROVISIONAL` |

### 10.4 Reproducibility contract

```
MatchResult = f(rulesVersion, contentVersion, seed, playerSelections[24], snapshotId)
```

This function must be **pure and total**. Given identical inputs it returns an identical `MatchResult`, including `replayHash`, on any device, at any time, in any process.

**Residual float risk.** Effective Power is computed with the 0.75 / 0.50 stack multipliers and the ×1.40 counter, so intermediate values are non-integral. Two mitigations are available and one should be chosen during implementation: (a) compute effective Power in fixed-point hundredths as integers throughout, or (b) accept doubles for the intermediate and rely on the fact that all inputs are small integers scaled by 0.75, 0.5 and 1.4 — products that are exactly representable in IEEE-754 for the value ranges in play (max ~50). Option (a) is safer and is recommended; this is an implementation detail, not a rules question, and does not appear in the OPEN-V1 register.

### 10.5 Minimum snapshot contents

Per §5.14: `snapshotId`, `rulesVersion`, `contentVersion`, `seed`, `selections[24]`, `recordedScore`. Everything else is presentation.

A snapshot does **not** store board state, unit positions, or resources — all of that is recomputed by replaying `selections` against `seed`. This is what makes MR §6.3's shareable seed code work.

---

## 11. Scoring and match completion

### 11.1 What MR establishes

| Rule | Statement | Label | Source |
|---|---|---|---|
| SC1 | The result is the **score differential at turn 24**, not board wipe. | `LOCKED` | §1.4 |
| SC2 | A run always runs to turn 24. | `LOCKED` | §1.4 |
| SC3 | Score is a running total visible during play (MR §7.4.1 shows a score on the capital tile and on the opponent header at turn 21). | `DERIVED` | §7.4.1 |
| SC4 | Reference magnitudes: 41 vs 38 at turn 24; ~38 at turn 21. | `DERIVED` from §7.4.2, §7.4.1 | — |

### 11.2 The V1 scoring rule — `LOCKED-V1` [Lock 4]

MR contains no scoring formula. Lock 1 supplies one.

```
SC-V1:  score += 1 × count(lanes where lane.ownedBy == side)     at step 13 of §4.1
```

| Property | Value |
|---|---|
| Points per exclusively held contested tile | **1** |
| Evaluation | After movement **and** combat resolve (step 13) |
| Range per side per turn | 0–3 |
| Disputed tile | 0 points to either side |
| Unoccupied tile | 0 points to either side |
| Accumulation | Cumulative across all 24 turns; monotonically non-decreasing |
| Theoretical maximum | **72** (24 × 3) |
| Victory | Higher score at turn 24 |
| Tie | Equal scores → `TIE`, no tiebreaker |

This **replaces** the 2-points-per-tile figure recommended in the pre-Lock draft. Every worked example and golden test in this document has been recomputed at 1 point.

**Observation for economy simulation, not a rule.** MR §7.4.2's illustrative 41–38 implies, at 1 point per tile, that the winner exclusively held ~1.71 of 3 contested tiles on every one of 24 turns — near-permanent board control. Either MR's numbers are illustrative only, or contested-tile control is expected to be highly persistent for the leading side. The 500-run simulation should report the actual distribution of end-of-run scores so the UI, the ladder and MR's own example can be reconciled.

### 11.4 Match completion

| Rule | Statement | Label |
|---|---|---|
| MC1 | At the end of turn 24, compute `scoreDifferential = playerScore − snapshotScore`. | `LOCKED` §1.4 |
| MC2 | `> 0` → `VICTORY`; `< 0` → `DEFEAT`. | `DERIVED` |
| MC3 | `== 0` → **`TIE`**. Ties are a distinct outcome. There is no tiebreaker. | `LOCKED-V1` [Lock 4] |
| MC4 | Board state at turn 24 does not affect the result beyond its contribution to score. | `LOCKED` §1.4 ("not board wipe") |
| MC5 | `MatchResult` payload per §5.17. | `PROVISIONAL` |
| MC6 | `playerScore` and `snapshotScore` are each bounded to 0..72. | `DERIVED` from [Lock 4] |

---

## 12. Edge-case matrix

| # | Scenario | Expected behaviour | Source | Status | Test |
|---|---|---|---|---|---|
| EC-01 | No offered card is legal for a side | PASS recorded; no card effect; offers **not** rerolled; income, movement, combat, scoring proceed | [Lock 12] | `LOCKED-V1` | GT-08, GT-22 |
| EC-02 | All three lanes hold 3 units of that side; TRAIN offered | TRAIN unselectable in every lane; PASS if nothing else is legal | MR §2.3 + [Lock 12] | `LOCKED-V1` | GT-09 |
| EC-03 | BUILD offered with a poor payback ratio | **Legal.** Payback is not a runtime gate in V1 | §6.7 | `V1-EXCEPTION` | GT-11 |
| EC-04 | BUILD on turn 20 (Age IV, cost 22, +4/turn, 4 turns left) | Legal. Returns 16 for 22 spent — a net loss of 6 Growth, permitted by design | §6.7 | `V1-EXCEPTION` | GT-11b |
| EC-05 | BUILD offered on turn 21+ | Never generated — 0% offer weight on turns 21–24 | §6.7 | `V1-EXCEPTION` | GT-11c |
| EC-06 | Keystone offered after one is already taken | Still offered; unselectable for that side; **selectable by the opposing side** | [Lock 12], [Lock 13] | `LOCKED-V1` | GT-12 |
| EC-07 | Keystone offered in Age I–II | Never generated; `minAge = 3` | MR §2.2 | `LOCKED` | GT-12 |
| EC-08 | Duplicate perk offered to a side that holds it | Unselectable for that side; remains in the shared offer | [Lock 13] | `LOCKED-V1` | GT-23 |
| EC-09 | Both sides advance into the same empty tile the same turn | Both enter (blocking uses pre-step occupancy); tile becomes a combat site; both take damage; tile is **disputed** → 0 points to either | [Lock 11], [Lock 4] | `LOCKED-V1` | GT-05 |
| EC-10 | Both stacks reduced to 0 HP the same turn | Damage is simultaneous, so both die; tile unoccupied at step 12; 0 points to either; engagement ends and REACH guards reset | [Lock 6], [Lock 4], [Lock 17] | `LOCKED-V1` | GT-06 |
| EC-11 | Unit reaches the enemy end tile | Stops there. Nothing happens. Run continues to turn 24 | [Lock 11], [Lock 14] | `LOCKED-V1` | GT-13 |
| EC-12 | COAST lane; unit blocked after step 1 of 2 | Stops; forfeits step 2. No combat occurs between the steps | [Lock 11] | `LOCKED-V1` | GT-04 |
| EC-13 | Mixed-class stack faces a mixed-class stack | Dominant class = most post-stacking, pre-counter effective Power; ties by strongest unit → earliest trained turn → instance ID; counter applied **once** between the two dominant classes | [Lock 16] | `LOCKED-V1` | GT-18b |
| EC-14 | HIGHLAND lane; tile 3 was disputed or empty last turn | **Neither** side receives +3 | [Lock 15] | `LOCKED-V1` | TV-10b, GT-20 |
| EC-15 | HIGHLAND bonus outside tile 3 | Never applies. The bonus is scoped to that lane's tile 3 only | [Lock 15] | `LOCKED-V1` | GT-20b |
| EC-16 | REACH unit on tile 2 with no living friendly unit on tile 3 | Contributes **no** Power and receives **no** protection | [Lock 17 R3] | `LOCKED-V1` | TV-13c |
| EC-17 | REACH unit in its second clash of the same engagement | Guard already consumed; it is a valid damage target | [Lock 17 R4] | `LOCKED-V1` | TV-13b, GT-21 |
| EC-18 | Tile-3 engagement ends, then a new one begins in the same lane | `engagementId` increments; all REACH guards in that lane reset | [Lock 17 R5] | `LOCKED-V1` | GT-21 |
| EC-19 | Δ Power exceeds ±40 | Clamped, then rounded; identical result to ±40 | MR §2.3, [Lock 7] | `LOCKED` | GT-07, TV-07 |
| EC-20 | Non-integer Δ from the ×1.40 counter or stack multipliers | Round Δ half away from zero, **then** read the damage table | [Lock 7] | `LOCKED-V1` | TV-09, TV-11, TV-14 |
| EC-21 | Stack order and damage allocation | Descending effective pre-stacking Power; earliest trained; stable instance ID. Damage is front-to-back with overflow; REACH-protected units are omitted for that clash. | [Final Lock 1] | `LOCKED-V1` | GT-17, GT-21, GT-25 |
| EC-22 | Resource exceeds 999 | State remains unchanged except for the earned amount; emit diagnostic warning. Test build asserts only above 1,000,000. No gameplay cap. | [Final Lock 6] | `LOCKED-V1` | GT-28 |
| EC-23 | Snapshot version differs from the run | Snapshot is **not selectable**; the run is never created | [Lock 18] | `LOCKED-V1` | GT-14 |
| EC-24 | Snapshot selection is illegal under matching versions | **Determinism error.** Test builds fail loudly; production records a PASS and emits an error event | [Lock 18] | `LOCKED-V1` | GT-14b |
| EC-25 | Turn 24 ends with equal scores | `TIE`. No tiebreaker | [Lock 4] | `LOCKED-V1` | GT-19b |
| EC-26 | Structure placed in a RIVER lane | +1 Growth on top of the tier yield; lowers `paybackTurns` (telemetry only) | MR §1.3, §6.7 | `LOCKED` + `V1-EXCEPTION` | GT-11 |
| EC-27 | Age boundary | Age advances at step 15 of turn 6/12/18, effective the following turn. Turn 6 resolves entirely under Age I | [Lock 1] | `LOCKED-V1` | GT-10 |
| EC-28 | ADVANCE taken — does it accelerate the Age? | **No.** ADVANCE grants a perk only | [Lock 2] | `LOCKED-V1` | GT-10b |
| EC-29 | Two structures in the same lane | Permitted. No cap. Structures are lane-scoped and occupy no tile | [Lock 9] | `LOCKED-V1` | GT-02b |
| EC-30 | Unit trained on turn 24 | Enters at the entry tile, moves once, may fight, then the run ends. In a plain lane it cannot reach tile 3 | [Lock 8] | `LOCKED-V1` | GT-15b |
| EC-31 | Turn 1 contested income and HIGHLAND | `tile3HolderPrevTurn` is null for all lanes on turn 1 → no contested income, no HIGHLAND bonus | §4.3 | `DERIVED` | GT-01 |
| EC-32 | Ordering of multiple ActiveEffects in one trigger | ADD partition then MULTIPLY; within each, priority → source type → source ID; then clamp and round | [Final Lock 4] | `LOCKED-V1` | GT-27 |
| EC-33 | Composition of the three offers | Three independent weighted type draws; duplicate types allowed, duplicate IDs redrawn per indexed slot | [Final Lock 2] | `LOCKED-V1` | GT-16, GT-26 |
| EC-34 | BUILD tier | Tier and base yield equal the resolving Age; no lower-tier purchase; existing structures never upgrade | [Final Lock 3] | `LOCKED-V1` | GT-02, GT-11, GT-29 |

---

## 13. Golden test scenarios

All scenarios assume `rulesVersion = 1.2.0-v1-final`, `contentVersion = 1.0.0-v1`, starting **Growth 8 / Insight 2** [Lock 3], `UNIT_MAX_HP = 100` [Lock 5], and **1 point per exclusively held contested tile** [Lock 4].

| ID | Name | Setup | Choice | Expected |
|---|---|---|---|---|
| **GT-01** | Turn 1 baseline | Run start. No structures, no units. All `tile3HolderPrevTurn` null | ADVANCE (cost 2 Insight) | After step 7: G = 8 + 3 = **11**; I = 2 − 2 + 2 = **2**. Perks = 1. Score **0**. No contested income (EC-31). |
| **GT-02** | Structure yields on its placement turn | Turn 1, plain lane | BUILD tier-1 Growth into Lane A | Pay 8 → G = 0. Income = 3 base + 1 structure = 4 → **G = 4**, **I = 4**. Proves Q1. |
| **GT-02b** | Second structure, same lane | Turn 3, G = 8 | BUILD into Lane A again | Legal. Lane A now holds 2 structures. No cap, no tile occupancy (EC-29). |
| **GT-03** | Train, move, no contact | Turn 5, Lane C empty | TRAIN | Unit enters tile **1** at **100 HP**, ends the turn on tile **2**. No combat. |
| **GT-04** | COAST double step and blocking | Lane B modifier = COAST. Player unit on tile 1 | — | Empty lane: ends on tile **3**. With an enemy on tile 2 at the start of step 1: unit is blocked and ends on tile **1** (forfeits step 2). No combat between steps. |
| **GT-05** | Simultaneous entry into an empty tile 3 | Player unit tile 2, Snapshot unit tile 4, both Power 10, plain lane | — | Both enter tile 3 (pre-step occupancy). Δ = 0 → both deal **25**. Both at **75 HP**. Tile disputed → **0 points** each. |
| **GT-06** | Mutual annihilation | Both units at 20 HP, Power 10, both on tile 3 | — | Both deal 25 ≥ 20 → both destroyed simultaneously. Tile unoccupied at step 12 → **0 points** each. `engagementId` increments. |
| **GT-07** | Clamp proof | Player effective 70.0 vs Snapshot 10.0 | — | Δ = +60 → clamped **+40** → Player deals **68**, Snapshot deals **9**. Byte-identical to a true Δ = +40 setup. |
| **GT-08** | PASS on no legal card | Turn 9 (Age II: build 11 / unit 8 / perk 3). G = 2, I = 1 | — | No legal offer → `Selection { offerIndex: −1 }`. No cost, no effect. Income, movement, combat, scoring all still run. |
| **GT-09** | All lanes full | 3 units in each of A, B, C. TRAIN offered | — | TRAIN unselectable everywhere. If the other two offers are illegal, GT-08 applies. |
| **GT-10** | Age transition boundary | Turn 6, Age I | TRAIN | Unit `basePower = 10`, cost **6**. At step 15, `age → 2`. Turn 7 TRAIN costs **8** and gives Power **12**. |
| **GT-10b** | ADVANCE does not accelerate the Age | Turn 3, Age I; take ADVANCE on turns 3, 4, 5 | ADVANCE ×3 | `age` is still **1** on turn 6 and becomes 2 only at step 15 of turn 6. |
| **GT-11** | RIVER lowers payback, does not gate | Turn 14, Age III (cost 16, tier yield +3) | BUILD into a plain lane, then a fresh run BUILD into the RIVER lane | Both are **legal** (§6.7). Plain: `paybackTurns = 5.33`, yield +3. RIVER: `paybackTurns = 4.0`, yield **+4**. Neither is rejected. |
| **GT-11b** | Dominated late BUILD is legal | Turn 20, Age IV, G ≥ 22 | BUILD | Legal. Spends 22, returns 4 × 4 = **16** over the remaining turns. Net **−6 Growth**. The engine must not reject it. |
| **GT-11c** | BUILD closes at turn 21 | Turns 21–24 | — | No offer of any turn 21–24 hand has `cardType == BUILD`, across 1,000 seeds. |
| **GT-12** | Keystone gating | Turn 12 (Age II), turn 15 (Age III, take it), turn 17 | — | Turn 12: no Keystone generated. Turn 15: taken → `keystoneTaken = true`. Turn 17: Keystone appears in the shared offer, unselectable for that side, **selectable by the opponent**. |
| **GT-13** | Capital contact is inert | Player unit reaches tile 5 with the lane clear | — | Unit remains on tile **5**. No score change, no damage, no run termination. |
| **GT-14** | Version guard | Snapshot `rulesVersion = 1.1.0-lock1`; run is `1.2.0-v1-final` | — | Snapshot is **not selectable**. No run is created. |
| **GT-14b** | Determinism error | Versions match; snapshot's turn-11 selection is illegal in the run | — | Test build: **fail loudly**, reporting turn 11. Production: record PASS for turn 11 and emit an error event. |
| **GT-15** | Full-run determinism | Fixed seed, fixed `playerSelections[24]`, fixed snapshot | — | Two independent executions on two different platforms produce identical `replayHash`, `playerScore`, `snapshotScore`, and every `ReplayEvent.stateHash`. **This is the V1 acceptance test.** |
| **GT-15b** | Turn-24 training | TRAIN on turn 24 into a plain lane | TRAIN | Unit enters tile 1, moves to tile 2, does not reach tile 3, contributes no score. Run ends normally. |
| **GT-16** | Same-seed offer identity | Same seed, two different player selection lists | — | All 72 offered `cardId` values are identical across both runs and identical between PLAYER and SNAPSHOT. Proves CO-1 / MR §1.4. |
| **GT-17** | Stack multiplier | Lane holds Power [17, 14, 12] | — | Effective power = 17 + 10.5 + 6 = **33.50**. |
| **GT-18** | Counter applies before the curve | SWORD Power 14 vs SPEAR Power 14, plain lane, neither side held tile 3 last turn | — | Attacker 19.60, Δ = +5.6 → **+6** → **29** vs **22**. |
| **GT-18b** | Dominant class in a mixed stack | Side A: SWORD 17 + SPEAR 14 + SPEAR 12. Side B: SPEAR 17 | — | A's post-stacking pre-counter contributions: SWORD 17.0, SPEAR 10.5 + 6.0 = 16.5 → dominant class **SWORD**. SWORD beats SPEAR → A ×1.40. |
| **GT-19** | Score accrues at 1 point per tile | Side exclusively holds tile 3 in Lanes A and C; Lane B disputed | — | `score += 2` at step 13. Lane B awards nothing to either side. |
| **GT-19b** | Tie | Both sides finish turn 24 on 31 | — | `outcome = TIE`, `scoreDifferential = 0`. No tiebreaker is applied. |
| **GT-20** | HIGHLAND uses the previous turn's holder | Lane B = HIGHLAND. At the end of turn 9 the Snapshot exclusively held tile 3. Turn 10 combat on tile 3, both sides Power 14 | — | Snapshot gets **+3** → 17 vs 14 → Δ = −3 for the Player → Player deals **23**, Snapshot deals **27**. |
| **GT-20b** | HIGHLAND is tile-3-only | Same lane, combat on tile 2 | — | **No** +3 applied. |
| **GT-20c** | HIGHLAND after a disputed turn | Lane B = HIGHLAND; tile 3 was disputed at the end of turn 9 | — | `tile3HolderPrevTurn` is null → **neither** side gets +3 → Δ = 0 → both deal **25**. |
| **GT-21** | REACH guard consumption and reset | Lane A: Player SWORD 14 on tile 3 at 10 HP, Player SPEAR 12 with REACH on tile 2; Snapshot SWORD 14 on tile 3. Engagement runs three turns | — | First clash: incoming 20 destroys only the 10-HP frontline; the protected REACH unit is omitted and 10 damage remains unallocated. Guard is consumed. In the next clash of the same engagement the REACH unit is eligible; overflow reaches it after any front-most eligible unit. Ending the engagement increments `engagementId` and resets protection. |
| **GT-22** | PASS does not disturb the shared offer | Player passes on turn 9; Snapshot selects offer 1 | — | Both sides saw the same three `cardId`s. No reroll, no replacement. Turn 10's offers are unaffected by the PASS. |
| **GT-23** | Perk uniqueness is per side | Player holds perk `P`. Turn 14 offers `P` again | — | `P` is unselectable for the Player, and **selectable** by the Snapshot if the Snapshot does not hold it. |
| **GT-24** | Lane modifier set | 1,000 runs, 1,000 distinct seeds | — | Every run's modifier multiset is exactly `{RIVER, HIGHLAND, COAST}`. Across seeds all 6 permutations occur. Both sides see the same layout. |
| **GT-25** | Damage overflow | Three same-tile Player units ordered by M22 have HP `[10, 15, 100]`; incoming damage 40 | — | First two units are destroyed and the third ends at **85 HP**. Total applied damage is 40. |
| **GT-26** | Independent offers and collision redraw | Indexed draws produce TRAIN `T_A`, TRAIN `T_A`, TRAIN `T_B`; slot 1 redraw produces TRAIN `T_C` | — | Hand is `[T_A,T_C,T_B]`. Duplicate type is legal; duplicate ID is not. PLAYER and SNAPSHOT receive the same hand and slot 2 is unaffected. |
| **GT-27** | ActiveEffect total order | Applicable effects include ADD priorities 100 and 90 and MULTIPLY priority 50 from mixed source types | — | Both ADDs resolve before the MULTIPLY despite its lower numeric priority; within ADD, priority then source type then source ID. Clamp and round occur once at the end. |
| **GT-28** | Uncapped resources | Growth 998 receives +5 | — | Authoritative Growth becomes **1003** and warning emits. No clamp. The warning does not enter the state hash. |
| **GT-29** | Structure tier retention | Build in Age I; advance normally to Age IV | — | Structure remains tier 1 with base yield +1. A new BUILD in Age IV costs 22 and creates tier 4 with base yield +4. |
| **GT-30** | PRNG portability | Execute every §10.2 PRNG vector on two platforms | — | Every SplitMix64 value, PCG32 output, stream seed, and indexed offer address matches exactly. |

---

## 14. OPEN-V1 decision register

### 14.1 Resolved by V1 Rules Lock 1

Twenty-three of the original thirty items are closed. They are retained here for traceability; the rule text now lives in the body sections cited.

| ID | Original question | Resolution | Lock rule | Body §|
|---|---|---|---|---|
| 001 | Scoring formula | 1 point per exclusively held contested tile, after movement and combat; 0–3/turn; cumulative; max 72 | 4 | §2.9, §11.2 |
| 002 | Starting resources | Growth 8, Insight 2 | 3 | §2.2, §7.1 |
| 003 | Unit HP | 100 | 5 | §2.4 |
| 004 | Damage rounding | Round **Δ Power**, half away from zero, then read the table | 7 | §9.3 |
| 005 | Simultaneous damage | Yes — both sides compute from the same pre-damage state | 6 | §9.2 C9 |
| 007 | Age advancement | Fixed turn ranges; ADVANCE never accelerates | 1, 2 | §2.3, §6.4 A6 |
| 008 | Build viability contradiction | Payback is a content-balance target, not runtime legality | Build Exception | §6.7 |
| 009 | Build offer cutoff | BUILD offered turns 1–20; 0% on 21–24 | Build Exception | §2.6, §6.7 |
| 011 | No legal card | PASS; no effect; offers not rerolled | 12 | §6.6 |
| 012 | Lane modifier assignment | Exactly one of each, shuffled by `streamLaneMod`, shared by both sides | 10 | §2.5 |
| 013 | BUILD placement | Lane only; no tile; no cap; indestructible | 9 | §6.2 B5–B7 |
| 014 | TRAIN placement | Player tile 1, Snapshot tile 5; acts the same turn | 8 | §6.3 T4, T9 |
| 015 | Movement collision | Simultaneous, step-wise, blocking before each step, no pass/swap | 11 | §8.2 |
| 016 | Definition of "held" | Exclusive occupancy of tile 3 | 4 | §8.4 M30–M33 |
| 017 | Capital | Presentation-only; no HP; contact is inert | 14 | §8.1 M5, §9.5 |
| 018 | REACH "first clash" | Per engagement, requires a living friendly tile-3 unit, tracked and reset with `engagementId` | 17 | §9.2.1 |
| 019 | HIGHLAND defender | The side that exclusively held tile 3 at the end of the previous turn; null → nobody | 15 | §9.2 C4 |
| 020 | Mixed-class counters | Dominant class by post-stacking pre-counter Power; counter applied once | 16 | §9.2 C3–C4 |
| 021 | Duplicate perks | Unique per side per run; card stays in the shared offer | 13 | §6.4 A5 |
| 024 | Tie | Distinct `TIE` outcome, no tiebreaker | 4 | §11.4 MC3 |
| 025 | COAST second step | Blocking checked before each step; no combat between steps | 11 | §8.2 M12–M13 |
| 027 | Structure cap | None | 9 | §5.9 SI-2 |
| 029 | Snapshot illegal action | Version mismatch → not selectable; matched-version illegality → determinism error | 18 | §5.14 OS-2 |

### 14.2 Resolved by the final V1 rules lock

| ID | Resolution | Source |
|---|---|---|
| `OPEN-V1-006` | Total stack order plus front-to-back overflow allocation; REACH eligibility honored | Final Lock 1; §8.3, §9.2 |
| `OPEN-V1-010` | Non-negative uncapped integers; diagnostic warning >999 and test assertion >1,000,000 | Final Lock 6; §7.1 |
| `OPEN-V1-023` | Three independent weighted type draws; duplicate types allowed, duplicate IDs deterministically redrawn | Final Lock 2; §2.6, §6.0 |
| `OPEN-V1-026` | BUILD tier and base yield equal resolving Age; existing structures retain their tier/yield | Final Lock 3; §5.10, §6.2 |
| `OPEN-V1-028` | ADD then MULTIPLY total order using priority, source type, source ID; clamp and round last | Final Lock 4; §5.13 |
| `OPEN-V1-030` | PCG32 XSH-RR 64/32 with SplitMix64 seed derivation and indexed addresses | Final Lock 5; §10.2 |

### 14.3 Remaining open item

`OPEN-V1-022` is assigned to `_aiinfodocs/EPOCH_V1_Content_Manifest.md`. It is non-blocking for the rules engine and resolved for the playable prototype by that companion artifact; the Master Reference's 60-perk launch target remains a future content-production target.

---

## 15. Audit summary

### 15.1 Rule counts

Recalculated mechanically over §0–§13 by counting lines carrying each exact label token. A line carrying more than one distinct label counts once in each applicable row; citations and table labels are included, so this is a traceability-line audit rather than a count of unique semantic rules.

| Exact label token | Traceability lines |
|---|---:|
| `LOCKED` | 201 |
| `LOCKED-V1` | 134 |
| `V1-EXCEPTION` | 12 |
| `DERIVED` | 39 |
| `PROVISIONAL` | 13 |
| `OPEN-V1` | 9 |

The residual `OPEN-V1` traceability lines before §14 refer to resolved history or the delegated content deliverable; none marks a live blocking decision. Register audit: **30 IDs total — 29 rules decisions resolved in this specification; `OPEN-V1-022` assigned to and fulfilled by the V1 Content Manifest. Zero blocking rules decisions remain.** Historic mentions in the change log and resolution register are traceability, not unresolved references.

### 15.2 Remaining blocking decisions

**None.** No unresolved rule prevents deterministic simulation.

### 15.3 Remaining non-blocking decisions

No numbered gameplay-rule question remains. `OPEN-V1-022` points to the completed V1 Content Manifest. The following pre-existing implementation-policy statements remain `PROVISIONAL` and non-blocking because they sit outside authoritative match resolution: initialization API transitions (`S00`, `S01`); timeout/no-input handling and invalid-selection UI return (`S12`, `S13`); replay sealing and per-event `stateHash` emission (`S31`, turn step 16); the absence of a refund API; restart/replay persistence mechanics; the `contentPoolHash` drift failure policy; and the exact `MatchResult` payload. Choosing among those policies cannot change the result produced from the authoritative inputs in §10.4. Remaining balance assumptions in the manifest are prototype-tunable and likewise do not make simulation ambiguous.

### 15.4 Remaining contradictions

**None.** All three contradictions found in the pre-Lock audit are resolved:

| Contradiction | Resolution |
|---|---|
| Ages fixed vs. ADVANCE-driven (MR §1.2 vs §2.2) | Fixed turn ranges [Lock 1]; ADVANCE grants a perk only [Lock 2]. MR §1.2's "pushes Age progress" is reclassified as thematic language. |
| Build viability vs. Age cost tables (MR §2.1 vs §2.2/§1.6) | Payback becomes a content-balance target, not runtime legality; BUILD offered turns 1–20 (V1 Build Economy Exception, §6.7). |
| Build offer cutoff turn 19 vs turn 21 (MR §1.6 vs §2.1) | Superseded by the exception: BUILD closes at turn 21. |

The missing scoring formula — the fourth and most severe gap — is supplied by [Lock 4].

### 15.5 Is the core simulation now deterministic?

**Yes.** The core rules are deterministic and implementation-complete for the playable V1. Implementation and executable conformance tests have not been written in this task.

What is now guaranteed by the Lock:

- **No float in the authoritative damage path.** Δ is clamped, rounded half away from zero, and used to index a versioned 81-entry integer table. Native `exp()` is forbidden [Lock 7].
- **No hidden ordering ambiguity in the turn loop.** Card → income → movement → combat → ownership → score → persist → Age check, with income and HIGHLAND both reading the previous turn's persisted ownership [Lock 4, 6, 11, 15].
- **No RNG divergence by construction.** Versioned cross-platform PRNG, independent indexed streams, natives forbidden [Lock 19].
- **No snapshot drift.** Version mismatch prevents selection outright; matched-version illegality is a loud failure, not a silent substitution [Lock 18].

Final Lock 1 supplies unit and damage order; Final Lock 2 makes offers indexed and side-independent; Final Lock 4 supplies an effect total order; Final Lock 5 pins seed parsing, PCG32, SplitMix64, bounded selection and normative vectors. GT-15 and GT-30 are therefore implementable acceptance tests.

### 15.6 What is ready to build now

Everything below is fully specified and can be implemented immediately:

- The complete data model (§5)
- The 24-state run state machine and transition table (§3)
- The canonical turn order (§4.1), all nine ambiguity questions answered (§4.2)
- Age constants, scoring constants, combat constants (§2)
- The economy: starting balance, income, payment, PASS (§7, §6.6)
- Board, movement, blocking, COAST, ownership (§8)
- The damage table, counter, HIGHLAND, REACH (§9)
- Scoring and match completion (§11)
- All 30 golden test scenarios (§13)

---

## 16. V1 Content Manifest status

The rules boundary is complete and the companion prototype manifest is `_aiinfodocs/EPOCH_V1_Content_Manifest.md`, with machine-readable data at `_aiinfodocs/data/epoch_v1_content.json`. This resolves the deliverable assigned by `OPEN-V1-022` without changing the Master Reference's 60-perk launch target.

### 16.1 Ready

| Requirement | Status |
|---|---|
| Card types and their cost resources | `LOCKED` MR §1.2 |
| Cost per Age for BUILD / TRAIN / ADVANCE | `LOCKED` MR §2.2 |
| Unit Power per Age | `LOCKED` MR §2.2 |
| Structure yield per tier | `LOCKED` MR §2.1 |
| Unit classes and the counter triangle | `LOCKED` MR §2.3 |
| REACH semantics | `LOCKED-V1` [Lock 17] |
| Perk archetype counts (16/16/12/8/8 = 60) | `LOCKED` MR §2.2 |
| Keystone gating (Age III+, one per run, unique per side) | `LOCKED` + [Lock 13] |
| Deck weights per turn band, including the V1 Build window | §2.6 |
| `CardDefinition` / `PerkDefinition` / `ActiveEffect` schemas | §5.10, §5.12, §5.13 |
| The 81-entry damage table as a versioned content artefact | §9.3 |

### 16.2 Blocked

None.

### 16.3 Verdict

**Ready and authored.** The prototype pool supplies 4 BUILD definitions, 16 TRAIN definitions, 24 regular perks and 4 Keystones. Balance remains tunable; rules semantics do not.

---

## 17. Change log

### 17.1 Revision 1 — V1 Rules Lock 1 (8 September 2026)

`rulesVersion` `1.0.0-draft` → **`1.1.0-lock1`**.

Nineteen owner-approved rules plus one owner-approved exception applied throughout. Sections rewritten: §0.2 (label taxonomy — added `LOCKED-V1` and `V1-EXCEPTION`), §2.2, §2.4, §2.5, §2.6, §2.9 (new — scoring), §2.10 (new — determinism), §3.1, §3.2, §4.1, §4.2, §4.3, §5.3, §5.4, §5.6, §5.7, §5.8, §5.9, §5.13, §5.14, §5.17, §6.2, §6.3, §6.4, §6.5, §6.6, §6.7 (new — Build Economy Exception), §7.1, §7.2, §7.3, §8.1, §8.2, §8.3, §8.4, §9.1, §9.2, §9.2.1 (new — REACH), §9.3, §9.4, §9.5, §10.1, §10.2, §10.3, §11.2, §11.4, §12, §13, §14, §15, §16 (new), §17 (new).

| Lock | Rule | Closed |
|---|---|---|
| 1 | Ages fixed to turns 1–6 / 7–12 / 13–18 / 19–24 | 007 |
| 2 | ADVANCE grants a perk; does not accelerate the Age | 007 |
| 3 | Starting Growth 8, Insight 2 | 002 |
| 4 | 1 point per exclusively held contested tile, after movement and combat; 0–3/turn; cumulative; higher score wins; equal = TIE | 001, 016, 024 |
| 5 | Unit max HP 100 | 003 |
| 6 | Damage is simultaneous from a shared pre-damage state | 005 |
| 7 | Clamp, round Δ half away from zero, read a versioned table; native `exp()` forbidden | 004 |
| 8 | Player units enter tile 1, Snapshot tile 5; act the same turn | 014 |
| 9 | BUILD targets one lane; lane-scoped; no cap; indestructible | 013, 027 |
| 10 | Exactly one RIVER, HIGHLAND, COAST; shuffled by a dedicated stream; shared layout | 012 |
| 11 | Simultaneous step-wise movement; no pass/swap; COAST second step; combat after all steps | 015, 025 |
| 12 | PASS on no legal card; offers never rerolled | 011 |
| 13 | Perks unique per side per run | 021 |
| 14 | Capitals presentation-only | 017 |
| 15 | HIGHLAND +3 to the previous turn's exclusive tile-3 holder; tile 3 only | 019 |
| 16 | Dominant class by post-stacking pre-counter Power; counter applied once | 020 |
| 17 | REACH: tile 2, requires a living friendly tile-3 unit, first clash per engagement, tracked and reset | 018 |
| 18 | Snapshot version match required; matched-version illegality is a determinism error | 029 |
| 19 | Versioned cross-platform PRNG, independent indexed streams, natives forbidden | 030 (completed by Final Lock 5) |
| — | **Build Economy Exception** — payback is a balance target, not runtime legality; BUILD on turns 1–20, 0% on 21–24 | 008, 009 |

### 17.2 Corrections made while applying the Lock

| # | Correction |
|---|---|
| 1 | **Scoring recomputed at 1 point per tile.** The pre-Lock draft recommended 2. Every worked example (§7.2) and every golden test (§13) has been recalculated. Maximum score changed 144 → **72**. |
| 2 | **Worked examples rebuilt on the real opening balance.** The pre-Lock examples used a hypothetical 5/3 start; they now use 8/2 and show the actual turns 1–3 sequence, including the earliest reachable PASS. |
| 3 | **Damage derivation inverted.** The pre-Lock draft rounded *damage*; [Lock 7] rounds *Δ*. All fourteen test vectors were regenerated. The resulting integers are unchanged, but the derivation now removes float from the authoritative path. |
| 4 | **Ownership computed once, not twice.** The pre-Lock draft recomputed contested ownership at two points in the turn. [Lock 4] and [Lock 15] make one computation plus one persisted previous-turn value sufficient and unambiguous. |
| 5 | **Movement collision clarified.** Blocking is evaluated against **pre-step** occupancy, so two units advancing into the same empty tile both enter it. This is the case GT-05 tests, and it was ambiguous before. |
| 6 | **Three new state fields** added for locked behaviour: `LaneState.tile3HolderPrevTurn` [Lock 15], `LaneState.engagementId` and `UnitInstance.reachGuardUsedInEngagement` [Lock 17]. |
| 7 | **`TileState.structure` removed.** Structures are lane-scoped under [Lock 9] and no longer occupy tiles. |
| 8 | **Movement/stacking rules renumbered** M13–M17 → M20–M24 and M18–M20 → M30–M33, to avoid collision with the new locked movement rules M8–M13. |
| 9 | **Six new golden tests** added for locked behaviour with no prior coverage: GT-19/19b (scoring, tie), GT-20/20b/20c (HIGHLAND), GT-21 (REACH guard), GT-22 (PASS), GT-23 (perk uniqueness), GT-24 (lane modifier set). |

### 17.3 Revision 2 — Final V1 rules lock (8 September 2026)

`rulesVersion` `1.1.0-lock1` → **`1.2.0-v1-final`**. Owner decisions resolved `OPEN-V1-006`, `010`, `023`, `026`, `028`, and `030`; golden coverage expanded through GT-30. `OPEN-V1-022` remains the named content assignment and is fulfilled by the companion V1 Content Manifest. No blocking rule question remains.

---

*End of specification. Traceable to `_aiinfodocs/EPOCH-master-reference.md` (rules), V1 Rules Lock 1 (§17.1), the Final V1 Rules Lock (§17.3), and `_aiinfodocs/hooked-player-playbook.md` as supporting research. No application code has been written.*
