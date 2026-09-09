# EPOCH V1 Balance Report

## 1. Executive summary

Validation passed before analysis. The baseline contains 10,500 complete paired-orientation matches; sensitivity experiments add 28,800. PASS occurred on 11.42% of side-turns. PLAYER won 35.00%, with mean PLAYER-minus-SNAPSHOT score 0.000; paired swaps are required when interpreting policy strength.

## 2. Validation and determinism results

- Concrete golden/conformance scenarios executed: 49; failures: 0. No unconditional placeholder assertion remains.
- Effect coverage: PASS for all 35 authored effects across 29 explicit implementation sources.
- Repeated 1,000-match byte-equivalence suite: PASS.
- Indexed offer RNG isolation from logging/presentation: PASS.
- Shared offers and 150 normalized authoritative paired side swaps: PASS.
- Every sensitivity is paired with a MATCHED_BASELINE cohort using identical policies, seeds, and orientations.
- Previous simulator results are invalid and superseded by this repaired run.
- JSON content version: 1.0.1-v1; rules compatibility unchanged at 1.2.0-v1-final.

## 3. Baseline configuration

24 turns, Growth 8, Insight 2, unit HP 100, BUILD through turn 20, current late-BUILD cost, current REACH protection, and overflow damage. All six policies are paired in every unordered matchup, including mirrors, across 250 seeds and both orientations.

## 4. Bot-policy definitions

- RANDOM_LEGAL samples a current legal card/target only.
- ECONOMY_FIRST prefers early Growth BUILD and rejects negative-payback late BUILD when alternatives exist.
- MILITARY_FIRST reinforces weak lanes and prefers TRAIN/class military perks.
- INSIGHT_FIRST prefers ADVANCE, Insight structures, scaling perks, and Keystones.
- BOARD_CONTROL scores immediate weak-lane reinforcement and COAST access.
- ADAPTIVE combines current resources, lane power, remaining turns, perk tags, and payback.

No policy inspects future offers.

## 5. Matchup matrix

| Policy | Observed win rate when occupying either side |
|---|---:|
| RANDOM_LEGAL | 55.37% |
| ECONOMY_FIRST | 7.71% |
| MILITARY_FIRST | 57.37% |
| INSIGHT_FIRST | 13.03% |
| BOARD_CONTROL | 40.91% |
| ADAPTIVE | 35.60% |

Orientation: PLAYER win rate 35.00%, tie rate 30.00%, average scores 6.15–6.15. Any departure from 50% after pairing is a suspected resolution/orientation effect, not policy causation.

## 6. Economy findings

PASS frequency is 11.42% (57556 no-legal side-turns). Mean ending Growth/Insight are 155.07/64.16; mean earned are 299.59/102.88, and mean spent are 152.53/40.72. Mean structures, units, perks, and Keystones per side are 5.76, 7.98, 7.52, and 0.69.

Turn one remains a three-offer decision, but constrained starting affordability means its meaningfulness depends on hand composition; sample and card files permit exact auditing. Late BUILD is selected by policies that value remaining yield, while ECONOMY_FIRST rejects mathematically dominated cases when another legal option exists. Observed selection is not proof that a turn-14–20 BUILD caused a win.

## 7. Combat findings

Mean combats per side: 14.54; killed-unit survival: 6.62 turns; counter advantages: 2.48; REACH participations/protections: 0.98/0.33. Class balance must be interpreted through card-level offer/selection rates and human playtests; the policy heuristics are not tactical solvers.

## 8. Card-level findings

Highest legal-choice selection rates: KEYSTONE_TOTAL_MOBILIZATION 58.10%, KEYSTONE_DOUBLE_TILES 54.93%, KEYSTONE_INSIGHT_ENGINE 52.12%, KEYSTONE_GROWTH_ENGINE 49.70%, BUILD_RIVER_MILL 49.27%. Lowest: TRAIN_A3_SWORD 33.89%, TRAIN_A3_REACH 34.75%, TRAIN_A3_HORSE 35.90%, TRAIN_A3_SPEAR 36.18%, TRAIN_A2_HORSE 36.18%. Garrison Post and River Mill comparisons are observational; River Mill's duplicated baseline increases RIVER test coverage but also dilutes distinct BUILD packages. Full metrics are in the companion CSV.

