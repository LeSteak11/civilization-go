# EPOCH — Master Game Design Reference

**What this is:** the complete design specification for EPOCH, a mobile-native 4X strategy game derived from Civilization VI's shipped numerical data and compressed into a 3-minute asynchronous PvP run.

**Status:** Legacy v2 design reference, 8 September 2026. Consolidates the v1 design pass (core loop, systems) and the v2 pass (monetization, commerce, virality, UI). For current authority by subject, use the repository `README.md`; `EPOCH_PRODUCT_DIRECTION.md` supersedes this document for the outer product loop and board vocabulary.

**How to use this document (instructions for an AI assistant reading it):**
- This is a legacy source of design rationale. Apply it only where the subject-specific authoritative documents listed in the repository `README.md` are silent.
- Every section has a stable ID (`§0.1`, `§4.4`, etc.). Cite them when answering.
- `§CONST` below is a machine-readable block of every derived constant. Read it first.
- Numbers marked **DERIVED** come from parsing Civ VI's shipped XML. Numbers marked **CHOSEN** are design decisions with stated rationale. Numbers marked **BENCHMARK** are industry estimates, not measurements. Do not treat benchmarks as facts about EPOCH.
- Where a decision has an argued tradeoff, the tradeoff is written down. Do not silently reverse a decision without addressing its stated rationale.
- `§9` lists what is still open. If asked about something in `§9`, say it is undecided rather than inventing an answer.

---

## Table of contents

| ID | Section |
|---|---|
| §CONST | Machine-readable constants |
| §0 | Baseline — what the Civ VI data says |
| §0.1 | Extracted era curves |
| §0.2 | Building ROI is inverted |
| §0.3 | Combat is a two-parameter exponential |
| §0.4 | Class taxonomy is a hidden triangle |
| §0.5 | Constants carried forward |
| §1 | Core loop translation |
| §1.1 | The compression problem |
| §1.2 | The run: 24 turns, 4 Ages, one choice each |
| §1.3 | The board: three lanes, five tiles |
| §1.4 | Asynchronous PvP resolution |
| §1.5 | Session envelope and the turn clock |
| §1.6 | Beat sheet — what one run feels like |
| §2 | Systems deconstruction |
| §2.1 | Economy — six yields become two |
| §2.2 | Progression — 68 techs become 24 drafted perks |
| §2.3 | Combat — one-tap resolution |
| §2.4 | What is cut |
| §4 | Hard monetization data |
| §4.1 | Gem pricing ladder |
| §4.2 | Pity curve |
| §4.3 | Shard ladder and whale ceiling |
| §4.4 | Token economy (corrected) |
| §4.5 | Commander power budget |
| §4.6 | Revenue model |
| §4.7 | Entry offer ladder |
| §5 | Commerce and backend |
| §5.1 | Channel margin — the biggest financial decision |
| §5.2 | Commerce architecture |
| §5.3 | Store policy compliance |
| §6 | Social and virality |
| §6.1 | Why the replay screen |
| §6.2 | Export specification |
| §6.3 | The seed code growth loop |
| §6.4 | Supporting hooks |
| §6.5 | Share funnel and k-factor |
| §7 | Visual and UI framework |
| §7.1 | Thumb-zone budget |
| §7.2 | Touch targets and legibility |
| §7.3 | Age transition beat |
| §7.4 | Screen-by-screen layout spec |
| §7.5 | Card anatomy |
| §8 | Decision register — v1 to v2 changes |
| §9 | Open questions |
| §10 | Prototype order |
| §11 | Glossary |

---

## §CONST — Machine-readable constants

```json
{
  "source_data": {
    "game": "Civilization VI",
    "ruleset": "Base",
    "files_parsed": ["Units.xml", "Technologies.xml", "Buildings.xml", "Eras.xml"],
    "counts": { "techs": 68, "combat_units": 60, "buildings_total": 80, "wonders": 29, "buildings_non_wonder": 51, "eras": 8, "promotion_classes": 13 }
  },
  "derived_civ6_curves": {
    "tech_cost_ratio_per_era": 1.675,
    "unit_power_ratio_per_era": 1.186,
    "unit_cost_ratio_per_era": 1.398,
    "building_cost_ratio_per_era": 1.399,
    "power_to_cost_exponent": 0.5,
    "combat_damage_formula": "30 * exp(0.04 * delta_strength)",
    "building_payback_turns_range": [30, 130]
  },
  "epoch_run": {
    "turns": 24,
    "ages": 4,
    "turns_per_age": 6,
    "decisions_per_turn": 1,
    "cards_offered_per_turn": 3,
    "lanes": 3,
    "tiles_per_lane": 5,
    "target_run_seconds": 180,
    "median_turn_seconds": 7,
    "max_units_per_lane": 3,
    "stack_power_multipliers": [1.0, 0.75, 0.5],
    "yields": ["GROWTH", "INSIGHT"],
    "base_income_per_turn": { "growth": 3, "insight": 2 },
    "contested_tile_yield": { "growth": 1, "insight": 1 }
  },
  "epoch_age_table": [
    { "age": 1, "name": "Dawn",   "turns": [1, 6],   "civ6_eras": ["Ancient"], "perk_cost_insight": 2, "unit_power": 10, "unit_cost_growth": 6,  "build_cost_growth": 8 },
    { "age": 2, "name": "Bronze", "turns": [7, 12],  "civ6_eras": ["Classical", "Medieval"], "perk_cost_insight": 3, "unit_power": 12, "unit_cost_growth": 8,  "build_cost_growth": 11 },
    { "age": 3, "name": "Steel",  "turns": [13, 18], "civ6_eras": ["Renaissance", "Industrial"], "perk_cost_insight": 5, "unit_power": 14, "unit_cost_growth": 11, "build_cost_growth": 16 },
    { "age": 4, "name": "Modern", "turns": [19, 24], "civ6_eras": ["Modern", "Atomic", "Information"], "perk_cost_insight": 9, "unit_power": 17, "unit_cost_growth": 16, "build_cost_growth": 22 }
  ],
  "epoch_age_ratios": { "perk_cost": 1.65, "unit_power": 1.19, "unit_cost": 1.39, "build_cost": 1.40 },
  "epoch_combat": {
    "damage_formula": "25 * exp(0.025 * delta_power)",
    "delta_power_clamp": 40,
    "counter_triangle": { "SWORD": "beats SPEAR", "SPEAR": "beats HORSE", "HORSE": "beats SWORD" },
    "counter_bonus_percent": 40,
    "reach_keyword": "contributes power from tile 2 without return damage on first clash",
    "exchange_ratios": { "0": 1.0, "7": 1.42, "8": 1.49, "15": 2.12, "25": 3.49, "40": 7.39 }
  },
  "epoch_perk_pool": { "economic": 16, "military": 16, "tempo": 12, "conversion": 8, "keystone": 8, "total": 60, "keystone_min_age": 3, "keystone_max_per_run": 1 },
  "epoch_build_rule": "cost / yield_per_turn <= (24 - current_turn) / 2",
  "monetization": {
    "currency": "gems",
    "pull_cost_gems": 160,
    "ten_pull_cost_gems": 1440,
    "gem_packs_usd": [
      { "sku": "Pouch",    "price": 0.99,  "gems": 80,    "bonus_pct": 0,  "gems_per_usd": 80.8,  "usd_per_pull": 1.98 },
      { "sku": "Sack",     "price": 4.99,  "gems": 420,   "bonus_pct": 5,  "gems_per_usd": 84.2,  "usd_per_pull": 1.90 },
      { "sku": "Chest",    "price": 9.99,  "gems": 900,   "bonus_pct": 13, "gems_per_usd": 90.1,  "usd_per_pull": 1.78 },
      { "sku": "Vault",    "price": 19.99, "gems": 1900,  "bonus_pct": 19, "gems_per_usd": 95.0,  "usd_per_pull": 1.68 },
      { "sku": "Hoard",    "price": 49.99, "gems": 5000,  "bonus_pct": 25, "gems_per_usd": 100.0, "usd_per_pull": 1.60 },
      { "sku": "Treasury", "price": 99.99, "gems": 10800, "bonus_pct": 35, "gems_per_usd": 108.0, "usd_per_pull": 1.48 }
    ],
    "gacha": {
      "base_rates": { "legendary": 0.02, "epic": 0.08, "rare": 0.90 },
      "soft_pity_start_pull": 61,
      "soft_pity_ramp_per_pull": 0.049,
      "hard_pity_pull": 80,
      "expected_pulls_per_legendary": 36.61,
      "effective_legendary_rate": 0.02731,
      "published_rate_must_be": 0.0273,
      "cost_per_legendary_usd": [54.24, 72.49],
      "dupe_shard_value": 20
    },
    "shard_ladder": [
      { "step": "unlock", "shards": 60,  "cumulative": 60,  "est_pulls": 110, "est_usd": 163 },
      { "step": "2star",  "shards": 20,  "cumulative": 80,  "est_pulls": 146, "est_usd": 217 },
      { "step": "3star",  "shards": 40,  "cumulative": 120, "est_pulls": 220, "est_usd": 325 },
      { "step": "4star",  "shards": 80,  "cumulative": 200, "est_pulls": 366, "est_usd": 542 },
      { "step": "5star",  "shards": 120, "cumulative": 320, "est_pulls": 586, "est_usd": 868 },
      { "step": "6star",  "shards": 200, "cumulative": 520, "est_pulls": 952, "est_usd": 1410 }
    ],
    "token_economy": { "cap": 5, "refill_minutes": 25, "daily_grant": 3, "ranked_run_cost": 2, "runs_per_day_at_3_logins": 8, "refill_price_gems": { "single": 30, "full": 120 }, "unranked_runs": "unlimited and free" },
    "commander_power_budget_delta": 8,
    "commander_power_budget_exchange_ratio": 1.49,
    "commander_power_budget_hard_ceiling_delta": 10,
    "battle_pass": { "season_weeks": 8, "tiers": 50, "price_usd": 9.99, "plus_price_usd": 24.99, "plus_tier_skip": 15, "attach_rate_base": 0.09, "attach_rate_plus": 0.025 },
    "revenue_scenarios_per_100k_installs_180d": [
      { "case": "conservative", "conversion": 0.020, "revenue_usd": 115734,  "ltv_per_install": 1.16,  "arpdau": 0.082 },
      { "case": "base",         "conversion": 0.035, "revenue_usd": 526482,  "ltv_per_install": 5.26,  "arpdau": 0.371 },
      { "case": "upside",       "conversion": 0.048, "revenue_usd": 1249523, "ltv_per_install": 12.50, "arpdau": 0.880 }
    ],
    "revenue_mix_base": { "gacha": 0.56, "battle_pass": 0.23, "convenience": 0.14, "cosmetics": 0.07 },
    "planning_case": "conservative"
  },
  "commerce_channel_take_rates": {
    "ios_linkout_us_current": 0.00,
    "ios_linkout_apple_proposed": 0.15,
    "apple_iap_small_business": 0.15,
    "apple_iap_standard": 0.30,
    "google_play_iap": 0.30,
    "google_linkout_proposed": 0.20,
    "web_shop_psp_only": 0.05,
    "note": "iOS US link-out is 0% as of Sept 2026 pending district court; verify before pricing on it"
  },
  "ui": {
    "reference_device_pt": { "width": 390, "height": 844 },
    "reach_limit_pt": 506,
    "interactive_ceiling_pt": 560,
    "card_pt": { "width": 118, "height": 190 },
    "card_gutter_pt": 8,
    "lane_column_width_pt": 118,
    "unit_token_pt": 44,
    "min_body_text_pt": 15,
    "yield_numeral_pt": 28,
    "age_transition_total_seconds": 1.8,
    "export_aspect": "9:16",
    "export_resolution": [1080, 1920],
    "export_duration_seconds": [6, 12],
    "export_latency_budget_seconds": 3
  },
  "virality_targets": {
    "replay_view_rate": 1.0,
    "share_sheet_open_rate": [0.04, 0.07],
    "share_completion_rate": 0.55,
    "median_views_per_post": 300,
    "install_per_view": [0.004, 0.012],
    "k_factor_expected": 0.066,
    "k_factor_target": 0.15
  }
}
```

