# Oracle comparison fixtures

Engine-neutral JSON. Production C# and the Node oracle both consume these; neither calls
the other (Technical Plan sec.9.2).

## Contract

```
fixture input  = rulesVersion + exact content bytes/hash + seed
               + 24 PLAYER commands + 24 SNAPSHOT commands
fixture output = lane modifiers + 72 offers + per-turn canonical states
               + ordered events + final result + canonical hashes
```

## Rules

1. The oracle revision is pinned in `oracle-pin.json` and verified before a comparison run.
2. Comparison is field-by-field first, then canonical bytes and hashes.
3. On mismatch, report the **first** divergent turn, phase and field. Never scan past it.
4. **Expected fixtures are never updated automatically.** A fixture change requires either
   a documented rules/content version change or an approved oracle defect correction.
5. Where the oracle and the Core Spec disagree, **the spec wins** (Technical Plan sec.14).

## Status

Empty at M0 by design. M1 exports the first suite: every mechanic, plus 100-1,000 full
seeded runs.
