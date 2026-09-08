# The Hooked Player Playbook

A consolidated reference on game retention, monetization, the merge genre, ethical design limits, and AI-assisted production. Organized by idea, not by source.

**How to use this with an AI:** paste or upload this file and say — "This is my reference document on game design and monetization. Use it as your primary source when advising me. Ask before contradicting it."

---

## 1. THE NUMBERS THAT SET THE BAR

Retention is the whole game. Every point compounds into lifetime value, community size, and revenue.

### Mobile retention benchmarks

| Metric | Top quartile, actual | Aim for |
|---|---|---|
| Day 1 retention | 26–28% | 50%+ |
| Day 7 retention | ~10–15% | 20%+ |
| Day 30 retention | ~5% | 10%+ |
| Day 1, best hyper-casual FTUE | above 50% | — |
| Day 1, best-in-class casual puzzle | ~45% | — |

Benchmarks vary sharply by genre. Casual puzzle has lower long-term retention than mid-core RPG or strategy. Compare within your category.

### What moves each number

- **Day 1 low?** The problem is your first-time experience and opening hook. Nothing else.
- **Day 7 low?** The core loop isn't habitual; daily engagement mechanics are thin.
- **Day 30 low?** You lack social features, long-term goals, and a live content cadence.

### The single highest-leverage feature

If you build only one retention feature, build a guild/clan system with shared objectives.

- Players in a clan: **2–3x higher Day 30** than solo players.
- Players with a real-life friend in-game: **40% higher Day 7**.
- Communication density inside a clan is one of the strongest predictors of whether a member stays at all.

### Effect sizes worth memorizing

- **Narrative engagement:** +30% Day 30, effect holds across cultures.
- **Consistent LiveOps:** 30–50% higher Day 30; a good seasonal event lifts DAU 20–50% while running.
- **Battle passes:** +20–40% engagement during a season; free-tier players are 3x more likely to convert.
- **Meta-progression:** extends total game lifetime 40–60% in roguelike-shaped games.
- **Dynamic difficulty:** holding players in the flow channel reduces churn 15–25%.
- **Personalized push:** 20–30% higher Day 7, but only when personalized and infrequent.
- **Rewarded video:** correctly placed, can lift IAP revenue up to 6x rather than cannibalizing it.
- **Fair monetization:** 2–3x better long-term retention than pay-to-win, plus higher LTV.

---

## 2. WHY PEOPLE ACTUALLY PLAY

Four mechanisms explain nearly all sustained engagement. Design against all four.

### Self-determination theory: autonomy, competence, relatedness

The most-cited framework in game motivation research. Three psychological needs every game must satisfy:

- **Autonomy** — meaningful choice: your build, your path, your avatar, your reward from a set of three.
- **Competence** — mastery and visible growth.
- **Relatedness** — social connection and belonging.

Your opening minute should deliver on at least one immediately.

The finding that should shape your ethics: adolescents who felt competent, autonomous and socially connected *in their daily lives* were far less likely to develop problematic gaming patterns, even playing the same games for the same hours. Problematic play is usually a symptom of unmet needs elsewhere, not a property of the game. Games that fill a genuine void efficiently become disproportionately attractive to people with that void.

### Dopamine and the reward system

Games trigger reward pathways through a specific trifecta: **anticipation, unpredictability, and immediate feedback**. Dopamine is a motivation-and-anticipation chemical more than a pleasure chemical — the wanting matters more than the getting. The same mechanism drives engagement with music, exercise, and learning. What separates healthy from harmful is frequency, intensity and context, not the mechanism.

### Variable reward schedules

Rewards at unpredictable intervals drive far more repeat behavior than fixed schedules. This is the engine behind loot drops, card packs, gacha pulls, random bonuses. It is also the most abusable mechanic in the medium.

Worked example: a puzzle RPG added a "lucky drop" guaranteeing a rare item every fifth match. Day 7 retention rose 34%. Players began *anticipating* the fifth match, and the anticipation became the habit.

### Flow

Total absorption where time disappears. Requires:

1. Clear goals
2. Immediate feedback
3. Balance of challenge and skill
4. Minimal distraction
5. A sense of control

Below the flow channel players are bored; above it, anxious. For many players — especially anxious ones — flow, not the reward system, is the most valuable thing the game gives them.

The best difficulty adjustments are **invisible: shifts of less than 10% per session.** If a player fails the same challenge three times, quietly weaken the enemy or surface a hint. If they breeze through, ratchet up. A well-known platformer's "assist mode" lets players tune speed and invincibility with no judgment or penalty, on the reasoning that flow is where meaning happens and different players find it in different places.

### Escapism, and the paradox in it

Research on Gen-Z mobile players found:

- **Flow experience and escapism both significantly drive game addiction.**
- **Addiction drives both game loyalty and in-app purchase intention.**
- **Playfulness — playing purely for enjoyment — had a significant NEGATIVE effect on addiction.**
- Game loyalty by itself did not significantly drive purchase intention.

