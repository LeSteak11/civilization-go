# EPOCH Capital Progression Contract

**Status:** Authoritative Day 2 contract for outer-loop progression.

**Tuning status:** Every economy amount and pacing figure in this document is **INITIAL-TUNING**, intended for implementation and playtesting rather than final balance. Final economy tuning may change later; **reward eligibility rules in section 3 are decided**, not deferred.

**Scope of authority:** Persistent Gold, battle rewards, landmark progression, capital completion, world unlocking, progression fairness, and minimum save/reward behavior. `EPOCH_PRODUCT_DIRECTION.md` remains authoritative for the outer product loop, and `EPOCH_Core_Gameplay_Spec_v1.md` remains authoritative for battle simulation.

## 1. Currency boundary

**Gold** is the persistent outer-loop currency.

- A new progression save starts with 0 Gold.
- Gold is account-wide, non-negative, and uncapped for the Day 2 prototype.
- Gold is earned from completed battles and capital-completion rewards.
- Gold is spent only on landmark upgrades during Day 2.
- Gold is not Growth or Insight. Growth and Insight exist only inside a battle and never carry into progression.
- Gold and all other progression state remain outside authoritative battle state, battle resolution, replay hashes, and deterministic random streams.
- Persistent **Gold** must stay distinct in UI and copy from in-match yields. Battle simulation must not use "gold" as a synonym for Growth.

## 2. Battle rewards

Each newly completed **eligible** playable battle awards Gold exactly once according to its `MatchResult.Outcome`:

| Outcome | Gold | Tuning label |
|---|---:|---|
| Win (Victory) | 300 | `INITIAL-TUNING` |
| Tie | 250 | `INITIAL-TUNING` |
| Loss (Defeat) | 200 | `INITIAL-TUNING` |

Rewards do not scale with score differential, seed, capital, world position, or battle length. A defeat therefore always provides meaningful forward progress.

Reward calculation reads the completed battle result but does not mutate or participate in battle simulation. Awarding Gold must be an outer-loop operation after the authoritative result exists.

## 3. Battle-run reward identity

A **battle run** is one newly started playable instance with an outer-loop `battleRunId`. The identifier is progression/application metadata and is not an authoritative battle input.

- The first eligible completion of a **new** battle may award its outcome reward once.
- Reopening its result, reloading or resuming an already rewarded completed state, and watching a replay award no additional Gold.
- Resuming an incomplete battle preserves its `battleRunId` and remains eligible for one award when it completes, if that run has not already been rewarded.
- Replay is read-only and retains the original run identity; it never pays rewards.
- Using **Restart Same Seed** from the result flow creates a **practice run**. That practice run awards **no** persistent Gold, even if the seed, offers, commands, and result reproduce an earlier run.
- A genuinely new battle (not a same-seed practice restart from the result flow) remains eligible for rewards under the one-award rule.

The persistence mechanism must make the award-once rule survive result-screen reopening and application restart.

## 4. Capital structure and upgrades

Each capital contains **five landmarks**. Every landmark has stages `0..5`:

- Stage 0: not upgraded.
- Stages 1 through 5: sequential upgrades; stage 5 means the landmark is complete.

**Twenty-five** total upgrades are required to complete a capital (5 landmarks × 5 stages).

The player may upgrade the current capital's landmarks in any order, but stages within one landmark are sequential and cannot be skipped. An upgrade is allowed only when the landmark is below stage 5 and the player has at least the exact stage cost. One successful operation deducts that cost and advances the landmark by one stage atomically.

Landmark upgrades unlock that landmark's next visual state and its associated presentation/lore milestone. They grant no battle power.

## 5. Initial world sequence and costs

Day 2 supports three ordered prototype capitals:

1. `capital_01`
2. `capital_02`
3. `capital_03`

These stable identifiers establish the implementation sequence. Creative display names, cultures, and visual themes are deferred to visual planning.

Each row below gives the cost of stages 1 through 5 for every landmark in that capital, plus completion reward:

| Capital | Stage costs (1→5) | Total per landmark | Total capital cost | Completion reward |
|---|---|---:|---:|---:|
| `capital_01` | 20 / 30 / 40 / 50 / 60 | 200 | 1,000 | 250 |
| `capital_02` | 30 / 50 / 70 / 90 / 110 | 350 | 1,750 | 350 |
| `capital_03` | 50 / 75 / 100 / 125 / 150 | 500 | 2,500 | 500 |

All amounts are `INITIAL-TUNING`. Later capitals become more expensive through landmark costs, never by adding turns to battles.

At uniform outcomes and before completion bonuses or carried Gold, the gross capital costs correspond to these initial pacing references (battles to cover total capital cost):

| Capital | Wins (/300) | Ties (/250) | Losses (/200) |
|---|---:|---:|---:|
| `capital_01` | ≈3.3 | 4 | 5 |
| `capital_02` | ≈5.8 | 7 | ≈8.8 |
| `capital_03` | ≈8.3 | 10 | 12.5 |

These are tuning landmarks, not guaranteed completion counts. Mixed outcomes, completion rewards, and carried Gold change actual pacing.

## 6. Capital completion and world unlocking

A capital completes when all five of its landmarks reach stage 5 (twenty-five upgrades total).

The upgrade that completes the final landmark must atomically:

1. Mark the capital complete.
2. Grant that capital's completion reward once (`INITIAL-TUNING`: 250 / 350 / 500 for `capital_01` / `capital_02` / `capital_03`).
3. Record the completion reward as claimed.
4. Unlock the next capital when one exists.

`capital_01` is unlocked on a new save. Later capitals begin locked and unlock only in sequence. At most one unlocked capital is incomplete at a time. Completed capitals remain available for review, but cannot be upgraded again or grant another completion reward. Completing `capital_03` marks the initial world sequence complete and still grants its one-time completion reward.

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
- stage `0..5` for each of the five landmarks in each capital;
- whether each capital-completion reward has been claimed;
- battle-run identity and durable reward-claimed information sufficient to enforce one Gold award per eligible run, including distinguishing practice (same-seed restart) runs that never award Gold.

The storage shape and serialization technology are implementation decisions for D2P04. They must not place progression fields in authoritative `RunState` or change existing battle hashes.

## 9. Reset and replay expectations

- Day 2 has no prestige, seasonal reset, partial reset, or player-facing progression reset.
- An explicit development-only full reset may return Gold and capital progression to the new-save state and clear battle reward-claim records.
- Replay verification remains deterministic and read-only; it never pays rewards.
- **Restart Same Seed** from the result flow is a practice run and never pays persistent Gold, as defined in section 3.
- A genuinely new battle remains reward-eligible under §2–§3.

## 10. Deferred tuning and content

The following remain open for later playtesting or visual/content work and do not block implementation of this contract:

- final Gold rewards, landmark costs, completion bonuses, and target pacing (amounts above remain `INITIAL-TUNING`);
- capital display names, cultures, themes, landmark identities, lore, badges, and titles;
- additional capitals beyond the initial three;
- monetization, purchasable Gold, timed systems, LiveOps, prestige, seasons, and backend synchronization.

**Not deferred:** reward eligibility for eligible new battles, practice-only same-seed restarts, one-award-per-run, and replay/reopen/resume non-payment. Those rules are decided in section 3 and section 9.
