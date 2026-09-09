# EPOCH Capital Progression Contract

**Status:** Authoritative Day 2 contract for outer-loop progression.  
**Tuning status:** Every economy amount and pacing figure in this document is **INITIAL-TUNING**, intended for implementation and playtesting rather than final balance.  
**Scope of authority:** Persistent Gold, battle rewards, landmark progression, capital completion, world unlocking, progression fairness, and minimum save/reward behavior. `EPOCH_PRODUCT_DIRECTION.md` remains authoritative for the outer product loop, and `EPOCH_Core_Gameplay_Spec_v1.md` remains authoritative for battle simulation.

## 1. Currency boundary

**Gold** is the persistent outer-loop currency.

- A new progression save starts with 0 Gold.
- Gold is account-wide, non-negative, and uncapped for the Day 2 prototype.
- Gold is earned from completed battles and capital-completion rewards.
- Gold is spent only on landmark upgrades during Day 2.
- Gold is not Growth or Insight. Growth and Insight exist only inside a battle and never carry into progression.
- Gold and all other progression state remain outside authoritative battle state, battle resolution, replay hashes, and deterministic random streams.

## 2. Battle rewards

Each newly completed playable battle awards Gold exactly once according to its `MatchResult.Outcome`:

| Outcome | Gold | Tuning label |
|---|---:|---|
| Win | 100 | `INITIAL-TUNING` |
| Tie | 75 | `INITIAL-TUNING` |
| Loss | 50 | `INITIAL-TUNING` |

Rewards do not scale with score differential, seed, capital, world position, or battle length. A loss therefore always provides half of the win reward and meaningful forward progress.

Reward calculation reads the completed battle result but does not mutate or participate in battle simulation. Awarding Gold must be an outer-loop operation after the authoritative result exists.

## 3. Battle-run reward identity

A **battle run** is one newly started playable instance with an outer-loop `battleRunId`. The identifier is progression/application metadata and is not an authoritative battle input.

- The first completion of a battle run may award its outcome reward once.
- Reopening its result, reloading or resuming its completed state, and replaying it award no additional Gold.
- Resuming an incomplete battle preserves its `battleRunId` and remains eligible for one award when it completes.
- Replay is read-only and retains the original run identity.
- Same-seed Restart creates a new `battleRunId`. It is a new battle run and may earn Gold even when its seed, offers, commands, and result reproduce an earlier run.
- Same-seed Gold farming is accepted for Day 2 and should be revisited only if playtests show that it distorts progression pacing.

The persistence mechanism must make the award-once rule survive result-screen reopening and application restart.

## 4. Capital structure and upgrades

Each capital contains **four landmarks**. Every landmark has stages `0..3`:

- Stage 0: not upgraded.
- Stage 1: first upgrade complete.
- Stage 2: second upgrade complete.
- Stage 3: landmark complete.

The player may upgrade the current capital's landmarks in any order, but stages within one landmark are sequential and cannot be skipped. An upgrade is allowed only when the landmark is below stage 3 and the player has at least the exact stage cost. One successful operation deducts that cost and advances the landmark by one stage atomically.

Landmark upgrades unlock that landmark's next visual state and its associated presentation/lore milestone. They grant no battle power.

## 5. Initial world sequence and costs

Day 2 supports three ordered prototype capitals:

1. `capital_01`
2. `capital_02`
3. `capital_03`

These stable identifiers establish the implementation sequence. Creative display names, cultures, and visual themes are deferred to visual planning.

Each row below gives the cost of stages 1, 2, and 3 for every landmark in that capital:

| Capital | Stage 1 | Stage 2 | Stage 3 | Total per landmark | Total capital cost |
|---|---:|---:|---:|---:|---:|
| `capital_01` | 50 | 100 | 150 | 300 | 1,200 |
| `capital_02` | 75 | 150 | 225 | 450 | 1,800 |
| `capital_03` | 100 | 200 | 300 | 600 | 2,400 |

All costs are `INITIAL-TUNING`. Later capitals become more expensive through landmark costs, never by adding turns to battles.

At uniform outcomes and before completion bonuses or carried Gold, the gross capital costs correspond to these initial pacing references:

| Capital | Wins | Ties | Losses |
|---|---:|---:|---:|
| `capital_01` | 12 | 16 | 24 |
| `capital_02` | 18 | 24 | 36 |
| `capital_03` | 24 | 32 | 48 |

These are tuning landmarks, not guaranteed completion counts. Mixed outcomes, completion rewards, and carried Gold change actual pacing.

## 6. Capital completion and world unlocking

A capital completes when all four of its landmarks reach stage 3.

The upgrade that completes the final landmark must atomically:

1. Mark the capital complete.
2. Grant **200 Gold** once (`INITIAL-TUNING`).
3. Record the completion reward as claimed.
4. Unlock the next capital when one exists.

`capital_01` is unlocked on a new save. Later capitals begin locked and unlock only in sequence. At most one unlocked capital is incomplete at a time. Completed capitals remain available for review, but cannot be upgraded again or grant another completion reward. Completing `capital_03` marks the initial world sequence complete and still grants its one-time 200 Gold reward.

Capital completion unlocks the next capital plus cosmetic recognition such as a completion badge or title. Exact cosmetic content is a later visual/content decision. It does not unlock permanent combat strength.

## 7. Competitive-integrity boundary

Persistent progression must not alter the authoritative result of a same-seed or ranked battle. For Day 2, Gold, landmark stages, capital completion, and world position provide no:

- unit Power, HP, movement, or class advantage;
- starting Growth, Insight, score, structures, units, perks, or Keystone state;
- card-strength, cost, offer-count, hand-quality, or legality advantage;
- extra turns, rerolls, actions, targeting options, or deterministic RNG influence.

Progression unlocks are limited to capital-board presentation, lore milestones, cosmetic recognition, and access to the next capital board. Any future proposal for battle-affecting progression requires a separate product decision and explicit ranked/same-seed fairness review.

## 8. Minimum persistent state

The progression save must preserve enough information to restore these product facts:

- save schema version;
- Gold balance;
- current capital identifier;
- unlocked and completed state for each capital;
- stage `0..3` for each of the four landmarks in each capital;
- whether each capital-completion reward has been claimed;
- battle-run identity and durable reward-claimed information sufficient to enforce one Gold award per run.

The storage shape and serialization technology are implementation decisions for D2P04. They must not place progression fields in authoritative `RunState` or change existing battle hashes.

## 9. Reset and replay expectations

- Day 2 has no prestige, seasonal reset, partial reset, or player-facing progression reset.
- An explicit development-only full reset may return Gold and capital progression to the new-save state and clear battle reward-claim records.
- Replay verification remains deterministic and read-only; it never pays rewards.
- Same-seed Restart remains a fresh reward-eligible battle run as defined in §3.

## 10. Deferred tuning and content

The following remain open for later playtesting or visual/content work and do not block implementation of this contract:

- final Gold rewards, landmark costs, completion bonus, and target pacing;
- economy changes motivated by same-seed farming behavior;
- capital display names, cultures, themes, landmark identities, lore, badges, and titles;
- additional capitals beyond the initial three;
- monetization, purchasable Gold, timed systems, LiveOps, prestige, seasons, and backend synchronization.