---

## §0 — Baseline: what the Civ VI data says

Four shipped XML files were parsed and reduced to growth curves. Civ VI is balanced by three geometric progressions running in parallel across eight eras. Those ratios are the foundation of everything below.

Source path on the authoring machine: `Sid Meier's Civilization VI/Base/Assets/Gameplay/Data/`.

### §0.1 — Extracted era curves — DERIVED

Medians per era. Unit rows use `MAX(Combat, RangedCombat, Bombard)`, bucketed by the era of the unit's prerequisite technology. Buildings exclude wonders.

| Era | Tech cost | Unit strength | Unit cost | Building cost | Units | Techs |
|---|---|---|---|---|---|---|
| Ancient | 50 | 28 | 65 | 80 | 7 | 11 |
| Classical | 160 | 35.5 | 110 | 120 | 8 | 8 |
| Medieval | 390 | 46.5 | 180 | 210 | 8 | 7 |
| Renaissance | 540 | 55 | 250 | 290 | 5 | 9 |
| Industrial | 907 | 67 | 360 | 390 | 9 | 8 |
| Modern | 1250 | 75 | 430 | 580 | 8 | 7 |
| Atomic | 1410 | 86 | 540 | 600 | 7 | 8 |
| Information | 1850 | 92.5 | 680 | — | 8 | 10 |
| **Geometric mean ratio / era** | **×1.675** | **×1.186** | **×1.398** | **×1.399** | 60 | 68 |

**The one number that matters.** Power grows at ×1.19 per era while cost grows at ×1.40 per era. Military strength scales at roughly the **square root of investment**. This is Civ VI's anti-snowball governor. Every monetization decision in §4 depends on preserving this relationship. The moment purchased power scales linearly with spend, the ladder collapses into a whale-only mode.

### §0.2 — Building ROI is inverted for our purposes — DERIVED

Payback period in turns = `Cost / YieldPerTurn`.

| Building | Era | Cost | Yield | Payback (turns) |
|---|---|---|---|---|
| Monument | — | 60 | +2 Culture | 30 |
| Shrine | Ancient | 70 | +2 Faith | 35 |
| Market | Classical | 120 | +3 Gold | 40 |
| Library | Ancient | 90 | +2 Science | 45 |
| Stock Exchange | Industrial | 390 | +7 Gold | 55.7 |
| Bank | Renaissance | 290 | +5 Gold | 58 |
| University | Medieval | 250 | +4 Science | 62.5 |
| Research Lab | Modern | 580 | +5 Science | 116 |
| Factory | Industrial | 390 | +3 Production | 130 |

**Finding.** Payback periods lengthen across eras — ~30–45 turns early, 116–130 late. Fine in a 500-turn campaign, where late buildings are bought for adjacency and unlocks rather than raw yield.

**Consequence for EPOCH.** In a 24-turn run this is fatal. A building drafted on turn 18 with a 60-turn payback is a dead card, and dead cards in a draft game are the fastest way to make a session feel bad. §2.1 inverts it.

### §0.3 — Combat is a two-parameter exponential — DERIVED

Civ VI resolves every fight through one curve:

```
damage = 30 * exp(0.04 * delta_strength)
```

| Δ Strength | Damage dealt | Damage taken | Exchange ratio | Read |
|---|---|---|---|---|
| 0 | 30.0 | 30.0 | 1.0 : 1 | Coin flip |
| +5 | 36.6 | 24.6 | 1.5 : 1 | Slight edge |
| +10 | 44.8 | 20.1 | 2.2 : 1 | Winning trade — one era of tech |
| +15 | 54.7 | 16.5 | 3.3 : 1 | Dominant |
| +20 | 66.8 | 13.5 | 4.9 : 1 | Near-unloseable |
| +30 | 99.6 | 9.0 | 11.1 : 1 | Opponent has no game |

One era of tech (~+10 strength) buys a 2.2:1 exchange. Two eras is an auto-win. **This exponent is too steep for a PvP ladder where power can be purchased.** §2.3 flattens it.

### §0.4 — Class taxonomy is already a hidden triangle — DERIVED