Fun, on its own, does not produce compulsion. Compulsion comes from engineered friction and escape motive.

**The fork in the road:** you can monetize escapism and compulsion, which works and is what most of the market does. Or you can monetize playfulness and craft, which retains through affection rather than dependency. The mechanics overlap heavily; the tuning does not. Decide on purpose, early — it determines your whole economy.

---

## 3. THE FIRST THIRTY SECONDS

The first-time user experience determines Day 1 retention more than anything else. Top hyper-casual games clear 50% Day 1 by keeping tutorials **under 30 seconds** and letting players act immediately.

- **Show, don't tell.** Teach through action, never text. A single finger gesture demonstrating the core verb beats a tutorial screen.
- **Drop them into real gameplay in seconds.** The gold-standard casual example puts players in a live level within seconds, introducing special pieces, blockers and boosters gradually over dozens of levels.
- **Contextual tutorials only.** Explain a system the moment it first becomes relevant. Never front-load.
- **Make them feel like a genius.** Celebrate early wins generously. The first five minutes should feel like competence, not instruction.
- **Defer complexity until they're emotionally invested.** Systems after investment feel like depth; the same systems before it feel like homework.
- **Add a hook, not just a tutorial.** Within the first minute, show the one thing that makes this game unlike every other in its genre — a visual set piece, a narrative twist, a novel mechanic reveal, an emotional beat.

**Opening pattern worth stealing:** instead of a tutorial about tapping attack, open with a real choice — save the village or chase the villain. Both branches teach the same combat. The player gets autonomy and emotional investment in the same thirty seconds the tutorial would have spent on button names. Spectacle and choice first, mechanics second.

---

## 4. THE CORE LOOP AND HABIT ARCHITECTURE

Every successful game is a repeatable sequence of actions performed over and over. The genius is never the loop itself — it's how effortlessly it becomes automatic.

### The four-stage habit cycle

**Trigger → Action → Variable Reward → Investment**

Walk the player through all four stages *every session*. The investment step is the one most games skip: each session should end with something spent, queued, or committed — resources placed, a building started, a character leveling — creating anticipation for the return.

Players who return **21 or more consecutive days** show dramatically higher long-term retention. Your first three weeks of designed experience should be laser-focused on habit formation, not content volume.

### Consistency is a promise

Nothing erodes trust faster than bait-and-switch. If your ads show intense PvP, deliver intense PvP — not a farming sim with optional combat. Players who feel deceived churn immediately and leave reviews that poison your acquisition funnel. Consistency applies across visual style, tone, difficulty, and core gameplay — and to every update. A game that runs smoothly at launch and gets buggy after a patch has broken the same promise.

### Technical stability is retention

Crashes, long loads and frame drops are retention killers, and a crash in the first session is often a permanent loss. Set performance budgets and test on *target* devices — 60fps on a flagship means nothing if your audience plays on mid-range hardware where you run at 20.

---

## 5. PROGRESSION AND META

Progression gives players tangible evidence their time mattered. The most engaging games combine **three or more progression types** — skill-based, narrative, economic, social, meta — into interlocking goals at different timescales.

- **Next five minutes:** a progress bar filling, a merge completing, a level clearing.
- **Next day:** a character leveling, a building finishing, a daily quest set.
- **Next month:** a new region, a new mode, a legendary construction.

### Breadcrumb goals

Players should never wonder what to do next. Use a small, immediately achievable task that reveals the next slightly larger one. That chain is what pulls players deeper; without it they drift and leave.

### Meta-progression makes failure survivable

Why the best roguelikes are hard to put down: even a losing run earns currency that permanently upgrades future runs. This converts failure from frustration into investment — every session moves the player forward regardless of outcome.

Add persistent unlock trees, achievements granting permanent bonuses, cross-session carryover. The most powerful meta systems **compound**: the more you play, the more tools you have, the more strategies open up. Eventually the accumulated progress becomes part of the player's identity, and leaving means abandoning who they've become in your game.

### Separate account progression from character progression

Let players level their account independently of any single character. When they switch playstyles they keep forward momentum instead of starting over.

### Customization is switching cost

Avatar and base customization significantly increases identification, enjoyment and session length — **even when options are limited presets**. Personalization creates ownership; ownership makes leaving feel like abandoning something you built. Offer free and premium options across characters, equipment appearance, base layouts, UI.

### Serve the late game

Late-game players are your whales, evangelists and community leaders. If they run out of content they leave and take their spending and influence with them. Give them rotating challenges — a new dungeon or event every two weeks — plus a prestige system letting max-level players reset for exclusive cosmetics and titles.

---

## 6. NARRATIVE AS A RETENTION ENGINE

The most underused retention tool available, and one of the cheapest for a small team with strong art. Players who engage with narrative show **30% higher Day 30 retention**.

