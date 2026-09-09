# EPOCH / Civilization-Go — Project Manager Handoff to Grok Bot

**Handoff date:** September 9, 2026  
**Project:** Civilization-Go, internally titled EPOCH  
**Repository:** `C:\Users\jakeb\2026 Projects\_games\civilization-go`  
**Current initiative:** Day 2 — prepare the project for grouped UI implementation

---

## 1. Purpose of this handoff

Grok Bot is taking over the day-to-day Project Manager role for EPOCH from a prior ChatGPT PM conversation. This document contains the context needed to continue that conversation without restarting discovery or reopening decisions that have already been made.

The owner communicates with the implementation developer in a separate ChatGPT Codex chat by copying and pasting messages between chats. Grok PM should produce clear developer-ready messages for the owner to paste into that chat.

The immediate job is to finish the existing Day 2 plan. Day 2 does **not** include finished UI implementation. Its finish line is a stable, approved foundation that allows UI and visual work to begin one group at a time without avoidable rework.

---

## 2. Product summary

EPOCH is a portrait mobile strategy game inspired by Civilization. Its existing playable core is a deterministic, approximately three-minute, 24-turn asynchronous strategy match.

During a battle:

- The board contains three lanes with five tiles each.
- The player chooses one of three offered cards each turn.
- BUILD creates economy, TRAIN deploys units, and ADVANCE grants perks.
- Growth and Insight are temporary resources used only inside the match.
- Units move and fight automatically after the player's choice.
- The match spans four Ages: Dawn, Bronze, Steel, and Modern.
- The opponent is a deterministic snapshot using the same seed/card sequence.
- Matches support replay, checkpoint/resume, and same-seed restart.

The current Unity project is at M4: a technically playable portrait battle presentation. The owner considers the current visuals poor and has been developing a visual deliverables list with an AI creative team.

The major new product direction is to wrap the tactical match in a satisfying persistent board-completion loop similar in cadence to Monopoly Go, while remaining unmistakably civilization-themed.

The authoritative outer loop is:

> **Capital → Battle → Rewards → Landmark upgrades → Capital completion → Next capital**

Terminology must remain consistent:

- **Battle board:** the existing 3×5 tactical match.
- **Capital board:** the persistent city-building/progression scene.
- **World progression:** the ordered sequence of completed and upcoming capital boards.

The 24-turn battle remains short and structurally unchanged. Later capital boards become longer through higher persistent progression costs, not through longer matches.

---

## 3. Core product principles already decided

Treat these as approved unless the owner explicitly changes them:

1. Preserve the deterministic 24-turn battle.
2. Add a persistent capital-building layer around the battle rather than replacing it.
3. Persistent progression uses **Gold**. Growth and Insight remain match-only currencies.
4. Every completed eligible battle provides useful Gold progress, including defeats.
5. Persistent upgrades should primarily provide visible construction, collection, cosmetic recognition, world progression, and later lateral options.
6. Persistent progression must not create uncontrolled raw-power advantages in ranked or same-seed competition.
7. The primary portrait reference canvas is **390 × 844**, approximately 9:19.5—not 9:16.
8. The UI must also adapt safely to shorter 9:16 displays and taller modern phones.
9. Fix product flow and layout foundations before producing or integrating large batches of finished UI assets.
10. Visual assets will be produced and implemented in approved groups, with a review stop after each group.

---

## 4. Existing project authority and important documents

Grok PM should ask the developer to respect the repository's documented authority hierarchy. The following documents are especially important:

- `README.md` — repository status, authority order, and operating overview.
- `_aiinfodocs/DAY_2_UI_READINESS_PLAN.md` — authoritative plan for the current Day 2 initiative.
- `_aiinfodocs/EPOCH_PRODUCT_DIRECTION.md` — created during D2P01 to lock the outer loop and terminology.
- `_aiinfodocs/EPOCH_Core_Gameplay_Spec_v1.md` — authoritative deterministic battle rules.
- `_aiinfodocs/data/epoch_v1_content.json` — authoritative prototype battle content.
- `_aiinfodocs/EPOCH-master-reference.md` — older broad product reference; still useful where not superseded, but legacy where newer authoritative documents conflict.
- `_aiinfodocs/EPOCH_V1_Technical_Implementation_Plan.md` — older M0–M4 technical plan; legacy where superseded.
- `Assets/EPOCH_Visuals/# EPOCH Visual Deliverables.md` — current creative deliverables list. It was designed mainly around the battle screen and will need rebaselining in D2P05.

