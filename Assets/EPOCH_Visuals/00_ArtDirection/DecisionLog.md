# EPOCH Visual Decision Log

## 2026-09-10 — Group 2 palette lock (Stage-0 DNA + board)

**Decision:** Lock named palette roles for Capital landmark stage language 0→5 and later Gold icon / modal chrome.

**Sources:** Stage-0 MJ DNA pick (bottom-right: navy top + gold geometric band on light carved ring); `board_carved_sandstone.png`; UX tabletop MJ prefix; PM navy+gold Capital language.

**Locked roles**
| Role | Hex | Layer |
|------|-----|-------|
| NavyWire | `#0F2D46` | Capital chrome |
| NavyMid | `#193C55` | Capital chrome |
| GoldWire | `#C4A35A` | Capital chrome (thin inlays only) |
| GoldWireDeep | `#8E6B2A` | Capital chrome |
| SandBase | `#EBD4B3` | Prop fill |
| SandRing | `#CD9E8C` | Prop fill |
| OchreWarm | `#E8A969` | Prop fill |
| TurquoiseAccent | `#30C3C3` | Prop fill |
| TurquoiseDeep | `#297673` | Prop fill |
| CoralAccent | `#DE7846` | Prop fill |
| TerracottaAccent | `#BD7439` | Prop fill |
| MatteShadow | `#0A0A0F` | Shared |
| GoldCurrency | `#E0B84A` | Currency icon only (≠ GoldWire) |

**Rules**
- Restrained GoldWire edges on stage props; no yellow-metal gold paint flooding landmarks.
- GoldCurrency never on stage props or Battle chrome.
- Art Producer is the single Midjourney/ChatGPT prompt stream to the user; Game Art Director feeds style lock only.
- DNA gold band currently ~`#B4826E` bronze; ChatGPT refine pushes thin inlays toward GoldWire `#C4A35A`.

**Artifacts:** `EPOCH_G2_PALETTE_LOCK.md`, `EPOCH_G2_STYLE_GUIDE.md`, `EPOCH_G2_palette_swatch.png` in this folder.

**Next:** Consistency-check ChatGPT 0–5 ladder sheet at ~72pt / five-across; then log each approved final (`landmark_stage_0`…`_5`, `icon_gold`, modal chrome).

**Approved by:** Game Art Director (measurement) + Art Producer (adopted) + PM (welcomed). User edit pass open if hexes feel off.

## 2026-09-10 — Group 2 stage language finals consistency

**Files:** `Assets/EPOCH_Visuals/Capital/stage_language/landmark_stage_0.png` … `landmark_stage_5.png` (362×659 each)

**Call:** PASS-WITH-NOTES (non-blocking). Gold icon track may proceed.

**Pass**
- Shared base DNA across 0–5; matte miniature; no unique culture read
- Painted sheet numerals removed from finals
- Mass progression present; phone ~72px row and five-across scannable
- No GoldCurrency flood (bright yellow-gold negligible)
- Canvas sizes consistent

**Notes (non-blocking)**
- 0→1 still subtle at ~72pt (Art noted) — optional stage-1 bump only if playtest fails empty-vs-started read
- 3→4 remains largest silhouette jump (stacked discs → taller tower) — shared base keeps ladder identity
- GoldWire inlays read bronze-sand (~`#989078`) not locked `#C4A35A` — restrained, acceptable

**Required stage-1 bump:** No


## 2026-09-10 — Stage ladder user-final

User confirmed `landmark_stage_0.png` … `landmark_stage_5.png` are **final**. No ChatGPT should-fix pass; do not overwrite. PASS-WITH-NOTES notes remain historical. Next: Gold icon.

## 2026-09-10 — Gold icon PASS

**File:** `Assets/EPOCH_Visuals/02_UI/Currency/icon_gold.png`
**Call:** PASS. Thick matte puck, 4-point sparkle stamp, NavyWire rim, GoldCurrency family (~`#E8B838`). ≠ Growth/Insight. Cleared to write.

**Written:** `icon_gold.png` saved on Dojo (2026-09-10). Next Group 2 art: upgrade/complete modal chrome.


## 2026-09-10 — Gold icon user override

User locked revised Gold (flatter HUD + `#E0B84A` nudge) and overwrote `Assets/EPOCH_Visuals/02_UI/Currency/icon_gold.png`. This supersedes prior Art Director PASS crop. Next Group 2 art: upgrade/complete modal chrome.

## 2026-09-10 — Modal chrome process exception

MJ upgrade-modal batch FAIL (ornamental carved panels / face motif — not functional phone UI). GAD PASS: stop MJ as primary for system chrome; use ChatGPT flat mobile UI kit (or Unity 9-slice) on Group 2 palette lock. No Assets write from that MJ grid.

## 2026-09-10 — Upgrade modal chrome PASS

ChatGPT flat UI kit PASS. Write `Assets/EPOCH_Visuals/02_UI/Modal/upgrade_modal_chrome.png`. System panel; NavyWire/NavyMid; GoldWire thin edges; SandBase/Ochre CTA; layout DNA complete.