The 60 combat units resolve into 13 `PromotionClass` values: `MELEE`, `RANGED`, `ANTI_CAVALRY`, `LIGHT_CAVALRY`, `HEAVY_CAVALRY`, `SIEGE`, four naval classes, two air classes, `MONK`.

`ANTI_CAVALRY` exists for exactly one purpose — it is defined by what it counters. Rock-paper-scissors is already latent in the data; Civ VI spreads it across 13 buckets and hides it behind promotion trees. §2.3 extracts it explicitly.

### §0.5 — Constants carried forward

| # | Constant | Value | Source | Used in |
|---|---|---|---|---|
| 1 | Tech cost growth | ×1.675 / era | Technologies.xml, 68 nodes | §2.2 perk costs |
| 2 | Unit power growth | ×1.186 / era | Units.xml, 60 combat units | §2.2 Age power table |
| 3 | Unit cost growth | ×1.398 / era | Units.xml | §2.2 Train card costs |
| 4 | Building cost growth | ×1.399 / era | Buildings.xml, 51 non-wonder | §2.2 Build card costs |
| 5 | Power-to-cost exponent | ~0.5 (sqrt) | Ratio of #2 to #3 | §4.5 commander budget |
| 6 | Combat curve exponent | 0.04 → **0.025** | Civ VI damage formula | §2.3 flattened for PvP |
| 7 | Building payback window | 30–130 turns | Cost ÷ yield, 9 samples | §2.1 inverted and capped |

**Constant #5 is the design's spine.** Because power grows ×1.19 and cost ×1.40 per era, doubling investment buys ~√2 ≈ 1.41× the power. Every purchasable advantage in §4 must obey the same square-root relationship.

---

## §1 — Core loop translation

### §1.1 — The compression problem

A Civ VI game is 250–500 turns across 6–10 hours, with 10–20 discrete decisions per turn (every unit moved individually, every city build queue, tech, civic, government, religion, diplomacy, trade). Total decision count is in the tens of thousands.

Target: **3–5 minutes**. This is not a 100:1 time compression problem, it is a **decision-density** problem. The naive approach — same game, faster turns — produces an unreadable phone screen. EPOCH reduces the game to **exactly one decision per turn** and makes that decision meaningful by making it a draft.

### §1.2 — The run: 24 turns, 4 Ages, one choice each — CHOSEN

- 24 turns, ~7s median turn, ~3:00 target run length
- 4 Ages, 6 turns each
- 1 decision per turn

Each turn the player is dealt **three cards** and keeps one. Cards come from three decks, weighted by current Age:

| Deck | Effect |
|---|---|
| **BUILD** | Places a yield structure. Permanent income. The engine deck. |
| **TRAIN** | Deploys a unit into a lane. The board deck. |
| **ADVANCE** | Takes a rogue-lite perk and pushes Age progress. The scaling deck. |

After the pick the board auto-resolves: yields tick, lanes push forward one tile, contested tiles fight. The player watches ~2 seconds, then the next hand deals. **No unit selection, no movement, no confirm step. Tap a card, watch the board answer.**

### §1.3 — The board: three lanes, five tiles, vertical phone — CHOSEN

Civ VI's hex map becomes three parallel five-tile lanes in portrait. Player capital at the bottom, opponent snapshot at the top. Tiles 1–2 are yours, 4–5 theirs, tile 3 is the contested middle.

Lanes: A (left flank), B (centre — contested first), C (right flank).

**Why lanes and not a grid.** A 5×7 hex grid is 35 tap targets on a 6-inch screen and demands pinch-zoom; it also reintroduces per-unit movement, the single biggest time sink in Civ VI. Three lanes gives a genuine positional decision — *which* lane to commit to — while reducing the input space to three targets. Tile granularity inside the lane keeps "how far forward am I" legible at a glance.

**TRADEOFF (accepted).** Lanes cost us terrain. Civ VI's rivers, hills, forests and adjacency bonuses are a large part of why its map is interesting. Mitigation: **lane modifiers** — each run randomly assigns each lane one trait:

| Modifier | Effect |
|---|---|
| River | +1 Growth from Builds placed here |
| Highland | Defender gets +3 Power |
| Coast | Units push 2 tiles per turn instead of 1 |

This restores adjacency-style decision-making at ~5% of the UI cost. It does not fully replace it.

### §1.4 — Asynchronous PvP resolution — CHOSEN

The opponent is a **snapshot**, not a live player: their last completed 24-turn run replays as a deterministic script against your board. Real-player variety and real ladder stakes without matchmaking liquidity requirements, netcode, or disconnect-forfeit handling.

| Property | Implementation |
|---|---|
| Matchmaking | Snapshot from a ±100 Elo band; falls back to bot-authored snapshots below 200 daily actives in a band |
| Determinism | Server seeds the card RNG per run; client replays it. Server authoritative on outcome |
| Fairness | **Both players draft from the same seed.** Identical card sequence, different choices |
| Result | Score differential at turn 24, not board wipe — prevents a losing run ending early and killing session length |

**The same-seed rule is load-bearing.** It converts a match from "did I get luckier draws" into "did I draft better" — the difference between a ladder players trust and one they abandon at Elo 1200. It also makes every loss legible: the post-run screen shows the opponent's pick next to yours, turn by turn. That replay screen is the retention hook and the growth engine (§6).

### §1.5 — Session envelope and the turn clock

See §4.4 for the corrected parameters. Summary: runs are gated by **Campaign Tokens**; unranked runs remain unlimited and free, and spending a token is what makes a run count for ladder points, chests and Battle Pass XP.

**RECOMMENDATION — gate the loot, not the game.** The aggressive alternative (no play at all at zero tokens) converts better in week one and measurably worse by D30, because players who churn on an empty energy bar are disproportionately those who would have become payers in month two. Recommendation is the soft gate plus a more aggressive chest economy, A/B tested at soft launch. **This is still open — see §9.**

### §1.6 — Beat sheet: what a single run feels like

Pacing is the deliverable, not the systems.

| Turns | Age | Deck weighting (Build/Train/Advance) | Player question | Beat |
|---|---|---|---|---|
| 1–3 | I | 70 / 20 / 10 | Which lane is worth my economy? | **Commit.** Lane modifiers revealed turn 1; opening three picks set the run's identity. |
| 4–6 | I | 45 / 35 / 20 | Contest the middle now or bank? | **First contact.** Lane B collides ~turn 5. First feedback on the read. |
| 7–12 | II | 35 / 40 / 25 | Am I ahead on board or on curve? | **Divergence.** The two viable lines separate. Most-watched turns on the replay screen. |
| 13–18 | III | 20 / 45 / 35 | Which Keystone, and can I support it? | **Spike.** Keystone perks enter the pool. One pick visibly changes the board. |
| 19–21 | IV | 0 / 60 / 40 | Where do I dump everything? | **Commitment.** Build cards stop appearing (§2.1 payback ceiling). Pure aggression. |
| 22–24 | IV | 0 / 65 / 35 | Can I hold the middle to the bell? | **Bell.** Score locks at turn 24. Straight into the replay screen — no results modal, no interstitial. |

**RISK.** Turns 7–12 are where this design most plausibly falls apart. If the Growth-versus-Insight tension resolves into one dominant line, the middle six turns become autopilot and 40% of the experience goes flat. This is what the 500-run economy simulation (§10) is for, and it should run before any art is commissioned.

---

## §2 — Systems deconstruction

### §2.1 — Economy: six yields become two — CHOSEN

Civ VI runs Food, Production, Gold, Science, Culture, Faith, plus Amenities, Housing and Loyalty as constraint systems. Nine tracked numbers is ~six too many for a HUD readable in a 7-second turn on a one-handed phone.

**The merge is justified by the data.** Library (90 cost, +2 Science, 45-turn payback) versus Market (120 cost, +3 Gold, 40-turn payback): same district pattern, same era, near-identical ROI. They differ only in which victory track they feed. With one victory condition — score — that distinction carries no decision weight.

| Yield | Merges | Buys | Character |
|---|---|---|---|
| **GROWTH** | Food + Production + Gold | Build and Train cards | Tempo — spend it, see something appear |
| **INSIGHT** | Science + Culture + Faith | Advance cards, Age progress | Scaling — spend it, get stronger later |