Do not assume every older monetization, commander, store, or UI statement remains current. Newer Day 2 decisions supersede older concepts where they conflict.

---

## 5. Day 2 objective and phases

The full authoritative phase plan is in `_aiinfodocs/DAY_2_UI_READINESS_PLAN.md`. The summary is:

### D2P01 — Lock the Day 2 product direction

Status: **Completed and accepted.**

Outcome:

- Established the new outer loop and vocabulary.
- Confirmed the deterministic 24-turn battle remains unchanged.
- Added the product-direction document.
- Updated the README authority hierarchy.
- Clarified that older broad plans are legacy where superseded.
- Documentation only; no Unity or gameplay testing was needed.

### D2P02 — Define the capital-progression contract

Status: **Implemented incorrectly and awaiting a focused correction. Do not treat as complete yet.**

The developer initially documented an outdated 4-landmark × 3-stage structure and left same-seed farming unresolved. The PM rejected that completion because it did not match the approved direction.

The owner has been given a correction message to send to the developer. Grok PM's first responsibility is to confirm whether that correction was sent and obtain the developer's corrected completion report.

### D2P03 — Lock the screen flow and responsive layout contract

Status: **Not started.**

This phase defines the capital/home, battle, results/rewards, landmark completion, and world progression screens at wireframe/contract level. It also settles responsive behavior for 390 × 844, 9:16, and taller phones. No finished UI should be implemented in this phase.

### D2P04 — Prepare the non-visual game foundation

Status: **Not started.**

This is the primary implementation phase. It adds persistent progression state and operations, connects battle completion to rewards, supports landmark/capital advancement, and exposes clean UI-facing state and actions. Persistent meta progression must remain outside the authoritative deterministic match simulation.

### D2P05 — Rebaseline visual production and certify UI readiness

Status: **Not started.**

This phase updates the visual deliverables plan, distinguishes battle/capital/world/shared assets, establishes UI implementation groups, performs one proportionate readiness check, and produces the final UI-readiness handoff.

---

## 6. Approved D2P02 progression contract

The following values and rules are the approved initial prototype contract. They are initial tuning targets, not final balance.

### Persistent currency

- Currency name: **Gold**.
- Gold is persistent, account-wide, non-negative, and uncapped for the prototype.
- Gold is entirely separate from match-only Growth and Insight.

### Battle rewards

- Victory: **300 Gold**.
- Tie: **250 Gold**.
- Defeat: **200 Gold**.
- Do not add score-differential scaling yet.

### Capital structure

- Each capital contains **five landmarks**.
- Each landmark contains **five sequential upgrade stages**.
- A capital therefore contains **25 total upgrade beats**.
- Players may upgrade landmarks in any order, but each individual landmark advances sequentially.
- A capital completes when all five landmarks reach stage five.

### Prototype world scope

- Three ordered capital boards.
- Use stable generic IDs initially.
- Capital names, cultures, themes, and finished visual identities remain deferred to creative work.

### Upgrade costs

Use the same stage costs for each of the five landmarks within a capital.

#### Capital 1

- Stage costs: **20, 30, 40, 50, 60 Gold**.
- Total per landmark: **200 Gold**.
- Total capital cost: **1,000 Gold**.
- Completion reward: **250 Gold**.

#### Capital 2

- Stage costs: **30, 50, 70, 90, 110 Gold**.
- Total per landmark: **350 Gold**.
- Total capital cost: **1,750 Gold**.
- Completion reward: **350 Gold**.

#### Capital 3

- Stage costs: **50, 75, 100, 125, 150 Gold**.
- Total per landmark: **500 Gold**.
- Total capital cost: **2,500 Gold**.
- Completion reward: **500 Gold**.

These costs intentionally make early progression fast and later capitals progressively longer without changing battle duration.

### Rewards, unlocks, and fairness

- Landmark upgrades provide visible construction and presentation/lore milestones.
- Capital completion unlocks the next capital and provides cosmetic recognition.
- Do not add permanent battle-stat, starting-resource, card-strength, offer-count, or RNG advantages.
- Any future gameplay unlocks require separate approval and should be lateral rather than vertical.

### Reward eligibility and farming prevention