- Stories create curiosity; curiosity creates return visits. When a player closes the game wondering what happens next, you've won half the retention battle.
- You don't need an epic. A rival who taunts you, a mystery that deepens, a world that reacts to your choices — any of these turns a mechanical experience into an emotional one.
- Use cliffhangers, unresolved quests and deliberate narrative gaps as re-engagement mechanisms.
- The strongest pattern pairs region-specific mysteries with content updates: each new area opens plot threads carrying across patches, so story-engaged players have a reason to return for every release. Narrative and exploration become a dual retention engine.

---

## 7. SOCIAL SYSTEMS

Social connection consistently outperforms every other retention lever — in one large study it outweighed even the reward mechanics themselves as a predictor of sustained engagement. Social bonds create an obligation to return that has nothing to do with whether the gameplay is still novel.

The pattern players describe directly: *"I might get bored with the gameplay eventually, but I log in every day because that's where my friends are."* This is why decades-old MMOs hold their player bases against newer games with better graphics and mechanics.

### What to build, in order of value

1. **Guilds or clans with shared objectives**, introduced early. One studio went from 8% to 19% Day 7 within a month of adding guilds with shared goals and guild chat — players said they returned because their guild needed them for wars.
2. **Cooperative modes that genuinely require teamwork**, so the obligation is real.
3. **Leaderboards** celebrating both individual and group achievement.
4. **Lightweight social touches** — chat, gifting, visiting other players' bases — which move the needle far more than their build cost suggests.

---

## 8. ECONOMY DESIGN

A fair economy means progress always feels possible, whether through play or payment. If free players hit a wall only money can clear, they leave — and take the social fabric of your community with them, which then costs you the paying players too.

### The faucet-and-sink frame

Resources flowing in (faucets) should roughly equal resources leaving (sinks). Monitor inflation: if currency accumulates with nothing to spend it on it loses meaning; if sinks drain too aggressively players feel exploited.

### Energy systems, honestly assessed

Players resent them, but they serve two real purposes: they prevent binge-and-burn where a player exhausts your content in a weekend, and they create scheduled return triggers. The fairness rule — free players should always have enough energy for meaningful progress, and paying players buy **convenience, never exclusivity**.

### The pay-to-win trap, in numbers

A free-to-play studio shipped a premium weapon objectively stronger than anything free players could earn. Revenue spiked 300% in week one. By week four, DAU had dropped 45% — free players left because they felt powerless, paying players left because there was no one left to beat. Recovery took six months. They replaced the weapon with a cosmetic skin line: ARPU fell, and both long-term retention and total revenue rose.

---

## 9. HOW GAMES MAKE MONEY

The modern default is **hybrid monetization** — IAP, rewarded ads and subscriptions together, with one carrying the majority. For most casual and puzzle titles, IAP carries it.

### The three legitimate IAP pillars

- **Cosmetics** — visual expression, no gameplay advantage. Safest and most retention-friendly.
- **Convenience** — time-savers that accelerate progression without bypassing it. Largest revenue source in most casual games.
- **Content** — new characters, regions or modes that expand rather than dominate.

Avoid selling power directly. It alienates free players and cheapens the achievements of paying ones.

### Rewarded video: the non-payer engine

The overwhelming majority of casual players will never spend money, no matter what you do. Rewarded video monetizes them anyway, and done right it doesn't cannibalize purchases — it can multiply them.

- **Booster placement:** offered when a player is stuck or out of resources. Watch an ad, get currency.
- **Time-booster placement:** watch an ad to reduce a wait timer.
- **Choice placement:** the highest-engagement pattern — after the ad, let the player pick between three different rewards. Players consistently prefer this because it feels like *they* determine their fate rather than the game doing it to them. Autonomy, monetized honestly.

### Battle passes and subscriptions

- **Introduce late, not at launch.** Typically after a few sessions — one major title waits until level seven. It's an offer for engaged players; offering it to a stranger reads as a shakedown.
- **Free and paid tiers.** The free track gives everyone a taste of the reward cadence; free-tier engagement makes players roughly 3x more likely to convert.
- **Price around $5–10.** Value matters far more than price — the pass should be among the best-value things in the game.
- **Consider premium-currency pricing.** One top merge game sells its pass for premium currency rather than dollars, routing players through the currency shop first.
- **Structure:** fixed rewards you can see coming, earned at a variable pace you control. Fixed destination, variable journey.

### Meta-layer monetization

Simple core mechanics get repetitive. The fix — a major casual-market trend — is monetizing the layer *around* the core loop instead of the loop itself. Three recurring types:

- **Narrative meta** — story chapters unlocked by progress.
- **Light construction and customization meta** — a home, island or base you shape.
- **Collectible meta** — characters, cards or sets to complete.

Meta layers broaden the audience, deepen the experience, lift retention, and monetize more player types both directly and indirectly. For a designer-led team they are also the cheapest place to spend your actual advantage: art and character work.

