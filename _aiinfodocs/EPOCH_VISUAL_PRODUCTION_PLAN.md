# EPOCH Visual Production Plan

**Status:** Authoritative Day 2 visual-deliverables scope and production order.

**Inputs:** `EPOCH_PRODUCT_DIRECTION.md`, `EPOCH_CAPITAL_PROGRESSION_CONTRACT.md`, and `EPOCH_UI_FLOW_AND_RESPONSIVE_CONTRACT.md`.

This plan prepares visual and UI production. It does not approve finished screen art, generate assets, or authorize UI implementation. Every implementation group begins only after its PM checkpoint is approved.

## 1. Existing package classification

### Existing files

| Existing item | Classification | Day 2 disposition |
|---|---|---|
| `Assets/EPOCH_Visuals/01_Board/board_carved_sandstone.png` | **Keep** | Keep as a concept/style anchor only. Its welcoming carved-tabletop treatment, broad shapes, three-column/five-row read, and transparent surround remain useful references. It is not a production export: its 941×1672 9:16 composition fixes River/Highland/Coast to specific columns instead of supporting seeded lane-modifier assignment, and it has not been validated inside the responsive UI. |
| `Assets/EPOCH_Visuals/00_ArtDirection/DecisionLog.md` | **Keep** | Keep as the future visual approval log. It is currently empty and confers no approvals. |
| `Assets/EPOCH_Visuals/01_Board/IMPLEMENTATION.md` | **Pause** | Keep empty until the responsive battle shell and modular board construction are approved. Do not write an implementation brief around the current monolithic concept image. |
| `Assets/EPOCH_Visuals/# EPOCH Visual Deliverables.md` | **Pause / superseded** | Preserve as the original battle-first inventory. This plan replaces its production order and adds the missing Capital, World, reward, responsive, and shared-foundation work. |
| `Assets/EPOCH_Visuals/Visual-Folder-System.txt` | **Keep, revised** | Retain the folder guide but organize new work under the four authoritative families below. Existing numbered folders may remain until assets are migrated during approved implementation groups. |

### Existing checklist sections that remain valid

These requirements remain useful, but final production waits for the ordered groups in §4:

- visual identity studies: color, material, shape, lighting, ornament, icon, UI surface, miniature, structure, effects, phone-scale consistency, style guide, implementation brief, and decision log;
- battle board lanes, center objectives, lane dividers/frame, player/opponent edges, and approval captures;
- Sword/Spear/Horse identity and owner/opponent differentiation;
- movement, attack, damaged, defeated, and REACH feedback;
- card frames and states for BUILD, TRAIN, ADVANCE, and KEYSTONE;
- Growth and Insight icons/tokens and gain/spend feedback;
- movement/combat feedback supported by the existing battle rules;
- Age, advancement, Keystone, ownership, score, battle HUD, card area, result outcome, buttons, panels, and final readability/performance reviews.

### Existing checklist items paused or replaced

| Item | Disposition |
|---|---|
| Clean/final monolithic board PNG and final lane art | Pause until a modular board system proves seeded modifier reassignment and responsive cropping. |
| Final illustration for every card | Pause bulk production. Validate the card system with a representative set before committing to the full content pool. |
| Selected-unit state | Pause; units are not a primary direct-selection action in the approved battle flow. |
| Retreating-unit state | Pause; the authoritative battle has no retreat command. |
| Shield-impact effect | Pause unless later mapped to an approved, visible battle event. |
| Structure footprint guide and structure upgrade variants | Replace. In-match structures are lane-associated, do not occupy tiles, and do not upgrade. Use lane association and readable structure-state guidance instead. |
| Card-tray open/closed states | Pause; no tray expansion interaction is approved. |
| Pause, Settings, and Loading finished presentations | Pause until they enter an approved minimum flow. |
| Future board themes | Pause until the first modular battle board and all required outer-loop surfaces validate. |
| Final global polish | Pause until every preceding group has an approved in-game integration checkpoint. |

## 2. Four visual families

### A. Shared UI foundations

**Keep/continue:** the existing style prefix and proposed color/material/shape/icon/UI-surface studies as exploratory inputs.

**New required deliverables:**

- 390×844 reference grid plus short 9:16 and tall-phone variants;
- safe-area and sticky-action specifications;
- typography scale and minimum-size rules;
- primary, secondary, disabled, selected, and pressed control states;
- standard surface, modal, sheet, tooltip/status, and overlay anatomy;
- spacing, gutters, scroll, and maximum-width tokens;
- Growth, Insight, and persistent Gold icon/name separation rules;
- navigation shell and wireframes for every approved destination/overlay at all three height classes;
- accessibility checks for contrast, shape-plus-color encoding, and touch targets;
- shared placeholder component inventory and approval-capture format.

### B. Battle-board family

**Keep/continue:** the current board concept as reference; 3×5 topology; lane/objective, unit, structure, card, Growth/Insight, feedback, Age, Keystone, ownership, score, HUD, and offer-area requirements.

**New or revised deliverables:**

- modular lane surfaces so River, Highland, and Coast can appear in any seeded lane;
- board frame/objective/entry pieces separated from modifier art;
- responsive battle wireframes that keep the board and offers visible without main-surface scrolling;
- explicit no-persistent-Gold battle HUD rule;
- secondary seed access rather than a dominant seed header;
- lane-associated structure treatment without tile footprints or upgrade variants;
- representative phone-scale test set for cards, units, structures, ownership, and effects before bulk production;
- practice/replay state indicators that do not alter battle presentation state or simulation.