- Each eligible new battle completion awards Gold once.
- Reopening results awards nothing additional.
- Replaying a match awards nothing additional.
- Resuming an already rewarded completion awards nothing additional.
- Selecting **Restart Same Seed** from the result flow creates a practice run.
- A same-seed practice restart awards no additional persistent Gold.
- A genuinely new battle remains eligible for rewards.
- Reward eligibility is decided now; it is not deferred as a future response to farming.

### Persistence

The minimum saved progression information must include:

- Save schema version.
- Gold balance.
- Current and unlocked capital identifiers.
- Landmark upgrade stages.
- Capital completion states.
- Claimed completion rewards.
- Enough reward-completion identity to prevent duplicate grants after reopen/resume.

There is no player-facing prestige or progression reset in Day 2. An explicit development reset path may exist.

---

## 7. Exact current blocker and next action

The most recent developer completion report claimed D2P02 was complete, but it still documented:

- Four landmarks × three stages instead of five × five.
- The earlier, slower capital structure.
- Same-seed restart eligibility instead of practice-only no-reward behavior.
- A deferred response to farming even though the PM had resolved it.

The prior PM instructed the developer to make a documentation-only correction using the approved values in Section 6 of this handoff, without starting D2P03.

### Grok PM should do next

1. Ask the owner whether the D2P02 correction message has been sent to the developer.
2. If it has not, prepare a concise developer message using Section 6.
3. If it has, wait for the corrected developer report.
4. Verify that the report explicitly confirms:
   - Five landmarks × five stages.
   - The exact Capital 1–3 cost tables and completion rewards.
   - Battle rewards of 300/250/200.
   - Same-seed restarts are practice-only and grant no additional Gold.
   - Documentation only; no production code changes.
5. If correct, accept D2P02 and request the developer's D2P03 plan only. Do not authorize D2P03 execution until its plan is reviewed and approved.

No manual Unity testing or automated code suite is necessary for the D2P02 documentation correction. Arithmetic and cross-document consistency checks are sufficient.

---

## 8. Developer workflow and approval protocol

The implementation developer is ChatGPT Codex using **GPT-5.6 Sol for the entire Day 2 effort**. Recommended reasoning effort:

- D2P01: Medium.
- D2P02: High.
- D2P03: High.
- D2P04: High.
- D2P05: Medium.

Keep all phases in the same developer chat so it retains context.

The developer must follow this cycle:

1. Propose a concise plan for exactly one phase.
2. Identify genuine product decisions requiring PM direction.
3. Stop and wait for approval.
4. Execute only the approved phase.
5. Perform targeted validation proportional to the change.
6. Stop and provide:
   - A concise PM update explaining what changed, assumptions, affected areas, and open items.
   - One short Git commit title only.

The owner handles commits and GitHub pushes. The developer should not commit or push.

The developer should not:

- Execute multiple phases together.
- Begin finished UI or final art integration during Day 2.
- Expand into monetization, backend services, speculative architecture, or unrelated cleanup.
- Perform exhaustive audits or repeatedly run broad test suites.
- Add safeguards for hypothetical problems without a concrete requirement.
- Change deterministic battle rules without explicit approval.

Grok PM should review whether a proposed validation plan is proportionate. Documentation phases need documentation checks; D2P04 needs focused automated coverage of the new progression behavior and the boundary with the existing battle, not indiscriminate testing.

---

## 9. Grok specialist team and when to use it

The owner created three Grok Bots in addition to the Grok PM.

### Progression Designer

Use during:

- D2P02 before approving significant economy changes.
- D2P04 only when implementation exposes an unresolved progression rule.
- Later reward/capital-screen tuning.

Do not ask it to redesign the tactical battle or create a full final economy model.

### UX & Visual Director

Use during:

- D2P03 before approving the screen-flow and responsive-layout contract.
- D2P05 to rebaseline the visual deliverables and define UI implementation groups.
- Later UI implementation reviews.

Do not ask it to create final art before the relevant group is approved.

### Scope & QA Reviewer

Use:

- At the end of D2P02–D2P05.
- To identify material omissions, contradictions, or scope drift.
- To recommend the minimum proportionate validation.

Do not let it turn every phase into an exhaustive audit. It advises; Grok PM makes the acceptance recommendation and the owner provides final approval.

### Collaboration rule

Use specialists at phase boundaries, not continuously. One relevant specialist should review a phase; the Scope & QA Reviewer may check completion. Avoid group-chat churn and duplicate analysis.

---

## 10. UI and creative-production context