The entire economic decision every turn: **board now, or curve later.** That is the real Civ VI decision stripped of accounting.

| Source | Growth/turn | Insight/turn | Notes |
|---|---|---|---|
| Capital (base) | 3 | 2 | Turn-1 income |
| Build card tier 1 | +1 | — | Or +1 Insight variant |
| Build card tier 2 | +2 | — | Age 2+ |
| Build card tier 3 | +3 | — | Age 3+ |
| Build card tier 4 | +4 | — | Age 4 |
| Held contested tile | +1 | +1 | The reason to fight at all |

**The payback ceiling.** To fix the inverted ROI curve in §0.2, every Build card must satisfy:

```
Cost / YieldPerTurn <= (24 - CurrentTurn) / 2
```

A turn-6 building must repay in ≤9 turns; a turn-18 building in ≤3. Practical effect: **Build cards stop being offered in the last four turns** and deck weights shift to Train and Advance — which is also the pacing shape you want, since the final Age should feel like a fight, not a spreadsheet.

### §2.2 — Progression: 68 techs become 24 drafted perks — CHOSEN

Civ VI's tech tree is 68 nodes across 8 eras with a parallel 50-node civic tree. Neither survives a 3-minute session, and a linearised version removes the choice that made the tree interesting.

**The rogue-lite inversion:** instead of choosing a path through a fixed tree, the player is offered 3 perks from a pool of ~60 and keeps one, up to 24 times. Run-to-run variance from the draw; mastery from knowing which perks combine. This is Slay the Spire's structure applied to Civ VI's content. It solves both problems a tree cannot: three cards fit a phone screen, and run #200 differs from run #2.

| Age | Turns | Civ VI eras folded in | Perk cost (Insight) | Ratio | Unit Power | Unit cost (Growth) | Build cost |
|---|---|---|---|---|---|---|---|
| I — Dawn | 1–6 | Ancient | 2 | — | 10 | 6 | 8 |
| II — Bronze | 7–12 | Classical + Medieval | 3 | 1.50 | 12 | 8 | 11 |
| III — Steel | 13–18 | Renaissance + Industrial | 5 | 1.67 | 14 | 11 | 16 |
| IV — Modern | 19–24 | Modern + Atomic + Information | 9 | 1.80 | 17 | 16 | 22 |
| **Geo-mean per Age** | | | **×1.65** | | **×1.19** | **×1.39** | **×1.40** |
| *Civ VI shipped, per era* | | | *×1.675* | | *×1.186* | *×1.398* | *×1.399* |

**DESIGN DECISION — defended.** Two ways to fold 8 eras into 4 Ages:

- **(a)** Preserve the per-step ratio — one Age advances like one Civ era. Total Age I→IV power spread ×1.70.
- **(b)** Preserve the total range — one Age advances like two Civ eras, ratios square (power ×1.41/Age, cost ×1.95/Age). Total spread ×3.3.

**Chose (a).** Under (b), an Age-I unit is worthless by Age III, meaning every card drafted in the first third of the run stops mattering and — critically — **every commander a player owns is invalidated by the next banner.** That is the mechanism by which mid-core gacha titles lose their two-year cohort. Under (a) an Age-I unit still trades ~1.2:1 against an Age-IV unit and an old roster stays playable.

**COST OF THIS CHOICE (real).** Ascending an Age feels numerically underwhelming. A ×1.19 power bump is not a moment. That has to be paid for in art — see §7.3. It is an art-budget line item, not a design one, and must be in the schedule from day one rather than bolted on.

**Perk pool shape:**

| Archetype | Pool | Example | Role |
|---|---|---|---|
| Economic | 16 | +1 Growth per Build you own | Compounding; rewards early draft |
| Military | 16 | Sword units gain +2 Power | Board pressure; class-tied |
| Tempo | 12 | Train cards cost 2 less this Age | Burst; corrects a bad opening |
| Conversion | 8 | Convert 3 Insight to 5 Growth each turn | Pivot; punishes over-commitment |
| Keystone | 8 | Contested tiles yield double | Build-defining; Age III+ only, one per run |

### §2.3 — Combat: from tactics to one-tap resolution — CHOSEN

**Counter triangle.** Thirteen promotion classes reduce to three, extracted from relationships already in the data (§0.4):

```
SWORD (melee)      beats SPEAR
SPEAR (anti-cav)   beats HORSE
HORSE (cavalry)    beats SWORD
```

- Counter bonus: **+40% Power**, applied before the damage curve.
- Ranged and Siege are **not classes** — they are a keyword, `REACH`, letting a unit contribute Power from tile 2 of the lane without taking return damage on the first clash.
- Naval and Air classes are **cut from v1**. They exist in Civ VI to make a world map interesting; there is no world map here.

**Resolution.** Fully automatic. When opposing units occupy the same tile, lane Power totals are compared and the exponential determines destruction:

```
damage = 25 * exp(0.025 * delta_power)      delta_power clamped to ±40
```

| Δ Power | EPOCH exchange | Civ VI exchange (0.04) | Read |
|---|---|---|---|
| 0 | 1.0 : 1 | 1.0 : 1 | Mutual destruction |
| +7 (one Age) | 1.42 : 1 | 1.75 : 1 | Edge, not a win |
| +15 | 2.12 : 1 | 3.32 : 1 | Strong |
| +25 | 3.49 : 1 | 7.39 : 1 | Dominant, still counterable |
| +40 (clamp) | 7.39 : 1 | 24.53 : 1 | Hard ceiling on purchased advantage |

**Why 0.025 and not 0.04.** Civ VI's steeper exponent is correct where the strength gap is earned over six hours and the loser can retreat, tech up and re-engage. In a 24-turn ladder run there is no retreat and the gap can be *bought*. At 0.04 with a ±40 clamp, a maximally invested player trades at 24.5:1 — that is not a match, it is a cutscene. At 0.025 the same maximum advantage trades at 7.4:1: decisive and worth paying for, but upsettable by a correct counter-triangle draft and a lane commitment.

**The clamp is the more important half.** It sets a hard ceiling on what money can buy and is the single mechanic keeping the ladder credible.

**Stacking.** Civ VI's one-unit-per-tile rule exists to make positioning matter on a hex map; with three lanes it would just cap board state at three units and make the mid-game static. EPOCH uses **soft stacking**: up to 3 units per lane, lane Power is the sum with diminishing returns — **100% / 75% / 50%** for first, second, third unit. Preserves "quality beats quantity" without positioning UI, and gives a losing player a real comeback lever (flood one lane) that is strong but not free.

### §2.4 — What is cut, and the honest cost

| Civ VI system | Disposition | What we lose |
|---|---|---|
| Religion (Beliefs, Prophets, 6 belief types) | **Cut** | A whole victory path and a strong flavour layer. Faith folds into Insight. |
| Diplomacy, Gossip, Agendas | **Cut** | Meaningless in 1v1 async. Revisit only if a 4-player mode is added. |
| Districts & adjacency | **Folded** | Becomes lane modifiers (§1.3). **Biggest real loss in the design.** |
| Great People (9 classes, ~200 individuals) | **Repurposed** | Becomes the Commander gacha roster (§4). Strong content mine, already written and historically grounded. |
| Wonders (29, cost 180–1850) | **Repurposed** | Becomes the Keystone perk tier — 8 run-defining picks, Age III+. |
| Naval / Air domains | **Cut v1** | ~20 units of content. No map for them to matter on. |
| Amenities, Housing, Loyalty | **Cut** | Constraint systems that need a long game to bite. Nothing replaces them. |

---

## §4 — Hard monetization data

Context: territory is **US-only**; age rating is explicitly not a design constraint for this pass; the game is optimized for in-app purchase with a turn clock players can pay to refill.

### §4.1 — Gem pricing ladder — CHOSEN

Six US price points on standard tiers. Gems-per-dollar climbs with pack size so the ladder pulls upward, but the spread is deliberately narrow — 80.8 to 108.0 gems/$ , a 34% range. A wider spread makes small packs feel like a punishment and suppresses the $0.99 and $4.99 tiers that do the conversion work.

