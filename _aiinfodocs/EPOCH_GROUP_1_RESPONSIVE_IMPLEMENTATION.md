# EPOCH Group 1 Responsive Shell Implementation

**Status:** Group 1 implementation checkpoint  
**Authority:** Implements `EPOCH_UI_FLOW_AND_RESPONSIVE_CONTRACT.md`; it does not replace product or progression authority.

## Shared layout foundation

- Functional UI is parented to the platform safe-area rectangle; only the background is full-bleed.
- The safe root contains three ordered layers: destination, protected sticky action, and overlay.
- Reference scaling is 390 × 844 logical points with 16-point side gutters, 48-point minimum controls, and a 56-point primary action inside a 76-point sticky region.
- Typography, spacing, panels, primary/secondary/quiet buttons, disabled/selected/pressed states, scroll regions, dimmer, modal, and sheet are shared procedural wireframe primitives.
- The shell distinguishes Growth and Insight as battle resources and keeps persistent Gold out of Battle chrome.

## Responsive grids

| Profile | Validation viewport | Simulated safe area | Layout behavior |
|---|---:|---:|---|
| Reference | 390 × 844 | x 0, y 20, w 390, h 804 | Baseline spacing and composition |
| Short 9:16 | 360 × 640 | x 0, y 24, w 360, h 596 | Reduced nonessential spacing; near-full-height overlays; sticky actions remain protected |
| Tall phone | 430 × 1000 | x 0, y 44, w 430, h 922 | Control sizes remain stable; surplus height becomes breathing room |

Capital/Home protects Gold, progress, World entry, and the sticky Battle CTA. Result & Rewards protects outcome/reward state and the sticky Continue action while keeping Replay, Copy Seed, and practice restart subordinate. The Upgrade modal scrolls its body while keeping fixed actions. The World sheet owns its capital-list scroll and dismissal while open.

## Placeholder navigation and data boundary

- Destinations: Capital/Home → hosted Battle → Result & Rewards → Capital/Home.
- Overlays from Capital/Home: Landmark Upgrade modal and World Progression sheet.
- `EpochGameSession` supplies Capital, Upgrade, World, and Result projections and owns battle start, completion rewards, and Continue-to-Capital behavior.
- The existing battle presentation is mounted only after `EpochGameSession.StartNewBattle`; its selections pass through the wrapper. Battle layout adaptation remains Group 5.
- Placeholder Result preview explicitly applies no reward. Group 3 owns player-facing Replay, Copy Seed, and practice restart bindings.

## Approval captures and checks

The local capture pack is generated at `Logs/Group1Approval` with Capital, Battle host, Result & Rewards, Upgrade, and World images for all three profiles (15 images total). The editor smoke checks profile selection, safe-area application, the 56-point sticky-action contract, destination/modal/sheet transitions, and mounting the existing battle through `EpochGameSession`.

This group contains wireframes only. Finished art, capital polish, complete result actions, world behavior, and responsive battle adaptation remain in their approved later groups.
