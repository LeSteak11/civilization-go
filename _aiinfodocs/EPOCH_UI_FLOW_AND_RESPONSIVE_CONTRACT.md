# EPOCH UI Flow and Responsive Contract

**Status:** Authoritative Day 2 wireframe-level contract for screen responsibilities, navigation, and portrait responsiveness.

**Scope of authority:** Presentation flow and layout behavior. `EPOCH_PRODUCT_DIRECTION.md` governs the outer loop, `EPOCH_CAPITAL_PROGRESSION_CONTRACT.md` governs progression and reward eligibility, and `EPOCH_Core_Gameplay_Spec_v1.md` governs battle behavior.

This document defines structure and behavior only. It does not specify finished styling, final copy, animation timing, final artwork, or implementation architecture.

## 1. Screen model

EPOCH uses three full-screen destinations and two Capital overlays:

- **Capital/Home:** persistent hub and the only place a new reward-eligible battle begins.
- **Battle:** the existing 3×5 battle board and its turn interaction.
- **Result & Rewards:** combined battle outcome and already-credited reward presentation.
- **Landmark Upgrade modal:** focused upgrade and capital-completion presentation over Capital.
- **World Progression sheet:** orientation and capital selection/revisiting from Capital.

The upgrade modal and World sheet deliberately preserve Capital as the outer-loop anchor rather than creating additional destinations.

## 2. Navigation contract

```text
Launch with no incomplete battle
  → Capital/Home

Capital/Home
  → Battle CTA → new reward-eligible Battle
  → Landmark → Landmark Upgrade modal → Capital/Home
  → World → World Progression sheet → selected unlocked Capital/Home

Battle completes
  → eligible run: credit Gold once → Result & Rewards
  → practice run: no Gold → Result & Rewards (Practice state)

Result & Rewards
  → Continue to Capital → Capital/Home
  → Replay → read-only Battle replay → same Result & Rewards state
  → Copy Seed → remain on Result & Rewards
  → Restart Same Seed → practice Battle → Result & Rewards (Practice state)

Final landmark upgrade
  → Capital Complete state in Landmark Upgrade modal
  → unlock and auto-focus next capital → Capital/Home
  → final capital: show World Complete state → completed final Capital/Home
```

If the application resumes with an incomplete battle, it returns to Battle with the same battle-run identity. If it resumes an already rewarded result, it returns to Result & Rewards without crediting Gold again. Replay, result reopening, and practice runs never become reward-eligible through navigation.

A genuinely new reward-eligible battle starts only through the Capital/Home Battle CTA. There is no reward-eligible “Battle Again” action on Result & Rewards.

## 3. Screen responsibilities

### 3.1 Capital/Home

**Purpose:** Show persistent progress, provide the next outer-loop choices, and anchor navigation.

**Primary action:** Start a new reward-eligible battle. The sticky bottom CTA remains available when the player has unspent Gold; spending Gold is never required before battling.

**Essential information:**

- current capital identity and completion progress;
- persistent Gold balance;
- five landmarks and each landmark's stage `0..5` state;
- clear affordance and affordability state for upgradeable landmarks;
- World Progression entry;
- Battle CTA state and whether it starts a new eligible battle.

**Transitions:**

- Battle CTA starts a new eligible Battle.
- Selecting a landmark opens the Landmark Upgrade modal.
- World opens the World Progression sheet.
- Returning from Result & Rewards refreshes Gold and progression state without requiring a claim action.
- Capital remains usable with banked Gold and with affordable upgrades left unpurchased.

### 3.2 Battle

**Purpose:** Present and control the deterministic 24-turn battle board.

**Primary action:** Choose one legal offered card and, when required, its legal lane target.

**Essential information:**

- turn and Age;
- player and Snapshot score;
- Growth and Insight for both sides, named explicitly as Growth and Insight;
- three five-tile lanes, units, structures, modifiers, ownership, and combat feedback;
- three shared offers, costs, legality, and target/cancel state;
- forced PASS feedback and presentation-speed/skip state where applicable.

Persistent Gold does not appear in Battle chrome. Battle UI must never use “Gold” as a label or synonym for Growth or Insight.

Seed information is player-accessible but secondary. It does not need to occupy the primary battle header; the Result & Rewards screen is the primary seed-view/copy location.

Battle completion transitions directly to Result & Rewards after the authoritative result exists and any eligible Gold award has been recorded. Pausing or closing during an incomplete battle preserves/resumes that battle rather than starting a replacement.