The owner initially planned to have an AI creative team produce all items in `Assets/EPOCH_Visuals/# EPOCH Visual Deliverables.md`, followed by one large developer integration. That strategy has been changed.

New strategy:

- Produce assets in coherent groups.
- Implement one group at a time.
- Review the in-game result at each stop.
- Correct scale, readability, or style problems before producing dependent groups.

The current deliverables list mainly covers:

- Visual identity.
- Battle board and objectives.
- Units and unit states.
- Structures.
- Cards.
- Match resources.
- Combat and movement feedback.
- Age progression.
- Advancement.
- Lane ownership and score.
- Top HUD and card tray.
- Future battle-board themes.

It does not yet adequately cover:

- Capital/home board.
- Five landmarks with five upgrade stages.
- Landmark affordability and upgrade feedback.
- Capital completion celebration.
- Persistent Gold presentation.
- Battle reward count-up and return-to-capital flow.
- World progression/capital selection.
- Locked, current, and completed capital states.

D2P05 must correct that gap.

Do not discard valid battle art direction. The carved-sandstone battle board, unit language, card style, objective language, and other battle assets may remain useful. However, final HUD, result flow, global navigation, phone-readability passes, and future themes should wait until the screen-flow and responsive contracts are settled.

---

## 11. Aspect-ratio decision

The project is not intended to be locked to 9:16.

- Primary design reference: **390 × 844 portrait**.
- Approximate ratio: **9:19.5**.
- Short-screen compatibility target: **9:16**.
- Tall-phone compatibility: contemporary tall Android/iPhone displays.

D2P03 should define safe areas, anchored regions, flexible spacing, and allowed reflow at the contract level. It should not implement finished UI. The battle board should not simply be stretched to fill every screen.

---

## 12. PM decision style

The owner wants efficient progress and has meaningful but limited AI usage. Grok PM should:

- Lead with a recommendation.
- Keep developer messages precise and directly pasteable.
- Make reasonable low-risk decisions instead of generating endless options.
- Separate decisions needed now from tuning that can wait for playtesting.
- Stop rabbit holes early.
- Avoid asking for tests the owner can perform more efficiently unless developer-side verification is materially safer.
- Reject phase work that contradicts explicit approval, as happened in the first D2P02 attempt.
- Avoid praising activity; evaluate outcomes against the phase stop condition.
- Keep a short decision log so later agents do not reopen settled questions.

When the owner pastes a developer PM update, Grok PM should respond with:

1. Whether the phase is accepted, needs correction, or needs clarification.
2. Whether the owner should manually test anything and exactly what.
3. A paste-ready developer reply.
4. Any specialist Bot that should be consulted before the next approval.

---

## 13. Scope explicitly outside Day 2

Unless the owner expands scope, do not pursue:

- Finished UI implementation.
- Final visual asset generation or bulk art integration.
- Monetization implementation.
- Gacha or commander implementation.
- Live backend, account services, commerce, or production analytics.
- Final economy balance.
- Full campaign narrative or historical capital naming.
- Social features, battle pass, events, or store work.
- Broad refactoring of the deterministic battle engine.
- Final device certification or exhaustive regression testing.

These may be referenced as future considerations only when necessary to avoid a foundational conflict.

---

## 14. Definition of Day 2 complete

Day 2 is complete when:

- The capital-building outer loop is authoritative and consistently named.
- Gold, rewards, five-by-five landmark progression, capital completion, persistence, reward eligibility, and world unlocking have an approved contract.
- Required screens and navigation are specified.
- Responsive portrait behavior is defined for 390 × 844, 9:16, and taller phones.
- The non-visual application/data layer supports the progression loop and exposes clean UI-facing state/actions.
- Persistent meta progression remains outside the deterministic match simulation.
- The creative deliverables plan reflects battle, capital, world, and shared UI needs.
- The first UI implementation group is clearly defined and can begin without inventing missing product decisions.

---

## 15. First message Grok PM should send the owner

Suggested response after reading this handoff:

> I have the EPOCH PM handoff. The immediate issue is that D2P02 was first documented with the rejected 4×3 progression structure. Before moving to D2P03, I need to confirm whether you sent the focused correction requiring 5×5 landmarks, the locked cost tables, 300/250/200 battle rewards, and practice-only same-seed restarts. If you have, paste the developer's corrected completion report when it arrives. If you have not, I will give you the exact message to send.

