# EPOCH UI Group Deliverables (Master Checklist)

**Owner:** PM  
**Source:** `EPOCH_VISUAL_PRODUCTION_PLAN.md`, `EPOCH_UI_READINESS_HANDOFF.md`, UX Group 1 brief  
**Rule:** One bullet = one deliverable. Finish a group's required work, then PM pastes Dev to implement that group (plan → execute → stop).  
**Tags:** `[Dev]` Unity/application wiring · `[Art]` visual asset (Midjourney→ChatGPT→Assets final) · `[Doc]` wireframe/spec/capture

Art Producer prompts from `[Art]` items only after UX brief for that group. Group 1 is almost all `[Dev]`/`[Doc]`.

---

## Group 1 — Shared responsive foundation + nav/wireframe shell
**PM checkpoint:** All destinations at 390×844, short 9:16, tall; sticky Continue + Battle; modal/sheet OK; Growth/Insight/Gold separation; no finished screen art.

- [x] `[Dev]` Safe-area root container (top/bottom/side insets)
- [x] `[Doc]` 390×844 reference grid
- [x] `[Doc]` Short 9:16 height variant grid
- [x] `[Doc]` Tall-phone height variant grid
- [x] `[Dev]` Shared typography scale + minimum sizes
- [x] `[Dev]` Shared spacing / gutter tokens
- [x] `[Dev]` Primary sticky CTA control (56pt target)
- [x] `[Dev]` Secondary control style
- [x] `[Dev]` Disabled control state
- [x] `[Dev]` Selected control state
- [x] `[Dev]` Pressed control state
- [x] `[Dev]` Standard panel / surface primitive
- [x] `[Dev]` Modal shell (open/close, body scroll, fixed actions)
- [x] `[Dev]` Bottom sheet shell (handle/dismiss, list scroll)
- [x] `[Dev]` Overlay / dimmer primitive
- [x] `[Dev]` Placeholder Capital/Home screen
- [x] `[Dev]` Placeholder Battle screen host
- [x] `[Dev]` Placeholder Result & Rewards screen
- [x] `[Dev]` Placeholder Landmark Upgrade modal
- [x] `[Dev]` Placeholder World Progression sheet
- [x] `[Dev]` Placeholder navigation among all five destinations/overlays
- [x] `[Dev]` Sticky Battle CTA on Capital (survives short 9:16)
- [x] `[Dev]` Sticky Continue CTA on Result (survives short 9:16)
- [x] `[Dev]` Growth vs Insight vs Gold labeling separation in shell chrome
- [x] `[Dev]` Wire `EpochGameSession` / progression projections enough for placeholder flow
- [x] `[Doc]` Multi-aspect approval capture pack (ref / short / tall)
- [ ] `[Art]` Optional labeled graybox placeholders only if PM/UX explicitly request (otherwise skip)

**After Group 1 done → Dev already implemented this group; PM accepts checkpoint → start Group 2 brief/art as needed.**

---

## Group 2 — Capital/Home + Landmark Upgrade modal
**PM checkpoint:** Capital → upgrade/bank → Battle entry; 25th-upgrade completion; stage readability before unique landmark art.

- [ ] `[Dev]` Bind Capital/Home to progression Gold balance
- [ ] `[Dev]` Bind five landmark slots (stages 0..5)
- [ ] `[Dev]` Landmark affordability / unaffordable states
- [ ] `[Dev]` Banked Gold (Battle CTA available with unspent Gold)
- [ ] `[Dev]` Upgrade modal bind (cost, before/after stage, confirm/cancel)
- [ ] `[Dev]` Capital-complete modal state + completion Gold once
- [ ] `[Dev]` Auto-focus newly unlocked capital
- [ ] `[Dev]` World Complete end-state hook (placeholder OK)
- [ ] `[Art]` Capital/Home layout illustration system (placeholder identity OK)
- [ ] `[Art]` Landmark stage language — stage 0
- [ ] `[Art]` Landmark stage language — stage 1
- [ ] `[Art]` Landmark stage language — stage 2
- [ ] `[Art]` Landmark stage language — stage 3
- [ ] `[Art]` Landmark stage language — stage 4
- [ ] `[Art]` Landmark stage language — stage 5
- [ ] `[Art]` Landmark selected / affordable / complete feedback marks
- [ ] `[Art]` Upgrade modal frame + confirm/cancel controls
- [ ] `[Art]` Persistent Gold icon (distinct from Growth/Insight)
- [ ] `[Art]` Capital completion celebration treatment (placeholder capital IDs OK)

**After Group 2 finals + Dev implement → PM checkpoint → Group 3.**

---

## Group 3 — Result & Rewards, replay, seed, practice
**PM checkpoint:** 300/250/200 once; no duplicate credit; Practice visually distinct; secondaries don’t beat Continue.