### 3.3 Result & Rewards

**Purpose:** Confirm the outcome, explain the already-finalized Gold consequence, and return the player to the outer loop.

**Primary action:** **Continue to Capital**, presented as the single dominant sticky action.

**Essential information:**

- Victory, Tie, or Defeat;
- final player and Snapshot scores;
- reward state;
- seed value;
- clear distinction between eligible and practice results.

For an eligible first completion, Gold is credited before the screen is presented. Reward motion and copy communicate `+300`, `+250`, or `+200 Gold` and may show the updated balance, but are not a claim interaction. A reopened or resumed rewarded result says the reward was already credited and does not animate or apply another award.

For a same-seed practice completion, the same screen shell uses a persistent, visually distinct **Practice — no Gold** state. It must not imply that Gold is pending, missed, or claimable.

**Secondary actions:**

- **Replay:** Read-only; plays the recorded battle and returns to the same result state without awarding Gold.
- **Copy Seed:** Copies the visible seed, confirms success non-modally, and does not navigate.
- **Restart Same Seed — Practice, no Gold:** Starts a practice battle and must state its no-Gold consequence before activation without competing visually with Continue.

Secondary actions are grouped below or behind the result details hierarchy. Replay and Copy Seed must not resemble primary progression actions. Restart Same Seed must never be presented as a normal reward-eligible rematch.

### 3.4 Landmark Upgrade modal

**Purpose:** Let the player inspect and optionally buy the next sequential stage of one landmark without leaving Capital.

**Primary action:** Upgrade the selected landmark when affordable.

**Essential information:**

- landmark identity and current stage out of five;
- next stage and its Gold cost;
- persistent Gold balance and post-purchase balance preview;
- affordable/unaffordable state;
- close/back action that preserves banked Gold.

Upgrade success advances one stage and refreshes the modal and Capital behind it. The player may close the modal after any upgrade or without upgrading.

When the twenty-fifth upgrade completes a capital, the modal changes to a Capital Complete state. It presents the one-time completion Gold already applied, the newly unlocked capital, and a continue action. Continuing closes the modal and automatically focuses the newly unlocked capital on Capital/Home. Completing the final prototype capital instead presents World Complete and returns focus to that completed capital.

### 3.5 World Progression sheet

**Purpose:** Orient the player within the ordered world sequence and allow revisiting unlocked capitals.

**Primary action:** Select an unlocked capital or dismiss back to the currently focused Capital.

**Essential information:**

- all three capitals in sequence;
- locked, current/in-progress, and completed states;
- completion progress for the active capital;
- clear indication that completed capitals are revisitable and locked capitals are not selectable.

The sheet is not required to advance after completion: the newly unlocked capital is auto-focused by the completion flow. The sheet exists for orientation and revisiting completed capitals. Selecting a completed capital returns to its read-only completed Capital view; the one incomplete unlocked capital remains the place where upgrades can be purchased.

## 4. Responsive layout contract

### 4.1 Reference canvas and safe areas

- Primary reference viewport: **390 × 844 logical points**, portrait.
- Supported portrait behavior must cover shorter **9:16** phones and taller contemporary phones.
- Backgrounds and nonessential scene art may extend full-bleed.
- Essential text, interactive controls, modal content, and sheet content remain inside the platform safe-area rectangle.
- Sticky bottom actions sit above the bottom safe-area inset; top navigation and status content sit below the top inset.
- Left/right content gutters are 16 points at the reference width and may reduce only when necessary to preserve minimum controls on narrower devices.
- Interactive controls use at least a 48-point touch target. Primary sticky CTAs target a 56-point control height before safe-area padding.
- Portrait is the Day 2 contract. Landscape, tablets, and foldable-specific layouts are deferred.

The safe-area rectangle, not the raw screen bounds, is the layout container for functional content. No device notch, rounded corner, camera cutout, or home indicator may cover a required action or essential value.

### 4.2 Short 9:16 behavior

Short phones are treated as height-constrained layouts, not as uniformly scaled-down 390×844 screens.

Compression order is:

1. reduce decorative top/bottom spacing and nonessential scene exposure;
2. tighten gaps between content groups;
3. shorten or collapse secondary descriptions and decorative labels;
4. make noncritical detail regions vertically scrollable;
5. move secondary actions into a clearly labeled overflow/details region if necessary.

Do not reduce primary actions below their touch target, hide required values, or cover them with safe-area insets.