| SKU | Price | Gems | Bonus | Gems/$ | Pulls | $/pull | Role |
|---|---|---|---|---|---|---|---|
| Pouch | $0.99 | 80 | — | 80.8 | 0.5 | $1.98 | First-purchase trigger |
| Sack | $4.99 | 420 | +5% | 84.2 | 2.6 | $1.90 | Impulse / minnow |
| Chest | $9.99 | 900 | +13% | 90.1 | 5.6 | $1.78 | Volume anchor |
| Vault | $19.99 | 1,900 | +19% | 95.0 | 11.9 | $1.68 | Dolphin default |
| Hoard | $49.99 | 5,000 | +25% | 100.0 | 31.2 | $1.60 | Whale step |
| Treasury | $99.99 | 10,800 | +35% | 108.0 | 67.5 | $1.48 | Ceiling / status |

- **Pull price: 160 gems.**
- **Ten-pull: 1,440 gems** (10% discount), guaranteed epic-or-better.
- Every SKU **doubles on first purchase**, once per account per SKU.

### §4.2 — Pity curve — DERIVED from chosen parameters

Base legendary rate 2.0%. Soft pity begins at pull 61 and ramps **+4.90 percentage points per pull**, reaching certainty at pull 80. The ramp is derived, not chosen: `(100% − 2%) / (80 − 60) = 4.9%`.

| By pull | 10 | 20 | 30 | 40 | 50 | 60 | 65 | 70 | 75 |
|---|---|---|---|---|---|---|---|---|---|
| P(legendary) | 18.3% | 33.2% | 45.5% | 55.4% | 63.6% | 70.2% | 88.3% | 99.2% | 100% |

- **Expected pulls per legendary: 36.61** (verified analytically and by 400,000-trial Monte Carlo — both give 36.61)
- **Effective legendary rate: 2.73%** — this is the number that must be published, not the 2.0% base
- **Cost per legendary: $54.24** (best pack, $1.48/pull) to **$72.49** (worst, $1.98/pull)

**Note the shape.** 70% of legendaries land before soft pity engages, which keeps the pull loop feeling like gambling rather than a countdown. The last 30% resolve in a violent 10-pull window between 61 and 70 — that window is where "one more pull" behaviour lives, and it is worth a dedicated VFX escalation on the pull screen from pull 61 onward.

### §4.3 — Shard ladder and the whale ceiling — CHOSEN

Legendary duplicates convert at **20 shards**. There are no dead pulls.

| Step | Shards | Cumulative | Dupe-legendaries | Est. pulls | Est. cost |
|---|---|---|---|---|---|
| Unlock | 60 | 60 | 3.0 | 110 | $163 |
| 2★ | +20 | 80 | 4.0 | 146 | $217 |
| 3★ | +40 | 120 | 6.0 | 220 | $325 |
| 4★ | +80 | 200 | 10.0 | 366 | $542 |
| 5★ | +120 | 320 | 16.0 | 586 | $868 |
| 6★ | +200 | 520 | 26.0 | 952 | **$1,410** |

$1,410 to fully ascend one legendary through pulls alone is the whale ceiling per character.

**Shards must also be earnable** — ladder seasons, event shops, Battle Pass tracks — so a F2P path to a single 4★ legendary exists at roughly a two-season grind. With no F2P path the roster stops being aspirational and becomes a paywall, and players read the difference instantly.

### §4.4 — Token economy, corrected — CHOSEN

**v1 contained a real error: the v1 token economy did not gate anything.** At cap 5 / 25-min refill / 3 daily grant / 1 token per run, a player opening the app three times a day completes **18 runs** — 54 minutes of play — without hitting the wall.

Runs available per day, assuming 16 waking hours, evenly spaced sessions, and whole runs only:

| Configuration | 2 logins/day | 3 logins/day | 5 logins/day | Verdict |
|---|---|---|---|---|
| v1: cap 5, 25 min, grant 3, cost 1 | 13 | 18 | 28 | Gate never binds |
| cap 4, 30 min, grant 3, cost 1 | 11 | 15 | 23 | Still loose |
| cap 3, 40 min, grant 2, cost 1 | 8 | 11 | 17 | Binds, but punishes returners |
| **v2: cap 5, 25 min, grant 3, cost 2** | **6** | **8** | **12** | **Recommended** |

**Why raise the cost instead of tightening the cap.** Cap and refill rate govern the *returning* player; run cost governs the *engaged* player. Cutting the cap to 3 punishes exactly the person you want back — someone away eight hours who opens the app to a nearly empty bar. Raising ranked cost to 2 tokens leaves the returning player a full bar and a satisfying burst, and binds only after 4–5 consecutive games, which is when a session should end anyway. **The bar still fills to a visibly generous 5; it just buys fewer ranked games.** Same conversion pressure, better re-entry feeling.

At 8 ranked runs per day the gate binds ~24 minutes into an engaged player's day. Unranked play remains unlimited and free, so the wall never blocks the game — only the rewards.

Refill pricing: **30 gems** for one token, **120 gems** for a full bar (4× not 5× — small bulk incentive).

### §4.5 — Commander power budget — CHOSEN

```
Max aggregate commander contribution = delta Power 8   (20% of the ±40 clamp)
```

| Δ Power | Exchange ratio | Equivalent | Assessment |
|---|---|---|---|
| 4 | 1.22 : 1 | Half an Age | Too weak to sell |
| 7 | 1.42 : 1 | One full Age of tech | Reference point |
| **8** | **1.49 : 1** | One Age, slightly better | **The budget** |
| 14 | 2.01 : 1 | Two Ages | Ladder becomes pay-gated |
| 40 | 7.39 : 1 | Clamp | Not a match |

**The rule, stated so it can be enforced in review:** a fully maxed roster is worth **one Age of technology**. Real, purchasable, felt — and beatable by a correct counter-triangle read and a better lane commitment, both of which are free.

**Commander buffs must be lateral, not vertical.** A commander grants a *lane-shaped* effect ("+1 Growth per turn in Lane A", "Spear units gain REACH", "start with 2 extra Insight"), never a flat power multiplier. Vertical buffs stack multiplicatively with the Age curve and blow through the ±40 clamp; lateral buffs change how you draft, which is what players actually want to buy.

**TENSION (not papered over).** Δ8 will feel low against a category where maxed rosters routinely deliver 2–4× effective power. Whales will notice and say so. The compensation must be **breadth, not height**: a large roster buys more viable drafts, more counters to more openings, more ways to convert a bad hand — plus visible cosmetic status on the highest-traffic screen in the game. That is the Clash Royale lesson and the only version with a two-year cohort at the end. If the number moves, it moves to Δ10 (1.65:1) and no further, and only with ladder-health telemetry justifying it.

### §4.6 — Revenue model — BENCHMARK

Per 100,000 installs, 180-day window. Segment shares and spend levels are category benchmarks, **not forecasts**.

| Segment | Share | Users | Monthly | Months | Revenue |
|---|---|---|---|---|---|
| Non-payer | 96.50% | 96,500 | — | 1.4 | $0 |
| Minnow | 2.20% | 2,200 | $4.99 | 2.5 | $27,445 |
| Dolphin | 1.05% | 1,050 | $24.99 | 5.0 | $131,198 |
| Whale | 0.22% | 220 | $119.00 | 8.0 | $209,440 |
| Kraken | 0.03% | 30 | $480.00 | 11.0 | $158,400 |
| **Total** | **3.50%** | 100,000 | | | **$526,482** |

| Scenario | Conversion | Revenue / 100k | LTV / install | ARPDAU |
|---|---|---|---|---|
| Conservative | 2.00% | $115,734 | $1.16 | $0.082 |
| **Base** | **3.50%** | **$526,482** | **$5.26** | **$0.371** |
| Upside | 4.80% | $1,249,523 | $12.50 | $0.880 |

**Be honest about the base case.** 3.5% conversion and $0.37 ARPDAU sit at the *top* of what mid-core strategy delivers; typical is 1.5–3.0% and $0.15–0.35. **Plan the studio against the conservative row** and treat base as the target, not the assumption. The conservative row determines whether this is fundable.

**Revenue mix (base case):**

| Source | Share | Base revenue |
|---|---|---|
| Commander gacha | 56% | $294,830 |
| Battle Pass | 23% | $121,091 |
| Convenience / tokens | 14% | $73,707 |
| Cosmetics (direct) | 7% | $36,854 |