---

## 10. USER ACQUISITION ECONOMICS

Retention keeps players; acquisition finds them. The numbers here decide whether a game with good retention is also a business.

### The structural trend

Customer acquisition cost has risen roughly **222% over the last decade**, from about $19 to $29 per acquired user across digital categories. Global digital marketing spend was around **$526 billion in 2023**, forecast to reach **$936 billion by 2029**. Acquisition is getting more expensive every year and there is no sign of reversal — which means retention and lifetime value have to rise faster than CPI, or the model breaks.

### Cost per install by platform

| Year | iOS | Android |
|---|---|---|
| 2020 | $3.60 | $2.70 |
| 2021 | $3.80 | $2.90 |
| 2022 | $4.10 | $3.00 |
| 2023 | $4.50 | $3.20 |
| 2024 | $4.70 | $3.40 |

iOS consistently costs 35–40% more per install than Android, and both climb about 5–8% per year. Budget for that escalation rather than this year's number.

### Cost per install by region

| Region | 2024 CPI | Year-over-year change |
|---|---|---|
| North America | $5.28 | +$0.17 |
| EMEA | $1.03 | +$0.05 |
| APAC | $0.93 | +$0.08 |
| Latin America | $0.34 | +$0.04 |

**North American installs cost roughly 15x what Latin American installs cost.** This single fact drives most soft-launch strategy: test in cheap regions where you can buy statistically meaningful cohorts for very little, then scale into expensive ones only once retention and monetization are proven.

### Gaming-specific costs

- **Casual gaming CPI:** approximately **$0.98–$1.00** globally. Casual is one of the cheapest categories to acquire into, which is precisely why it is so crowded.
- **Gaming average CPI (2024):** **$4.83** across all game categories — the average is dragged up hard by mid-core and strategy titles competing for high-LTV players.
- **Simulation games:** the lowest CPI of any game genre at **$0.59** blended, splitting to **$2.23 on iOS** and **$0.63 on Android**.

The gap between casual (~$1) and the all-genre average (~$4.83) is the whole strategic picture: casual games buy cheap installs and must monetize thin margins at volume; mid-core games buy expensive installs and must monetize deeply per player.

### Channel costs

- **Facebook Ads CPI:** around **$4.75** in 2023, down dramatically from **$15.00** in 2021 as the post-ATT tracking shock worked through the market.
- **Instagram Ads CPI:** roughly **$1.75–$4.50** in 2024, up from around $5.50 in 2022 at the high end.

Channel costs move far more violently than category averages. Do not build a model on one channel's current rate.

### For comparison, outside gaming

- **Shopping apps:** $4.74 per install in North America, $1.42 in Latin America. US shopping-app acquisition spend was **$6.6 billion in 2023**.
- **Fintech:** $2.50–$6.00 CPI in 2024. Finance apps averaged $2.33 in 2022 — $4.35 iOS, $2.09 Android, $1.60 in LATAM.

### The number that connects this section to the rest of the document

**Over 70% of users abandon an app after the first day.** Every install you pay for is subject to that. At a $1 casual CPI with 28% Day 1 retention, you are paying roughly **$3.57 for each player who comes back once** — and that is before you account for the ~95% who are gone by Day 30. Improving Day 1 from 28% to 40% cuts your effective cost per retained player by about 30% without buying a single extra install.

That is the whole argument for spending on retention before spending on acquisition: retention work compounds against every dollar of media spend you will ever place, and it never stops paying.

### Non-media costs to budget

- **Influencer marketing platform access:** approximately **$10,000**.
- **Influencer agency retainers:** **$10,000–$18,000 per month**.

These are studio-scale numbers, not solo-dev numbers. For a small team, organic and community-led acquisition is not a fallback — it is the only channel with workable economics.

---

## 11. THE MERGE GENRE

Merge is a subgenre of puzzle — the most profitable mobile category. Players drag identical or similar items together to produce new, improved items, then get rewarded with coins, treasures or characters.

### Why players like it

- **Simple to grasp.** Near-zero learning curve, no mechanics needing lengthy explanation.
- **Instant satisfaction.** Merging creates order out of chaos — the comparison used is the feeling of finally cleaning your room.
- **Legible progression.** The path to the next tier is always visible, so there's always a goal.

### Market shape

The first merge title launched in 2017 and dominated for years. The number of merge games **tripled between 2019 and 2021**, producing strong competition and eventual saturation. The top ten combined generate roughly **$34–38 million per month**, but revenue is extremely concentrated:

| Rank | All-time revenue |
|---|---|
| 1 (2017 leader) | $730M–780M+ |
| 2 | $190M+ |
| 3 | $160M+ |
| 4 | $130M+ |
| 5 | $47M+ |
| 6–10 | $19M–32M |

**Read this before you build one:** the category is still growing in revenue but crowded, and a nine-year-old title still owns most of it. Merge mechanics are easy to copy and hard to differentiate on — differentiation has to come from the meta layer, the art, and the theme, not the merging.