## 9. Lane-modifier findings

All seeds contain exactly one RIVER, HIGHLAND, and COAST. Lane-target counts are recorded per card. Persistent modifier advantage should be treated as a hypothesis until policy-by-modifier score attribution and human tactical play corroborate it.

## 10. Sensitivity results

| Variation | Matches | PASS | End G | End I | Avg diff | PLAYER win | Tie |
|---|---:|---:|---:|---:|---:|---:|---:|
| BASELINE | 10500 | 11.42% | 155.07 | 64.16 | 0.00 | 35.00% | 30.00% |
| MATCHED_BASELINE_START_GROWTH_6 | 1200 | 11.55% | 143.10 | 63.62 | 0.00 | 49.33% | 1.33% |
| START_GROWTH_6 | 1200 | 16.10% | 94.64 | 53.57 | 0.00 | 48.92% | 2.17% |
| MATCHED_BASELINE_START_GROWTH_10 | 1200 | 11.55% | 143.10 | 63.62 | 0.00 | 49.33% | 1.33% |
| START_GROWTH_10 | 1200 | 11.10% | 139.94 | 64.76 | 0.00 | 50.00% | 0.00% |
| MATCHED_BASELINE_START_INSIGHT_0 | 1200 | 11.55% | 143.10 | 63.62 | 0.00 | 49.33% | 1.33% |
| START_INSIGHT_0 | 1200 | 11.57% | 145.89 | 62.26 | 0.00 | 49.33% | 1.33% |
| MATCHED_BASELINE_START_INSIGHT_4 | 1200 | 11.55% | 143.10 | 63.62 | 0.00 | 49.33% | 1.33% |
| START_INSIGHT_4 | 1200 | 11.32% | 145.75 | 65.03 | 0.00 | 49.33% | 1.33% |
| MATCHED_BASELINE_UNIT_HP_75 | 1200 | 11.55% | 143.10 | 63.62 | 0.00 | 49.33% | 1.33% |
| UNIT_HP_75 | 1200 | 11.43% | 141.48 | 63.72 | 0.00 | 49.58% | 0.83% |
| MATCHED_BASELINE_UNIT_HP_125 | 1200 | 11.55% | 143.10 | 63.62 | 0.00 | 49.33% | 1.33% |
| UNIT_HP_125 | 1200 | 11.63% | 143.82 | 63.56 | 0.00 | 49.67% | 0.67% |
| MATCHED_BASELINE_BUILD_CUTOFF_14 | 1200 | 11.55% | 143.10 | 63.62 | 0.00 | 49.33% | 1.33% |
| BUILD_CUTOFF_14 | 1200 | 11.68% | 128.81 | 50.68 | 0.00 | 49.67% | 0.67% |
| MATCHED_BASELINE_BUILD_CUTOFF_18 | 1200 | 11.55% | 143.10 | 63.62 | 0.00 | 49.33% | 1.33% |
| BUILD_CUTOFF_18 | 1200 | 11.57% | 141.89 | 59.19 | 0.00 | 49.33% | 1.33% |
| MATCHED_BASELINE_LATE_BUILD_COST_75 | 1200 | 11.55% | 143.10 | 63.62 | 0.00 | 49.33% | 1.33% |
| LATE_BUILD_COST_75 | 1200 | 11.48% | 146.28 | 65.08 | 0.00 | 49.25% | 1.50% |
| MATCHED_BASELINE_LATE_BUILD_COST_125 | 1200 | 11.55% | 143.10 | 63.62 | 0.00 | 49.33% | 1.33% |
| LATE_BUILD_COST_125 | 1200 | 11.62% | 140.74 | 62.16 | 0.00 | 49.42% | 1.17% |
| MATCHED_BASELINE_NO_REACH_PROTECTION | 1200 | 11.55% | 143.10 | 63.62 | 0.00 | 49.33% | 1.33% |
| NO_REACH_PROTECTION | 1200 | 11.55% | 143.10 | 63.62 | 0.00 | 49.33% | 1.33% |
| MATCHED_BASELINE_NO_DAMAGE_OVERFLOW | 1200 | 11.55% | 143.10 | 63.62 | 0.00 | 49.33% | 1.33% |
| NO_DAMAGE_OVERFLOW | 1200 | 11.55% | 143.16 | 63.59 | 0.00 | 49.33% | 1.33% |