Screen-specific rules:

- **Capital/Home:** Keep the Battle CTA sticky and visible. The capital scene/landmark region may crop or scroll vertically; persistent Gold, focused-capital progress, World entry, and Battle CTA remain directly reachable.
- **Battle:** Keep the board, current offers, turn/Age, scores, and Growth/Insight visible without scrolling the main turn surface. Compress header/footer spacing and card description detail before shrinking targets. Optional inspection content may use overlays.
- **Result & Rewards:** Keep Continue to Capital sticky. Outcome, score, and reward/practice state remain above it; secondary actions and extended details may scroll.
- **Landmark Upgrade modal:** Fit within the safe-area height. The body may scroll, while close/back and the upgrade action remain fixed and visible.
- **World Progression sheet:** Expand toward a near-full-height sheet when needed. Its capital list scrolls; dismissal and the selected/current state remain clear.

### 4.3 Tall-phone behavior

Tall phones do not enlarge every control or spread actions beyond comfortable reach. Width governs readable line length and control size; extra height is assigned in this order:

1. capital scene or battle-board breathing room;
2. result celebration/reward presentation;
3. separation between information groups;
4. optional secondary detail.

Sticky Continue to Capital and the Capital Battle CTA remain anchored above the bottom safe area. Core controls retain their reference sizing and maximum readable widths rather than stretching edge to edge.

### 4.4 Flex, scroll, and sticky priorities

| Surface | Fixed/protected | Flexible | Scrollable |
|---|---|---|---|
| Capital/Home | Safe-area navigation, Gold, progress summary, sticky Battle CTA | Capital scene exposure and spacing | Landmark/scene region when height-constrained |
| Battle | Turn/Age, scores, Growth/Insight, board, offers and legal input | Header/footer gaps, board spacing, card detail | No main-surface scroll; optional inspection overlays only |
| Result & Rewards | Outcome, reward/practice state, sticky Continue | Celebration and group spacing | Secondary actions/details on short screens |
| Upgrade modal | Close/back and upgrade action | Preview art and explanatory copy | Modal body |
| World sheet | Sheet handle/header and clear dismissal | Sheet height | Capital sequence list |

Avoid nested scrolling. A modal or sheet owns the active scroll while open, and the Capital surface behind it does not move.

## 5. Current prototype assumptions

### Retain

- 390×844 portrait as the primary reference.
- The battle board remains three lanes by five tiles with offers aligned beneath the tactical field.
- One primary decision per turn, legal/illegal offer states, lane targeting/cancellation, and forced PASS feedback.
- Growth and Insight remain explicit battle resources.
- Turn/Age, both scores, board state, event feedback, 1×/4× speed, skip, help, replay, seed, and same-seed restart remain valid capabilities.
- Presentation reads Application/Core state and events; it does not duplicate gameplay rules or alter authoritative state.
- Skipping presentation and replaying a battle do not change the result.

### Battle-prototype-only; do not use as finished-flow guidance

- Launching directly into a hard-coded battle rather than Capital/Home.
- Treating the single procedural Unity scene or one full-screen overlay as the required future screen architecture.
- Showing the seed prominently in the battle header.
- A result overlay with no Gold consequence or Continue to Capital action.
- Presenting Restart Same Seed as the dominant result action or as a reward-eligible run.
- Exposing replay verification or telemetry language as primary player-facing result content.
- Fixed normalized vertical bands that ignore device safe areas or assume all portrait phones share the 390×844 proportions.
- Any legacy status-bar token, gem, store, monetization, or persistent Gold chrome inside Battle.

## 6. Validation checklist

This contract is ready for implementation planning only when documentation review confirms:

- every full-screen destination, modal, sheet, and transition has one clear responsibility;
- a new eligible battle can start only from Capital/Home;
- eligible completion credits Gold once before Result & Rewards appears;
- replay, reopened/rewarded results, and practice restart results cannot credit Gold;
- Continue to Capital and the Capital Battle CTA remain protected on short 9:16 screens;
- the Battle surface never labels Growth or Insight as Gold and shows no persistent Gold chrome;
- all essential controls remain inside safe areas with at least 48-point targets;
- the Battle interaction does not require main-surface scrolling;
- tall-phone surplus height changes spacing/presentation rather than navigation hierarchy;
- completed capitals remain revisitable and completion auto-focuses the next unlocked capital;
- no finished visual treatment or production implementation is specified here.