### How merge games actually monetize

Critically: **merge players don't get stuck on hard levels — they get stuck behind session restrictions.** Match-3 sells extra moves and boosters because its friction is difficulty. Merge sells past *time*, and merge players are also more willing to pay for additional content.

Standard feature set:

- Pay to skip wait times — usually the single main monetization point
- Premium currency as the hub of everything purchasable
- Time-limited special deals
- Premium content: new lands, new heroes, new characters
- Rewarded video in booster and time-booster placements, as a secondary stream
- A battle pass introduced mid-game, priced modestly

### The reference implementation

The strongest example in the genre runs a core loop of **mining, merging, building and collecting**, forcing players to balance energy, resource management and inventory. On top sit two meta layers — character collection and narrative — and the whole thing is monetized hybrid: purchases as the primary engine, rewarded video for non-payers, a battle pass for the committed. Premium currency buys almost anything, with skipping timers as the main sink; the best-selling pack among US iOS players is the $9.99 tier, indicating high player lifetime values. Its battle pass appears at level seven, costs roughly $5, and is bought with premium currency rather than cash.

---

## 12. THE DARK-PATTERN LINE

A quantitative analysis of **1,496 games** found manipulative design patterns widespread not only in games generally considered problematic but also in games perceived as benign. A separate study found **93% of free mobile games popular with children under 12** contained at least one potentially manipulative design element. This is a design-quality issue and increasingly a legal one.

### The four exploitation vectors

- **Temporal** — playing by appointment: mechanics forcing daily returns at fixed times or forfeiting progress. Grinding, artificial waits.
- **Monetary** — obscured currency conversion, pay-to-skip artificial frustration, purchases with unclear real-money value.
- **Social** — leveraging friendships as obligation; progress gated on recruiting or pressuring others.
- **Psychological** — loot boxes, endless progression designed never to satisfy, manufactured FOMO.

### The named dark patterns to avoid

- **Pay-to-win** mechanics that manufacture artificial frustration to drive spending
- **FOMO** engineered through limited-time events that punish players who miss them
- **Endless progression** designed never to provide satisfaction or completion
- **Social pressure** mechanics that weaponize friendship to maintain engagement

### Regulatory exposure

Loot boxes have been found to activate psychological patterns closely resembling gambling, with documented effects on young players' decision-making and reward perception. **Belgium and the Netherlands already classify certain loot-box mechanics as illegal gambling**, and regulatory discussion is active elsewhere. If your revenue model depends on randomized paid rewards, you carry jurisdictional risk, not just reputational risk.

### The clinical baseline

Roughly **3–4% of players** develop patterns meeting clinical criteria for gaming disorder. The distinguishing factor is not hours played but *function*: someone playing 20 hours a week while maintaining responsibilities and relationships typically doesn't meet criteria, while someone playing 10 hours at the expense of essential life functions might. Known vulnerability factors include poor impulse control, limited alternative coping strategies, and social isolation. Design decisions land hardest on exactly these players.

### Healthy-design practices that don't cost revenue

- Events accessible at all player levels, with free and premium reward tracks, that never punish players who miss them
- Transparent odds and transparent currency-to-money conversion
- Assist and accessibility modes offered without penalty or judgment
- Completion states — things that can actually be finished
- Session-end moments that feel like a good place to stop, not a cliff

---

## 13. US LEGAL AND REGULATORY

This is the section that turns design opinions into liability. It concerns US law specifically.

### There is no federal definition of gambling

Gambling is prohibited unless a state expressly legislates to permit it. Every rule below varies by state, and a design that is clearly legal in one state may not be in another.

### The three-element test

An activity requires state gambling authorization when **all three** elements are present:

1. **Consideration** — the player risks something of value
2. **Chance** — the outcome is outside the player's control
3. **Prize** — the player can win something of value

**Removing any one element may place the activity outside gambling law.** This is the single most useful structural fact in the section: it means the compliance question is not "is this too much like gambling," it is "which of the three legs have I removed, and can I prove it."

### Skill versus chance — three competing legal standards

- **Predominance test** (most common): the game is skill-based if it falls predominantly toward the skill end of the continuum.
- **Material element test**: if chance can intervene to alter the outcome, even a skill-heavy game may be classified as chance-based.
- **Any chance test** (strictest): if any element of chance affects the outcome, it is a game of chance.

Design to the strictest standard that plausibly applies to you, not the most convenient one.

### Where loot boxes actually stand

US courts have **generally ruled that loot boxes are not illegal gambling if the virtual items lack real-world monetary value.** In one federal case the court found loot box prizes were not "things of value" because the items could not be exchanged for real currency. In another, items with value on secondary trading markets still did not qualify as things of value, because those trades violated the game's terms of use.

Two things follow directly:

- **Avoid cash-out mechanics for virtual items.** Cash-out is what converts a monetization system into a gambling system.
- **Write terms that prohibit secondary-market trading, and enforce them.** The prohibition itself is part of what preserved the classification in court.

### The state-level exceptions that break the general rule

- **Washington State:** a 2024 federal decision found virtual coins constitute "things of value" even without any cash-out feature, on the basis of their usability in enabling gameplay. This is the most dangerous single precedent for a free-to-play developer.
- **Eight states** — Alabama, Alaska, Hawaii, Kentucky, Missouri, Nebraska, New Jersey, New York and Washington — define consideration to include tokens or credits involving the extension of a service, entertainment, or the privilege of playing a game without charge. In those states, "you didn't pay for this spin" is a weaker defense than it sounds.

### FTC enforcement, and the settlement that set the current standard

In 2025 the FTC settled with a major gacha publisher for **$20 million** over loot box practices. The required changes are effectively the current compliance baseline for anyone shipping randomized paid rewards in the US:

- **Clear disclosure of item odds**
- **Transparent virtual currency exchange rates**
- **Simplified purchase mechanics**
- **Parental consent for players under 16 making in-game purchases**

FTC civil penalties reach **up to approximately $50,120 per violation**, adjusted annually for inflation. Per violation — not per case.

### Social casino and sweepstakes models

Dual-currency sweepstakes platforms — typically a purchasable "gold coin" plus a promotional "sweepstakes coin" redeemable for cash — are under active attack. **California, Montana, Connecticut and New Jersey have prohibited them outright**, and Pennsylvania, New York, Michigan and Maryland have issued cease-and-desist letters to operators. Treat the dual-currency sweepstakes model as a legal minefield rather than a clever workaround.

### Advertising rules

- Federal law (18 U.S.C. §§ 1302–1304) restricts advertising of lotteries in print and broadcast, with exemptions for state lotteries and licensed entities.
- Advertising must be accurate and not false, deceptive or misleading, and must clearly disclose all material conditions, limitations and eligibility requirements, with no design elements that obscure them.
- **Do not use "risk-free" or "free money" language.**
- Where gambling-adjacent features are promoted, problem-gambling helpline disclosures are required and must be clear and conspicuous.
- Advertisements must not target individuals under 21 in contexts where that applies.

### Licensing

A pure free-to-play game with non-redeemable currency requires **no gambling license**, provided it avoids gambling classification. Only seven states — Connecticut, Delaware, Michigan, New Jersey, Pennsylvania, Rhode Island and West Virginia — currently permit online gambling licenses at all, so if your model needs one, your addressable US market is seven states.

### The compliance checklist

1. Disclose loot box odds clearly, and disclose virtual currency conversion rates
2. Never build cash-out mechanics for virtual items
3. Implement parental consent for under-16 purchases
4. Write and enforce terms prohibiting secondary-market trading
5. Avoid dual-currency sweepstakes models entirely
6. Never use "risk-free" or "free money" in marketing
7. Keep the three-element test in mind at design time — know which leg you removed
8. Watch Washington State and the eight broad-consideration states if you ship randomized rewards

### How this connects to the design sections

The ethical-design guidance elsewhere in this document and the legal guidance here point the same direction, which is convenient. Transparent odds, no cash-out, honest pricing, completion states, and events that don't punish absence are simultaneously the retention-optimal design and the legally defensible one. The designs that create regulatory risk are the same designs that damage long-term retention.

---

## 14. LIVEOPS, EVENTS AND RETURN TRIGGERS

Consistent live operations produce 30–50% higher Day 30 retention, and a well-run seasonal event lifts DAU 20–50% while live. But poorly designed events actively *decrease* retention — quality beats quantity.

### A workable event calendar

- One major seasonal event per quarter
- One minor event weekly
- Daily micro-events

### Design for the five-minute session

Frequency breeds retention: the more often players return, the deeper the game embeds in their routine. Give them daily quests, energy refilling over time, limited daily rewards, rotating shop stock — and make sure a two-minute check-in still feels productive. Design for the five-minute session, not just the thirty-minute one.

### Keep it fresh without shipping a giant update

Stagnation kills retention, but novelty doesn't require scale. A new skin, a surprise bonus event, a twist on an existing mode — small novelties break monotony and give players something to talk about. Plan the roadmap so players hit something genuinely new every few sessions: a new enemy type at one milestone, a new mechanic at another, a new region later.

### Push notifications

- **2–3 per week maximum.** Over-messaging causes fatigue and opt-outs, permanently closing your best re-engagement channel.
- **Personalize and segment by lifecycle stage.** A Day 1 player needs a different message than a Day 30 player.
- **Energy-refill notifications have the highest re-engagement rate** of any type.
- **Use action-oriented language.** "Your troops are ready!" outperforms "Come back and play."
- **Time-zone aware, and always deep-linked** to the exact content the message is about.

---

## 15. FINDING THE LEAKS