- [ ] `[Dev]` Eligible result credits Gold once before screen shows
- [ ] `[Dev]` Already-credited / reopen state (no second award)
- [ ] `[Dev]` Practice — no Gold result state
- [ ] `[Dev]` Sticky Continue to Capital (primary)
- [ ] `[Dev]` Replay (read-only) secondary
- [ ] `[Dev]` Copy Seed secondary + clipboard
- [ ] `[Dev]` Restart Same Seed = practice / no Gold secondary
- [ ] `[Art]` Result & Rewards shell — Victory
- [ ] `[Art]` Result & Rewards shell — Tie
- [ ] `[Art]` Result & Rewards shell — Defeat
- [ ] `[Art]` Already-credited Gold motion/copy treatment (+300 / +250 / +200)
- [ ] `[Art]` Practice — no Gold distinct visual state
- [ ] `[Art]` Continue primary button treatment
- [ ] `[Art]` Replay control treatment
- [ ] `[Art]` Copy Seed control treatment
- [ ] `[Art]` Restart Same Seed — Practice control treatment

**After Group 3 finals + Dev implement → PM checkpoint → Group 4.**

---

## Group 4 — World sheet + capital-completion transition
**PM checkpoint:** Capitals 1→2→3 unlock/revisit with placeholders; sheet responsive before world final art.

- [ ] `[Dev]` World sheet bind — three ordered capitals
- [ ] `[Dev]` Locked capital state
- [ ] `[Dev]` Current / in-progress capital state
- [ ] `[Dev]` Completed capital state
- [ ] `[Dev]` Completed-capital revisit (read-only)
- [ ] `[Dev]` Completion reward grant display (250 / 350 / 500)
- [ ] `[Dev]` Auto-focus next capital after completion
- [ ] `[Dev]` Final World Complete state
- [ ] `[Art]` World sheet chrome / frame
- [ ] `[Art]` Capital node — locked
- [ ] `[Art]` Capital node — current
- [ ] `[Art]` Capital node — completed
- [ ] `[Art]` Capital-completion reward presentation
- [ ] `[Art]` World Complete presentation

**After Group 4 finals + Dev implement → PM checkpoint → Group 5.**

---

## Group 5 — Battle UI adaptation
**PM checkpoint:** One eligible + one practice/replay at all heights; no Gold mid-battle; hashes unchanged.

- [ ] `[Dev]` Fit existing battle into shared responsive shell + safe areas
- [ ] `[Dev]` No persistent Gold chrome in Battle
- [ ] `[Dev]` Secondary seed access (not dominant header)
- [ ] `[Dev]` Transition Battle → Result & Rewards via progression wrapper
- [ ] `[Dev]` Modular lane presentation hooks (seeded River/Highland/Coast)
- [ ] `[Art]` Modular lane surface — River
- [ ] `[Art]` Modular lane surface — Highland
- [ ] `[Art]` Modular lane surface — Coast
- [ ] `[Art]` Board frame / objective / entry pieces (separated from modifiers)
- [ ] `[Art]` Battle HUD chrome (Growth/Insight only)
- [ ] `[Art]` Card tray / offer area chrome
- [ ] `[Art]` Representative unit set proof (Sword / Spear / Horse — owners)
- [ ] `[Art]` Representative structure set proof (lane-associated, no tile footprint)
- [ ] `[Art]` Representative card frame set proof (BUILD / TRAIN / ADVANCE / KEYSTONE)
- [ ] `[Art]` Combat / movement feedback proof set (minimal)

**After Group 5 representative set + Dev implement → PM checkpoint → bulk only if approved → Group 6.**

---

## Group 6 — Final-art integration + bounded polish
**PM checkpoint:** Each family in-context before cross-screen consistency / release-readiness.

- [ ] `[Dev]` Integrate only PM-approved final assets per family
- [ ] `[Art]` Replace Group 2 placeholders with approved capital finals (only after identities locked)
- [ ] `[Art]` Remaining landmark uniqueness beyond shared stage language (only after PM unlock)
- [ ] `[Art]` Remaining battle card/unit bulk (only after Group 5 proof approved)
- [ ] `[Dev]` Transition polish (bounded)
- [ ] `[Dev]` Effects / motion polish (bounded)
- [ ] `[Dev]` Edge cleanup + phone-scale consistency pass
- [ ] `[Dev]` Performance / readability review pass
- [ ] `[Doc]` Decision log entries for each family approval

---

## Operating loop (every group)

1. PM confirms group start.  
2. UX brief (if art involved).  
3. Art Producer: one `[Art]` item at a time → MJ → ChatGPT → final into Assets only.  
4. When group’s required `[Art]` + scope ready, PM pastes Dev: plan → execute that group only.  
5. You review Unity → PM checkpoint → next group.

**Paused until later (do not pull forward):** future board themes; bulk every card; selected/retreat unit states; tray open/closed; store/monetization chrome; 9:16-only layouts.