**UA viability:**

| CPI | ROAS (base) | Verdict |
|---|---|---|
| $1.50 | 3.51× | Scale hard |
| $2.50 | 2.11× | Viable |
| $3.50 | 1.50× | Marginal |
| $5.00 | 1.05× | Not viable |

US strategy-game CPI runs $3–6 on paid social. **At base-case LTV, paid UA is marginal at best and underwater at realistic rates** — which is why §6 treats the replay screen's organic reach as a survival requirement, not a marketing nice-to-have.

### §4.7 — Entry offer ladder — CHOSEN

| Price | Offer | Trigger | Contents |
|---|---|---|---|
| $0.99 | First Purchase | After run #5 | 160 gems (2×) — exactly one pull. One time per account. |
| $4.99 | Starter Bundle | After first ranked loss | 1 guaranteed **epic** commander, 20 tokens, 1 card back. 72-hour window. **Never a legendary** — it prices the top of the roster too cheaply and cannibalises the banner. |
| $4.99/mo | Gem Subscription | Day 3 | 90 gems daily (2,700/mo, a 3× value framing), +1 chest slot. **Ships in v1** — subscriptions are the most reliable ARPDAU floor in the category. |
| $9.99 | Battle Pass | Season start | 50 tiers, 8-week season. ~9% of actives. |
| $24.99 | Battle Pass+ | Season start, re-offered at tier 10 | 15-tier skip + exclusive lane skin. ~2.5% of actives. |

Battle Pass combined generates ~**$45,700 per season per 100k installs**, or ~$147,000 across the 180-day window — 28% of base-case bookings.