Churn is predictable from the **first 24–48 hours** of player behavior. Leading indicators: declining session length, reduced social interaction, repeated failure at difficulty spikes.

- Instrument analytics events at *every stage* of the core loop, not just level completion. Track how many loops players complete per session and where they exit.
- If players consistently quit after the reward phase, either your rewards aren't compelling or the trigger to start the next loop is missing.
- Run funnel analysis on your first 30 minutes and flag every point where more than **5% of players drop off**.
- Fix causes, not symptoms. One studio found a 40% drop-off at level 12 caused by a boss with an untelegraphed one-shot attack — players felt cheated, not challenged. Adding a half-second visual warning took level-12 completion from 58% to 89%.

---

## 16. BUILDING IT WITH AI

**Governing principle: AI is the accelerator, you are the author.** AI compresses roughly the first 60% of the work — prototyping, code scaffolding, asset generation — from months into days. The remaining 40% — design iteration, tuning, polish, playtesting — is not compressed at all, and it determines whether the game is any good. Treating AI as the author rather than the accelerator is the most common reason AI-made games never ship.

### The six-step workflow

**1. Scope the smallest version.**
Not a tool — a decision, and the one that matters most. Because making things is suddenly cheap, **scope sprawl is the single biggest risk** in an AI-assisted project. Cut the idea to one core loop describable in a sentence: one mechanic, one win condition, one player fantasy. "A cozy shop where you brew and sell potions" is a scope. "An open-world RPG with crafting, combat and romance" is a wish. Commit to the small version; you can always expand a game that works.

**2. Prototype the loop with a prompt-to-game generator.**
Before writing any real code, find out whether the loop is fun. A prompt-to-game tool turns a one-sentence idea into a playable 2D sketch in minutes. Play it, hand it to a friend, feel whether the core is engaging. This is the cheapest possible way to kill a bad idea or confirm a good one — and it is **disposable by design**. Do not build your real game on top of the generator's output. Its only job is answering "is this fun?" and then getting out of the way.

**3. Pick a real engine and scaffold with an AI code assistant.**
Once the loop is proven, move to a real engine chosen for your game's needs, not for how "AI-friendly" it is — high-fidelity 3D, broad 2D/3D with a large ecosystem, or lightweight open-source 2D. AI assistants work across all of them. Use the assistant to set up structure, generate boilerplate, stand up core systems. It will write a lot of correct code fast, but **read and test every change**, especially anything performance-critical.

**4. Generate art and audio with stage-specific tools.**
Match each asset type to the right tool: image models for 2D art and textures, 3D generators for props and blockout, music generators for score, voice tools for VO and SFX. Two rules keep this from going wrong — **every output needs a human art-direction pass** so the game stays on-style, and **check the commercial license** before you rely on anything.

**5. Build the real systems with AI alongside you.**
This is where a prototype becomes a product: progression, save/load, economy balance, difficulty, moment-to-moment game feel. Pair-program — the assistant is excellent at system code, tests and refactoring — but **the design decisions, the tuning and the feel are authored by you**. A jump that lands right, an economy that stays tense, a difficulty curve that respects the player: AI can implement your intent quickly, but it cannot supply the intent.

**6. Playtest, polish, and declare AI content.**
Put it in front of real players; playtesting surfaces problems no tool predicts, and polish is the difference between a project and a game. Before release, **declare AI-generated content where required** — storefront submission forms ask — and keep written proof of your commercial rights to any AI-made art, audio or code. None of this final stretch is something AI does for you, which is exactly why most of the remaining work lives there.

**The rhythm of the whole process:** AI gets each piece to *good enough to test* fast, and you take it the rest of the way to *good enough to ship*. Budget your time for that last mile — it's where the game actually becomes good.


### The 2026 AI tool landscape

Match the tool to the stage. Prices are indicative monthly rates and move constantly.

| Stage | Tool | What it does | Approx. price |
|---|---|---|---|
| Code | Cursor | AI-native IDE for gameplay scripting and refactors | $20 |
| Code | GitHub Copilot | In-editor completion across engines | $10 |
| Engine-native | Unity AI | Inference and character assistance inside Unity | In Unity plans |
| 3D | Meshy | Text or image to engine-ready model in ~1 min; fastest all-rounder | ~$14.50 |
| 3D | Tripo | Clean quad topology, good for Blender import and animation | ~$12 |
| 3D | Rodin | High-fidelity 4K PBR textures for hero assets | $30 |
| 3D | Luma Genie | Free text-to-3D, good enough for greyboxing | Free / ~$30 |
| 2D | Scenario | Custom-trained, style-consistent sprites and concept art | $15 |
| 2D | Leonardo | General concept art, textures, props | ~$12 |
| Animation | Cascadeur | Physics-aware AI keyframe assistance | Free / ~$12 |
| Mocap | Move AI | Markerless motion capture from ordinary video | Usage-based |
| NPCs | Inworld | Real-time conversational character runtime | From $25 |
| NPCs | Convai | Conversational NPCs with vision and gesture support | Free / $29 indie |
| NPCs | NVIDIA ACE | On-device NPC reasoning, no cloud dependency | Platform (RTX) |
| Audio | ElevenLabs | Character voice synthesis; licensed music | From ~$6 |
| Audio | Replica Studios | AI voice under a SAG-AFTRA framework agreement | Freemium |
| Audio | AIVA | Adaptive music composition with commercial licensing | Free / ~$15 |

