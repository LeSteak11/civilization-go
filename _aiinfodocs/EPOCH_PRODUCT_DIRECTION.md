# EPOCH Product Direction

**Status:** Authoritative outer-loop direction for Day 2 and later UI work.  
**Scope of authority:** Product flow, board vocabulary, and the boundary between persistent progression and the existing battle. The Core Gameplay Specification remains authoritative for battle simulation behavior.

## Player loop

EPOCH's outer game loop is:

**Capital → Battle → Rewards → Landmark upgrades → Capital completion → Next capital.**

The player develops a persistent capital by repeatedly completing battles, collecting rewards, and applying those rewards to landmark upgrades. Completing the current capital advances world progression and makes the next capital available. Battle remains the repeatable play activity throughout this loop.

## Required vocabulary

- **Battle board:** The existing 3×5 tactical match. It contains three lanes of five tiles and always resolves after 24 turns.
- **Capital board:** The persistent city-building and progression scene where the player views and upgrades landmarks. It is not part of the tactical grid or authoritative match state.
- **World progression:** The ordered sequence of capital boards through which the player advances by completing capitals.

Any capital marker or capital-end tile shown on the battle board is battle presentation only. It is not the capital board, does not have persistent progression behavior, and does not change the match's completion rules.

## Battle boundary

Every battle remains the existing deterministic 24-turn activity and does not become longer as world progression advances. Persistent progression must remain outside the authoritative battle simulation. Any other relationship between progression and battle is deferred until it is separately specified and approved.

Detailed reward, landmark, unlock, and save rules are defined in the authoritative `EPOCH_CAPITAL_PROGRESSION_CONTRACT.md`.

That contract currently locks capital structure at **five landmarks x five sequential stages** (twenty-five upgrades to complete a capital), battle Gold at **300 / 250 / 200** for victory / tie / defeat (`INITIAL-TUNING`), and treats **Restart Same Seed** from the result flow as a **practice run that awards no persistent Gold**. Final economy amounts may still be retuned; reward eligibility is decided.

## Day 2 boundary

Day 2 covers the product and progression contracts, screen responsibilities, responsive-layout targets, minimum non-visual progression support, and visual-production sequencing needed before UI implementation.

Day 2 does not include finished UI implementation, final art integration, broad visual polish, monetization, gacha or stores, backend or live-service systems, accounts, broad refactors, speculative architecture, unrelated cleanup, or changes to the authoritative battle rules and 24-turn length.