The $0.99 first-purchase offer is **not a revenue line, it is a conversion mechanic.** Getting a player to transact once roughly triples their probability of transacting again; set the trigger early (run #5) even at negligible margin.

**Do not sell:** extra turns beyond 24, or a starting Age above I. Both break the same-seed fairness rule in §1.4, which is the ladder's entire credibility.

**Other convenience SKUs:** instant chest unlock; chest slot expansion (4→6); **run rewind** (one card re-draft per run — sells well, low balance impact, most-requested feature in every draft game ever shipped).

**Cosmetic sinks:** map/lane skins, unit particle effects, card-back and UI themes, commander portrait variants, victory animations, capital skylines, emotes on the replay screen.

---

## §5 — Commerce and backend architecture

### §5.1 — Channel margin: the biggest financial decision in this document

US-only territory is not just a compliance simplification. As of September 2026 it is a **margin opportunity**, and the window is open right now.

| Channel | Platform take | Net on $100 | Status |
|---|---|---|---|
| **iOS link-out to web shop (US)** | **0%** | **$100.00** | In effect now, post-*Epic* injunction |
| iOS link-out — Apple's proposed rate | 15% | $85.00 | Filed Aug 2026, awaiting district court |
| Apple IAP — Small Business (<$1M/yr) | 15% | $85.00 | Standard |
| Apple IAP — standard | 30% | $70.00 | Standard |
| Google Play IAP | 30% | $70.00 | Standard |
| Google Play link-out — proposed | 20% | $80.00 | In motion |
| Web shop direct (PSP fees only) | ~5% | $95.00 | Baseline cost of payments |

**What this is worth.** On base-case bookings of $526k per 100k installs, moving even **40% of gem revenue** from 30% IAP to a ~5%-cost web shop is roughly **$53,000 per 100k installs** of pure margin — larger than the entire cosmetics line, and the difference between the marginal and viable rows of the UA table in §4.6.

**LIVE LEGAL SITUATION — verify before pricing on it.** The 0% figure exists because Apple was barred from collecting fees on external links following the April 2025 *Epic* ruling. The Ninth Circuit modified this in December 2025 to permit a commission covering "genuinely and reasonably necessary" coordination costs, and Apple filed a tiered 15% / 10% / 5% proposal in August 2026. The Supreme Court was expected to rule on the related contempt matter around **14 September 2026** — six days after this document's date. Google's US external-link fees (proposed 20% digital / 10% subscriptions) are likewise unsettled.

**Design the commerce layer to be rate-agnostic.** Every price, bonus percentage, and channel take rate lives in server-side remote config. If the rate moves from 0% to 15% overnight, that is a config push, not a client release and a two-week review cycle.

### §5.2 — Commerce architecture

| Layer | Requirement | Notes |
|---|---|---|
| Catalog | Server-authoritative, remote-config driven | Client renders SKUs from a signed manifest. **Never hardcode a price in the client.** Prices, offers, bonus %, channel routing all live-tunable. |
| Receipt validation | Server-side only, both stores | App Store Server API v2 with S2S notifications v2; Google Play Developer API with Real-time Developer Notifications via Pub/Sub. Client-side validation is trivially spoofed and must not exist even as a fallback. |
| Entitlement ledger | Append-only, idempotent by transaction ID | Grants are events, not balance mutations. Balance is a projection. Makes refunds, chargebacks, support credits and disputes tractable. |
| Account identity | Sign in with Apple + Google + email, **day one** | A web shop is unreachable without a portable account. The hard dependency most teams discover too late — architectural, not a feature. |
| Web shop | Authenticated, one-tap from in-client link-out | Deep-link with a short-lived signed token so the player never re-authenticates. Friction here destroys the entire margin thesis. |
| PSP | Primary + fallback, card-network redundancy | Gaming-focused processors handle digital-goods chargeback profiles better than general-purpose ones. |
| Fraud & refunds | Velocity limits, device fingerprinting, refund clawback | Clawback must reverse the **entitlement**, not just the currency, or refund abuse becomes a free-legendary exploit. |
| Analytics | Every offer **impression**, not just conversions | Impression-level logging is the only way to compute true offer conversion. Retrofitting is expensive. |

### §5.3 — Store policy compliance

- **Published odds.** Apple (Guideline 3.1.1) and Google both require disclosed probabilities for any randomized paid item, enforced at review. This is store policy, not law, and independent of age rating and territory. Publish the **effective** rate — 2.73% including pity — alongside the base rate. Publishing only the 2.0% base is technically true and reads as deceptive.
- **Odds surface.** Reachable in **two taps** from the banner, not buried in settings. Include the full pity table from §4.2.
- **Link-out presentation.** The external purchase option may not be more prominent than the IAP option. Design the store with the two paths visually equal; the price difference does the persuading, not the styling.
- **Purchase confirmation.** Show gem balance before and after on every transaction.
- **Restore purchases.** Required and routinely missed at review.

---

## §6 — Social and virality: the replay screen

§4.6 established the problem plainly: at realistic US CPIs, paid acquisition is marginal to underwater. Organic reach is therefore not *a* growth channel, it is **the** growth channel, and the replay screen is the only surface with the traffic and the shape to deliver it.

### §6.1 — Why this screen and not a share button

Every player sees the replay screen at the end of every run — 100% of sessions, roughly **eight impressions per engaged player per day** (§4.4). Nothing else comes close.

More importantly its content is **inherently comparative**: your 24 picks against your opponent's 24 picks, same seed, different choices. That is already the structure of a short-form video — setup, divergence, payoff. It does not need to be invented, only exported.

### §6.2 — Export specification

| Property | Spec | Why |
|---|---|---|
| Aspect | 9:16, 1080×1920 | Native for Reels / Shorts / TikTok. Never letterbox a 16:9 capture. |
| Duration | 6–12s, auto-cut to the 3 divergence turns | Full 24 turns is unwatchable. The algorithm decides in 2 seconds. |
| Hook frame | First 0.5s shows **the result, not the setup** | "I won on turn 24 with one Spearman" — outcome first, always. |
| Silent-legible | Burned-in captions, no audio dependency | Majority of short-form is watched muted. |
| Generation | Client-side render from the deterministic seed | The run is already a replayable script (§1.4). Re-render offline at 1080×1920 — no server video pipeline, no cost per share. |
| Watermark | Corner mark + the run's **seed code** | The seed is the growth mechanic — see §6.3. |
| Latency | <3s from tap to system share sheet | Above 3s share rate collapses. Hard performance budget. |

### §6.3 — The seed code is the actual growth loop

Because both players draft from an identical card sequence (§1.4), **any run is perfectly reproducible from a short seed code** (format: `K7-QMRA-92`). Stamp it on every exported video.

A viewer who sees a clip can type the seed and play *the exact same run* — same cards, same opponent snapshot, same turn 24 — and find out whether they would have done better.

That converts a passive view into a challenge with a built-in comparison, and gives creators a format that needs nothing else built: **"beat my seed."** It is cheap — a string, a lookup, and a deterministic replay we already need for the ladder — and it is the difference between a share button nobody presses and a loop that compounds.

### §6.4 — Supporting hooks

- **Near-miss auto-prompt.** Runs decided by <5 score at turn 24 auto-surface the share prompt. Close losses share better than wins and are the emotionally loaded moment.
- **Weekly seed challenge.** One global seed per week, everyone plays the same run, leaderboard by score. Perfectly fair, zero matchmaking cost, natural weekly content beat for creators. **Most likely single mechanic to lift k above target.**
- **Creator codes** tied to the seed system, with attribution on installs arriving via a seed link.
- **Cosmetics are the share payload.** Skins, particle effects and card backs appear in every exported clip. This is what makes cosmetics worth buying at all — designing the replay screen as a showcase rather than a results table is a revenue decision, not an aesthetic one.
- **Do not gate sharing behind a reward.** Share-to-earn produces junk shares that platforms down-rank and viewers ignore. The clip has to be worth posting on its own.

### §6.5 — Share funnel and the numbers that decide it

| Step | Target | Read |
|---|---|---|
| Runs completed → replay screen viewed | 100% | Structural — no interstitial, no results modal |
| Replay viewed → share sheet opened | 4–7% | Below 2%, the clip is not worth posting. Redesign, do not incentivise. |
| Share sheet → post completed | 55% | Drop-off here is almost always export latency (§6.2) |
| Post → median views | 300+ | Below this the format is not legible to non-players |
| Views → installs | 0.4–1.2% | Seed code on-frame is the main lever |

```
k = (share rate) × (posts/share) × (views/post) × (installs/view)
target: k > 0.15
```

At the target figures: `k ≈ 0.05 × 0.55 × 300 × 0.008 ≈ 0.066` — meaningful but not self-sustaining.

**That is the honest expectation:** organic reach at these numbers cuts blended CPI by roughly a third rather than replacing paid UA. It moves the §4.6 UA table from marginal to viable; it does not make acquisition free. If k clears 0.15 the economics change completely.

---

## §7 — Visual and UI framework

Reference device: **390 × 844 pt** (6.1" phone), portrait, one-handed.

### §7.1 — Thumb-zone budget

Natural thumb arc from a bottom-corner grip reaches comfortably to ~60% of screen height (**506 pt**). Everything tapped in a 7-second turn must live in the bottom 500 pt.

| Zone | Height (pt) | Contents | Interactive |
|---|---|---|---|
| Status bar | 0–60 | System + token bar, gem count | Store entry only |
| Opponent field | 60–250 | Opponent capital, their lane units | **No** — read-only |
| Contested band | 250–390 | Tile 3 of all three lanes, combat VFX | **No** — auto-resolves |
| Player field | 390–560 | Your units, capital, yields | Tap-to-inspect only |
| Card hand | 560–790 | Three draft cards | **Yes** — the only required input |
| Safe area | 790–844 | Home indicator clearance | Nothing |

**The layout follows from the loop, not from taste.** Because §1.2 reduced the turn to exactly one decision, only **one** region needs to be reachable. That is what makes a three-lane portrait board work where a hex grid cannot: the top two-thirds is a display, the bottom third is the entire interface.

**Enforcement rule:** if a feature requires a second tap target above 560 pt, it is fighting the core loop and should be cut or moved into the card.

### §7.2 — Touch targets and legibility

| Element | Size | Rationale |
|---|---|---|
| Card tap target | 118 × 190 pt | Far above Apple's 44pt and Google's 48dp minimums. Cards are the only input; make them unmissable. |
| Card gutter | 8 pt | Mis-tap protection between adjacent cards |
| Lane column width | 118 pt | Aligns lanes to cards — card 1 sits under lane A. **The spatial mapping is the tutorial.** |
| Unit token | 44 × 44 pt | Legible class silhouette at arm's length |
| Yield numerals | 28 pt, tabular | Two numbers only (§2.1); they can afford to be large |
| Body / card text | 15 pt minimum | Nothing below 15pt. A 7-second turn has no time for squinting. |
| Class encoding | Shape + colour + icon | **Never colour alone** — ~8% of male players have some colour vision deficiency, and Sword/Spear/Horse is the game's central read |

Class shape language: **Sword = square**, **Spear = circle**, **Horse = diamond**.

### §7.3 — The Age transition beat

§2.2 committed to a ×1.19 power step per Age and accepted that this is numerically underwhelming. That debt is paid here, and it must be scheduled, not improvised.

| Time | Event |
|---|---|
| 0.0s | Board desaturates, time-stop. Cards slide out. |
| 0.3s | Full-bleed Age card wipes in — era artwork, Age numeral, name |
| 0.6s | Music stem swaps. **One new instrument layer per Age** — cheapest, most reliable escalation signal available |
| 1.1s | Board returns re-skinned: new tile texture, new unit silhouettes, upgraded lane VFX, new card frame material |
| 1.6s | New hand deals |
| **Total** | **≤1.8s interruption, 4× per run** |

**Budget line for the dev conversation:** 4 Ages × (1 full-bleed illustration + 1 tile set + 1 card frame + 1 music stem + 1 VFX pass). **This is the single largest art line item in the project** and it exists to compensate for a deliberate design choice. If it gets cut for schedule, the Age system stops carrying its weight and §2.2's decision should be revisited rather than shipped hollow.

### §7.4 — Screen-by-screen layout spec

Eight screens are specified. Coordinates are pt from the top of a 390×844 frame.

#### 7.4.1 Run screen (Age I and Age IV are the same layout)

| Element | Position | Detail |
|---|---|---|
| Top safe strip | 0–16 | No content. Real status bar renders here — **never draw a fake one.** |
| HUD row | 16–64 | Left: 5 token pips + `n/5` + "TOKENS". Right: gem count, menu button (26×26) |
| Opponent header | 64–92 | Snapshot avatar (16pt circle), opponent Elo, their score right-aligned |
| Lane grid | 92–532 | 3 columns × 5 rows, 8pt gap, 370pt wide, inset 10pt each side |
| Lane labels | 536–550 | `LANE A · RIVER` etc., 8pt mono, centred per column |
| Yield bar | 552–588 | Left: Growth value 28pt + `GROWTH +n`. Centre: Insight. Right: `T04 / 24` and `AGE I · DAWN` |
| Card hand | 592–782 | 3 cards, 118×190, 8pt gutters |
| Bottom safe | 786–844 | Home indicator clearance |

Grid tile states: opponent tiles use a warm grey fill; the contested row (row 3) uses an amber fill with a heavier amber border; player tiles are white; the player capital tile is solid dark with `CAPITAL` + score.

Age IV differs only in content density: up to 11 units on board, 3-stacks visible in one lane, unit tokens drop to 30pt when stacked, and the third unit in a stack renders at 42% opacity to signal its 50% power contribution.

#### 7.4.2 Replay / share screen

| Element | Position | Detail |
|---|---|---|
| Result block | 16–158 | Dark panel. `RUN COMPLETE · TURN 24`, `VICTORY` 30pt, score `41 — 38` 24pt. Two sub-panels: **SEED** (`K7-QMRA-92`) and **LADDER** (`1187 +18`) |
| Pick comparison | 158–430 | "Same cards. Different picks." Column headers YOU / SNAPSHOT. Three rows for the divergence turns (T07, T12, T18). Your picks amber-bordered, theirs neutral. Decisive turn gets a heavier border. |
| Explanation callout | inline | One sentence naming which turn decided it and why |
| Cosmetic showcase | 442–540 | Four slots: LANE SKIN, CARD BACK, VICTORY FX, STORE (+). Labelled `EQUIPPED — VISIBLE IN EVERY EXPORT` |
| **SHARE THIS RUN** | 576–638 | 62pt tall, full width, amber fill, upload icon + 17pt bold label. **The primary action — outranks Rematch.** Sub-caption: `9:16 · 8s · SEED STAMPED ON FRAME` |
| Secondary actions | 650–702 | Rematch / New run · 2 tokens, 52pt tall each |

#### 7.4.3 9:16 export frame (1080×1920)

| Element | Region | Detail |
|---|---|---|
| Top UI-safe margin | top 26pt equivalent | Platform chrome overlaps here — keep clear |
| Hook headline | upper third | 38pt bold, outcome-first, ≤8 words |
| Caption band | ~30% | White band, 15pt dark text, one line, burned in |
| Divergence pair | ~40% | Two boxes: YOU (amber) vs THEM (grey), turn number and pick name |
| Mini board | ~45–72% | 3×5 grid, contested row highlighted |
| Score | ~78% | 34pt mono, `41 — 38`, plus `+18 LADDER` in amber |
| **Seed stamp** | ~88% | Amber-bordered box: `PLAY THIS EXACT RUN` + seed code 21pt + arrow. **The growth mechanic.** |
| Watermark | bottom left | `EPOCH`, letter-spaced |
| Bottom UI-safe margin | bottom ~118pt equivalent | TikTok/Reels chrome |

#### 7.4.4 Store and banner screen

| Element | Position | Detail |
|---|---|---|
| Header | 16–58 | "Store", gem balance right |
| Featured banner | 70–242 | Dark panel. Commander name 25pt, rarity + lateral effect line, `PULL ×1 / 160` and `PULL ×10 / 1,440` buttons bottom-right |
| **Odds disclosure** | 250–~390 | Amber-bordered panel, chevron to full table. Shows `2.73% LEGENDARY EFFECTIVE`, `8.0% EPIC`, `90.0% RARE`, `80 HARD PITY`, plus base rate and ramp in body text. **Two taps from the banner — §5.3.** |
| Gem packs | 404–~500 | Three-up grid, gem count + price button |
| **Dual checkout** | 552–~740 | Two equal-weight cards: IN-APP `$19.99` / App Store checkout, and EPOCH.COM `$19.99 +15%` / 2,185 gems, opens browser. **Identical visual treatment — store policy forbids making the external option more prominent.** |
| Restore purchases | 752–796 | 44pt row, required |

### §7.5 — Card anatomy

Card is 118 × 190 pt. Six regions:

| # | Region | Height | Spec |
|---|---|---|---|
| 1 | Deck band | 26 pt | `BUILD` / `TRAIN` / `ADVANCE` / `KEYSTONE`, 9pt mono, letter-spaced, left-aligned. **Word, not icon** — three cards read in under two seconds only when the type is always in the same place. |
| 2 | Cost | in band, right | Tabular numeral. Growth for Build/Train, Insight for Advance. Colour-coded to the yield row, never colour alone. |
| 3 | Silhouette panel | flex | Class shape carries the counter triangle: square = Sword, circle = Spear, diamond = Horse. Shape + icon + colour. |
| 4 | Name | 15pt min | Nothing on the card drops below 15pt. |
| 5 | Effect line | one clause | **If an effect needs two lines at 15pt, the effect is too complex for this game.** A design constraint, not a layout constraint. |
| 6 | Counter chip | Train cards only | States the matchup explicitly ("beats Horse") rather than expecting the player to remember the triangle. |

Keystone and Advance cards use an amber border and amber deck band to mark them as run-defining.

---

## §8 — Decision register: what changed from v1 to v2

| # | Item | v1 | v2 | Why |
|---|---|---|---|---|
| 1 | Ranked run cost | 1 token | **2 tokens** | v1 gate never bound — 18 runs/day available (§4.4) |
| 2 | Commander power budget | Open question | **Δ Power 8** | One Age of tech; 20% of the clamp (§4.5) |
| 3 | Published odds | 2.0% base | **2.73% effective** | Base-only disclosure reads as deceptive (§5.3) |
| 4 | Commerce channel | Not specified | **Web shop primary** | 0% US link-out fee today; ~$53k per 100k installs (§5.1) |
| 5 | Portable accounts | Not mentioned | **Day-one requirement** | Hard dependency for the web shop (§5.2) |
| 6 | Prototype order | Replay screen first | **Replay + seed + export first** | Organic reach is a survival requirement, not marketing (§6.1) |
| 7 | Seed code | Internal fairness device | **The growth loop** | Same-seed determinism was already built (§6.3) |
| 8 | Planning case | Single model | **Conservative** | Base case sits at the top of category norms (§4.6) |

---

## §9 — Open questions

These are genuinely undecided. Do not invent answers.

1. **Soft vs hard token gate** — the D30 versus week-one conversion tradeoff in §1.5. Needs a soft-launch A/B.
2. **Ladder seasons** — length, reset curve, rank rewards. Unspecified.
3. **Keystone perk gating** — gacha-gated or earned only? **Standing recommendation: earned only.** They are build-defining and selling them is the most direct route to a ladder nobody trusts.
4. **Chest economy and drop tables** — the counterweight if the soft gate wins the A/B.
5. **Live-ops calendar** mapped to the Great People roster (§2.4).
6. **Apple and Google link-out rates** — genuinely undecided as of 8 September 2026 (§5.1).
7. **Art direction** — era illustration style, overall visual identity. The wireframes in §7 are structural only.
8. **Counter-triangle legibility** — does shape alone read without the text chip on Train cards? Needs a device test.

---

## §10 — Prototype order

1. **Replay screen + seed code + 9:16 export.** Promoted above everything else. §4.6 makes organic reach a survival requirement and §6.3 is the mechanic that delivers it. **If the clip is not worth posting, the business model does not close — and that is knowable in two weeks with no game attached.**
2. **500-run economy simulation** against the §2.1 tables, to confirm the Growth/Insight tension bites through turns 7–12 rather than resolving into one dominant line.
3. **One-handed play test** of the run screen on a real device — standing, one hand, in daylight.
4. **Commerce spine** — portable accounts, server-authoritative catalog, entitlement ledger. Architectural and expensive to retrofit.
5. **Commander design** against the Δ8 budget, only after the above.

**Two numbers decide whether this is a business:** `k` from §6.5, and whether the conservative row of §4.6 — $1.16 LTV per install — can be beaten. Both are testable before a single commander is designed or a single Age illustration is commissioned.

---

## §11 — Glossary

| Term | Meaning |
|---|---|
| **Age** | One of four 6-turn phases of a run (Dawn, Bronze, Steel, Modern). Folds 8 Civ VI eras into 4. |
| **Advance** | Card deck that grants a rogue-lite perk and pushes Age progress. Costs Insight. |
| **Build** | Card deck that places a permanent yield structure. Costs Growth. |
| **Campaign Token** | The energy currency. Cap 5, refills 1/25min, 3 granted daily, 2 spent per ranked run. |
| **Commander** | A gacha-acquired historical figure granting a lateral (lane-shaped) buff. Sourced from Civ VI's Great People tables. |
| **Contested tile** | Tile 3 of a lane — the middle. Yields +1 Growth and +1 Insight to whoever holds it. |
| **Growth** | Yield merging Food + Production + Gold. Buys Build and Train cards. |
| **Insight** | Yield merging Science + Culture + Faith. Buys Advance cards and Age progress. |
| **Keystone** | Run-defining perk tier, Age III+, one per run. Repurposed from Civ VI wonders. |
| **Lane** | One of three vertical five-tile columns (A, B, C). |
| **Lane modifier** | Per-run random trait on a lane: River, Highland, or Coast. |
| **REACH** | Keyword replacing Ranged/Siege classes — contribute Power from tile 2 without first-clash return damage. |
| **Seed code** | Short string (e.g. `K7-QMRA-92`) reproducing an entire run deterministically. The growth mechanic. |
| **Snapshot** | An opponent's completed 24-turn run, replayed as a deterministic script. Not a live player. |
| **Soft stacking** | Up to 3 units per lane, contributing 100% / 75% / 50% of Power. |
| **Train** | Card deck that deploys a unit into a lane. Costs Growth. |

---

*End of document. Source data: Civilization VI base ruleset XML (Units, Technologies, Buildings, Eras). All derived constants verified analytically and by Monte Carlo where stochastic.*