The requested 100% reference cases are the baseline: starting Growth 8, Insight 2, HP 100, cutoff 20, late cost 100%, current REACH, and overflow.

## 11. Suspected dominant strategies

No policy result is causal. A policy whose paired win rate leads every matchup is a suspected dominant heuristic only. Insight accumulation, stranded resources, economic-versus-military Keystone results, and leading-side persistence require the detailed CSVs and human play.

### Required design-question answers

1. **PASS:** 11.42% of side-turns.
2. **Turn one:** mechanically meaningful, but some hands constrain affordability; human feel remains unproven.
3. **Insight accumulation:** yes; mean ending Insight is 64.16, indicating supply materially exceeds modeled spending.
4. **Stranded resources:** both strand, especially Growth (155.07) and Insight (64.16).
5. **Late BUILD selection:** yes under several heuristics; ECONOMY_FIRST avoids mathematically dominated cases when another legal option exists.
6. **Turn 14–20 BUILD favorable:** sometimes associated with wins, but observational results cannot establish causal favorability.
7. **Garrison Post dominance:** no clear dominance; its legal-choice rate is 47.15% versus Growth Works 48.79%.
8. **River Mill duplication:** it improves RIVER sampling but dilutes the Growth BUILD pool; removal or differentiation is a promising hypothesis.
9. **Universal policy dominance:** none established causally; §5 reports observed heuristic performance.
10. **Class dominance:** none established; class selection is close enough to require a targeted policy-neutral experiment.
11. **Lane modifier advantage:** selection is recorded by modifier, but persistent score causation is not established.
12. **REACH:** rarely activated (0.979 participations per side), so it is under-observed rather than demonstrably weak or strong.
13. **Economic versus military Keystones:** offer rarity and policy valuation make a causal ranking unsafe; card-level observations are in the CSV.
14. **Leader retention:** 0.256 held tiles per side-turn does not establish snowball persistence; longitudinal lead-state analysis is still needed.
15. **Score range:** average scores are 6.15–6.15, compressed for these bots.
16. **41–38 plausibility:** rules permit it, but it is far above this bot population's average and therefore uncommon.
17. **Orientation:** mean differential 0.000 shows no large score bias; PLAYER win 35.00% should still be monitored alongside ties.
18. **Extreme cards:** §8 lists the five highest and lowest legal-choice rates; none is causal without controlling policy and hand alternatives.

## 12. Risks and limitations

The policies are transparent heuristics, not optimal agents. Card win rates are observational. The simulator does not use perfect information. It excludes presentation and persistence. The Core Spec leaves several non-authoritative integration policies provisional. Authoritative damage reads the materialized 81-entry integer table; native exp() is not used during match resolution.

The illustrative 41–38 is plausible only if both sides sustain unusually high contested control; baseline average scores above show whether this bot population reaches that range.

## 13. Recommended tuning changes

### High-confidence problem

- Treat any material paired orientation advantage as an engine-order defect to investigate before balance tuning.
- Treat cards with near-zero legality as pool/eligibility problems before changing their power.

### Promising hypothesis

- Reduce duplicated Growth BUILD weight if River Mill adds test dilution without a measurable RIVER coverage benefit.
- Tune late BUILD cost only if turn-14–20 selection is both rare and negatively associated across multiple policies and sensitivities.
- Compare economic and military Keystones in targeted human play because bot valuation embeds their own bias.

### Needs human playtest

- Whether turn-one hands feel meaningfully distinct.
- Whether REACH protection is legible and satisfying.
- Whether leading-side tile retention feels recoverable.
- Whether score ranges and 41–38 finishes feel exciting rather than predetermined.

## 14. Owner decisions required

No rules or content were automatically changed. Owner approval is required for any magnitude, cost, cutoff, roster, or effect-package tuning. First review priorities are paired orientation delta, PASS rate, stranded Insight, late BUILD outcomes, Garrison Post versus other BUILD entries, River Mill duplication, and extreme card selection rates.
