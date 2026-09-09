# EPOCH — Day 2: UI Readiness Plan

## Objective

Take the project from its current M4 playable battle prototype to a stable, clearly specified foundation for grouped UI and visual implementation.

Day 2 ends when the product flow, persistent capital progression, screen responsibilities, portrait layout targets, non-visual application contracts, and visual-production order are aligned well enough that UI work can begin without likely rework.

## Working rules

- Complete one phase at a time and stop for approval before beginning the next phase.
- Before executing a phase, briefly propose the phase plan and wait for approval.
- Preserve the existing deterministic 24-turn battle unless an approved requirement explicitly changes it.
- Do not begin finished UI implementation, final art integration, broad visual polish, monetization, backend services, or unrelated feature work during this plan.
- Prefer the smallest change that establishes the required product contract.
- Use targeted checks proportional to the change. Do not repeatedly rerun broad suites, add speculative safeguards, or spend time validating unchanged systems.
- When a phase ends, provide:
  1. A concise PM update stating what changed, key decisions or assumptions, and anything still open.
  2. One short Git commit title that can be copied directly.

---

## D2P01 — Lock the Day 2 product direction

### Goal

Make the new outer game loop authoritative and remove ambiguity between the tactical battle board and the persistent progression board.

### Required outcomes

- Document the intended player loop:
  **Capital → Battle → Rewards → Landmark upgrades → Capital completion → Next capital.**
- Establish the following vocabulary consistently:
  - **Battle board:** the existing 3×5 tactical match.
  - **Capital board:** the persistent city-building/progression scene.
  - **World progression:** the ordered sequence of capital boards.
- Confirm that the existing 24-turn battle remains the repeatable play activity and does not become longer as world progression advances.
- Identify older project statements that conflict with or omit this direction, and update only the documents necessary to prevent future implementation confusion.
- Record what is explicitly outside Day 2 scope.

### Stop condition

The project has one unambiguous, concise product-flow definition that later phases can treat as authoritative.

---

## D2P02 — Define the capital-progression contract

### Goal

Specify the minimum complete progression system the UI and non-visual game layers must support.

### Required outcomes

- Define persistent Gold separately from in-match Growth and Insight.
- Define how completed battles award Gold, including how losses still provide meaningful progress.
- Define the capital structure: number of landmarks, upgrade stages per landmark, completion requirement, and completion reward.
- Define the initial world sequence at a practical launch/prototype scope, including how later capitals become more expensive without lengthening battles.
- Define what landmark and capital progression unlocks.
- Protect competitive integrity: permanent progression must not create uncontrolled raw-power advantages in same-seed or ranked battles.
- Establish initial tuning targets sufficient for implementation and later playtesting; avoid attempting final economy balance today.
- Define the minimum persistent save data and any reset/replay expectations relevant to progression.

### Stop condition

The progression loop can be implemented without inventing product rules, while all economy numbers remain clearly marked as initial tuning rather than final balance.

---

## D2P03 — Lock the screen flow and responsive layout contract

### Goal

Define what the UI must present before any finished UI assets are built or integrated.

### Required outcomes

- Define the minimum screen set and navigation flow:
  - Capital/home
  - Battle
  - Battle result and rewards
  - Landmark upgrade/completion presentation
  - World progression/capital selection
- Describe the purpose, primary action, essential information, and transitions for each screen at wireframe-level detail.
- Adopt **390 × 844 portrait** as the primary reference canvas.
- Define support expectations for shorter **9:16** displays and taller contemporary phones, including safe areas and what may flex or reflow.
- Resolve how the existing replay, seed, restart, and match-result actions fit into the new post-battle reward flow.
- Identify which current UI assumptions remain valid and which should not guide implementation.
- Do not create finished visual assets or implement final UI during this phase.

### Stop condition

Every required screen and transition has a clear responsibility, and the responsive target is settled well enough to prevent aspect-ratio-driven rework.

---

## D2P04 — Prepare the non-visual game foundation

### Goal

Implement the minimum application and data support required for the UI to display and drive the approved loop without embedding progression rules in presentation code.

### Required outcomes

- Add the persistent progression state and operations established in D2P02.
- Connect completed battle outcomes to the approved reward calculation.
- Support landmark upgrades, capital completion, and unlocking the next capital.
- Expose clean UI-facing state/actions for the screens defined in D2P03.
- Preserve deterministic battle behavior and keep persistent meta progression outside the authoritative match simulation.
- Update or add only the focused checks necessary to protect the new progression behavior and unchanged battle boundary.
- Do not implement finished screens, final styling, art integration, monetization, live services, or speculative extensibility.

### Stop condition

The approved outer loop works at the application/data level and is ready to be represented by UI without presentation-layer rule duplication.

---

## D2P05 — Rebaseline visual production and certify UI readiness

### Goal

Align the creative deliverables with the revised product and leave a clean handoff point for group-by-group UI implementation.

### Required outcomes

- Revise the visual-deliverables plan so it distinguishes battle-board assets, capital-board assets, world-progression assets, and shared UI foundations.
- Reorder visual groups so aspect ratio, safe areas, typography, shared surfaces, and wireframe validation precede final screen art.
- Mark assets that remain valid, assets that should pause, and newly required deliverables.
- Define the recommended UI implementation groups and their approval checkpoints.
- Perform one proportionate final readiness check covering the new contracts and the existing battle’s critical path.
- Produce a concise UI-readiness handoff listing authoritative documents, confirmed scope, remaining tuning items, and the first UI implementation group.

### Stop condition

The project is ready to begin UI implementation one approved visual group at a time, with no known foundational product, navigation, aspect-ratio, or progression-contract decisions left unresolved.

---

## Day 2 completion criteria

Day 2 is complete only when:

- The capital-building outer loop is authoritative and consistently named.
- Persistent Gold, rewards, landmarks, capital completion, and world unlocking have an approved initial contract.
- Screen flow and portrait responsiveness are specified.
- Required non-visual progression support exists and is exposed cleanly to presentation.
- The visual-deliverables plan reflects the new product structure.
- The first UI implementation group can begin without the developer or creative team having to invent missing product decisions.
