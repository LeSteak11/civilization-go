# EPOCH Day 2 UI-Readiness Handoff

**Status:** D2P05 readiness summary. UI implementation has not started.

## Authoritative documents

1. `EPOCH_PRODUCT_DIRECTION.md` — Capital → Battle → Rewards → Landmark upgrades → Capital completion → Next capital; board vocabulary and fixed 24-turn battle boundary.
2. `EPOCH_CAPITAL_PROGRESSION_CONTRACT.md` — Gold, eligible/practice rewards, five-by-five landmark progression, three-capital costs/completion rewards, fairness, save/reset behavior.
3. `EPOCH_UI_FLOW_AND_RESPONSIVE_CONTRACT.md` — screen responsibilities, navigation, Result & Rewards actions, 390×844 reference, short 9:16/tall behavior, safe areas, flex/scroll/sticky priorities.
4. `EPOCH_VISUAL_PRODUCTION_PLAN.md` — keep/pause/new classification, four visual families, production order, and UI implementation checkpoints.
5. `EPOCH_Core_Gameplay_Spec_v1.md` plus `data/epoch_v1_content.json` — unchanged authoritative battle rules and prototype battle content.

The repository `README.md` lists authority by subject. The Master Reference and V1 Technical Implementation Plan remain legacy rationale where newer subject-specific documents are silent.

## Confirmed Day 2 scope

- The battle board is the existing deterministic 3×5, 24-turn match.
- The capital board is persistent progression; world progression is the ordered capital sequence.
- Eligible battles award 300/250/200 Gold once; result restart is practice-only and awards no Gold.
- Every capital has five landmarks with five upgrade stages and the approved three-capital cost/reward curve.
- Progression is cosmetic/presentation-only relative to battle power and remains outside authoritative simulation.
- Capital/Home, Battle, Result & Rewards, Upgrade modal, and World sheet have defined responsibilities and transitions.
- 390×844, short 9:16, tall-phone, safe-area, sticky-action, flex, and scroll behavior are defined.
- The non-visual Application progression module exposes persistent state, battle/reward wrapping, upgrade/unlock operations, save/load/reset support, and UI-neutral projections/actions.

Finished UI, final art integration, monetization, stores/gacha, accounts, backend/live services, broad refactors, and battle-rule changes remain outside Day 2.

## Visual deliverable status

- **Keep:** carved-sandstone board as concept reference; art-direction checklist requirements that still map to approved battle behavior; decision-log and revised family folder structure.
- **Pause:** monolithic/final board export, bulk card art, unsupported unit/structure/card-tray states, future themes, and global polish until foundations and representative integrations pass.
- **New:** shared responsive foundation; Capital and five-stage landmark system; persistent Gold treatment; combined reward/practice result states; World sheet; modular seeded-lane board; and multi-aspect approval wireframes.

The complete item-level classification is in `EPOCH_VISUAL_PRODUCTION_PLAN.md`.

## Remaining tuning and content

- Gold rewards, landmark costs, completion rewards, and pacing remain `INITIAL-TUNING`; reward eligibility is locked, not deferred.
- Capital display names, cultures, themes, landmark identities, lore, badges, and titles remain open.
- Exact cosmetic rewards and final visual identity specifications remain open.
- Representative asset sets must prove phone-scale readability before bulk production.
- Physical-device/OS-floor certification, final motion timing, and final performance budgets remain later validation items.

## Known wiring gaps before UI implementation

- Unity's `EpochMatchController` still launches and controls `PlayableMatchSession` directly; it does not yet use `EpochGameSession` or the progression projections/actions.
- The Unity scene still launches directly into Battle and uses the M4 result overlay. Capital/Home, Result & Rewards, Upgrade modal, and World sheet do not exist in Unity.
- `FileProgressionStore` exists, but Unity has not yet supplied a platform save path or lifecycle wiring.
- The current result overlay has no automatic Gold presentation or Continue to Capital, and its same-seed restart bypasses practice classification because the outer wrapper is not connected.
- Copy Seed has no Unity clipboard action.
- The current Canvas scales from 390×844 but does not apply platform safe areas or the approved short/tall reflow rules.
- The current board concept fixes modifier art to columns; production needs modular lane assignment driven by the existing seeded battle state.
- Capital and landmark identifiers are generic; creative identities and visual states are not authored.

These are expected UI-group tasks, not blockers in the approved non-visual contracts.

## Recommended implementation order for PM acceptance

1. **Shared responsive foundation and navigation/wireframe shell.**
2. **Capital/Home and Landmark Upgrade modal.**
3. **Result & Rewards, replay, seed, and practice flow.**
4. **World Progression sheet and capital-completion transition.**
5. **Battle UI adaptation.**
6. **Approved final-art integration and bounded polish.**

Each group has the explicit PM checkpoint defined in `EPOCH_VISUAL_PRODUCTION_PLAN.md`.

## First UI group

**Recommend Group 1: Shared responsive foundation and navigation/wireframe shell.**

Its goal is to validate safe areas, aspect-ratio behavior, shared primitives, sticky primary actions, overlays, and placeholder navigation before any finished screen art. Starting Group 1 requires explicit PM acceptance of this ordering and recommendation; D2P05 does not start it.