### Tooling strategy by team size

- **Solo or small indie:** use free and discounted tiers for prototyping, and upgrade only the stages you actually intend to ship. A workable starting stack is Meshy or Luma for 3D, Cascadeur for animation, Cursor for code, and free voice options.
- **Mid-size studio:** standardize on one tool per pipeline stage with commercial rights secured. Use style-trained 2D generation for visual cohesion. Treat AI voice as placeholder before hiring union actors.
- **Large studio:** prioritize engine-native and on-device solutions, and set internal policy on disclosure, data provenance and commercial rights.

### Rights and disclosure, current standards

- **Steam requires generative-AI disclosure.** As of January 2026 pure efficiency tools — AI-assisted coding, for instance — are exempt; generated content is not.
- **Free tiers typically do not grant commercial rights.** All commercial use requires paid-tier licensing. Buy the commercial rights before you ship, not after.
- **AI voice work** is governed by the 2025 SAG-AFTRA interactive-media agreement.
- Maintain human quality review at every stage, and be transparent with players about AI-generated content.

### Market context worth knowing

Generative AI in gaming was roughly a **$2.21 billion market in 2026**, projected to reach **$5.09 billion by 2030**. Roughly **one in three new Steam games now discloses AI use**, up from about 11% in 2024. At the same time **52% of developers say AI negatively impacts the industry while 36% actively use it** — adoption is real but cautious, and player sentiment is not settled. Disclose accordingly, and let the craft rather than the tooling be what you market.


### Practical constraints

- **Pure no-code only works for tiny browser games.** For anything you intend to sell you need enough coding ability to read and verify what the assistant writes, catch mistakes, and make architecture and performance calls. AI lowers the barrier dramatically; it does not remove the need to understand what your game is doing.
- **Timeline reality:** a playable prototype can take an afternoon. A small commercial game still takes months, because design iteration, tuning and playtesting dominate the schedule and AI doesn't shorten them.
- **Legal:** selling an AI-assisted game is generally fine, with two cautions — verify the commercial license of every tool whose output you ship, and keep your own records. The legal landscape around training data is still evolving, so favor tools with clear commercial terms.

### How to prototype well on a generator site

- **Grey-box it.** Feed placeholder shapes, not finished art. If the loop is only fun because the art is beautiful, you won't find that out until much later.
- **Build two or three variants of the same loop in one sitting and compare them.** The real value of cheap prototyping is comparison, not speed.
- **Hand it to someone else and say nothing** while you watch the first thirty seconds. That's your Day 1 retention, live.

---

## 17. DECISION RULES

Retention isn't a single silver bullet — it's a system where every element reinforces the others. The core loop creates the habit. The first-time experience makes the impression. Progression provides momentum. Social features build the community. The economy keeps it fair. Narrative creates investment. Events and notifications provide the return triggers.

### Do

- Start where your data says the leak is, not where the advice is loudest
- Ship social features before almost anything else
- Sell cosmetics, convenience and content
- Let the player choose their reward
- Introduce the battle pass after they're invested
- Differentiate on meta layer, art and theme
- Keep the tutorial under thirty seconds
- Design the five-minute session
- End every session with something invested
- Read every line of code the assistant writes

### Don't

- Sell power
- Front-load your systems
- Ship randomized paid rewards without checking your jurisdictions
- Build endless progression with no completion state
- Punish players for missing an event
- Send more than three notifications a week
- Differentiate on the merge mechanic itself
- Build the real game on the prototype generator's output
- Let AI decide what's fun
- Confuse revenue this week with revenue this year

### The one-line version

Use AI to reach a testable game fast, spend the time you saved on the craft it can't do, retain through community and story rather than friction, and sell convenience and beauty rather than power.

---

## KNOWN GAPS IN THIS DOCUMENT

Not yet covered — flag these if asked about them:

- App Store / Google Play optimization
- Soft launch methodology (region choice, gating metrics, duration)
- ROAS and LTV modeling, cohort payback periods, creative testing methodology
- Web and instant-play distribution economics
- Gacha specifics (pity systems, banner design, dupe conversion)
- Full-year live game post-mortems with real numbers
- Solo dev production time budgets
- Art pipeline specifics (atlases, resolution targets, file size)
- Game feel and juice craft
- Business setup, tax, storefront payouts
- COPPA specifics and age-gating implementation
- Non-US regulation (EU, UK, China, Japan)