### C. Capital-board family

This family is entirely new.

**New required deliverables:**

- Capital/Home composition with sticky Battle CTA and persistent Gold;
- capital scene behavior at reference, short, and tall heights;
- five landmark positions and selection/affordability/complete states;
- shared visual language for landmark stages `0..5`;
- Landmark Upgrade modal, Gold cost/balance treatment, and bank/close behavior;
- upgrade and capital-completion feedback;
- next-capital auto-focus and final World Complete states;
- completed-capital read-only treatment;
- placeholder identity slots for three capitals pending approved names, cultures, themes, landmarks, lore, badges, and titles.

Bulk production of unique landmark stages remains paused until capital identities and the shared stage language are approved. The stage system must prove readability before producing three capitals × five landmarks × six visual states.

### D. World progression and rewards family

This family expands the old Match Results section and adds the missing world flow.

**New required deliverables:**

- combined Result & Rewards structure for Victory, Tie, and Defeat;
- already-credited Gold presentation for `+300`, `+250`, and `+200`;
- persistent **Practice — no Gold** result state;
- dominant Continue to Capital action and subordinate Replay, Copy Seed, and Restart Same Seed — Practice actions;
- returned/reopened “already credited” state without another reward animation;
- capital-completion reward treatments for 250, 350, and 500 Gold;
- World Progression sheet with locked, current, incomplete, and completed states;
- ordered three-capital orientation and completed-capital revisit behavior;
- short-screen scrolling and tall-screen spacing variants for Result and World surfaces.

## 3. Production order

Production follows this dependency order:

1. **Contract translation:** convert approved safe-area, responsive, navigation, hierarchy, and terminology rules into measurable wireframe constraints.
2. **Shared foundations:** approve typography, spacing, control hierarchy, surfaces, icon separation, touch targets, and safe-area behavior.
3. **Multi-aspect wireframes:** validate all destinations, modal/sheet behavior, sticky CTAs, and transitions at 390×844, short 9:16, and tall-phone heights.
4. **Functional placeholder implementation:** connect approved non-visual state/actions without finished art.
5. **Family visual systems:** approve Capital, World/reward, and Battle visual systems with small representative asset sets.
6. **Bounded asset production:** create remaining approved assets only after their system survives phone-scale integration.
7. **Final integration and polish:** integrate approved final art, then perform consistency, readability, motion, and performance review.

Finished screen art must not begin before steps 1–3 pass their PM checkpoint. Bulk asset generation must not begin before a representative implementation proves scale, state coverage, and responsiveness.

## 4. UI implementation groups and PM checkpoints

### Group 1 — Shared responsive foundation and navigation/wireframe shell

**Scope:** Safe-area container, reference/short/tall breakpoints, shared spacing/type/control/surface primitives, and placeholder navigation among Capital, Battle, Result & Rewards, Upgrade modal, and World sheet.

**PM checkpoint:** Review all destinations at 390×844, short 9:16, and a tall-phone height. Confirm sticky Continue and Battle CTAs, modal/sheet behavior, information hierarchy, Growth/Insight/Gold separation, and that no finished screen art has been introduced.

### Group 2 — Capital/Home and Landmark Upgrade modal

**Scope:** Bind Capital/Home and Upgrade modal to the approved UI-facing progression state/actions using placeholders; support banked Gold, five landmarks × five stages, affordability, completion, and next-capital auto-focus.

**PM checkpoint:** Complete the Capital → upgrade/bank → Battle-entry and twenty-fifth-upgrade completion walkthrough at all height classes. Approve capital-stage readability before unique landmark art production.

### Group 3 — Result & Rewards, replay, seed, and practice flow

**Scope:** Bind eligible, already-credited, and practice results; Continue hierarchy; read-only Replay; Copy Seed; and practice-only same-seed restart.

**PM checkpoint:** Verify 300/250/200 eligible outcomes, no duplicate rewards, visually distinct Practice — no Gold, and secondary actions that do not compete with Continue.

### Group 4 — World Progression sheet and capital-completion transition

**Scope:** Bind the three-capital ordered sheet, locked/current/completed states, completed-capital revisits, completion rewards, auto-focus, and final World Complete state.

**PM checkpoint:** Walk capital 1 → 2 → 3 unlocks and revisits with placeholder content; approve orientation and sheet responsiveness before world/capital final art.

### Group 5 — Battle UI adaptation

**Scope:** Adapt the existing playable battle to the shared responsive shell, safe areas, secondary seed treatment, modular board presentation, and transition boundaries. Preserve the existing deterministic 24-turn battle.

**PM checkpoint:** Complete one eligible battle and one practice/replay path at all height classes. Confirm no Gold chrome mid-battle and no changes to authoritative result or replay hash.

### Group 6 — Approved final-art integration and bounded polish

**Scope:** Integrate only approved family assets and complete phone-scale consistency, transitions, effects, motion, edge cleanup, and performance work.

**PM checkpoint:** Approve each family in-context before final cross-screen consistency and release-readiness review.

## 5. First implementation recommendation

**Recommend Group 1 first.** It removes the highest rework risks—safe areas, aspect ratios, navigation hierarchy, shared controls, and overlay behavior—without committing to finished art. Group 1 requires separate PM acceptance before implementation begins.

