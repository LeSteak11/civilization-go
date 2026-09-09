# Oracle fixture exporter

Exports comparison fixtures from the pinned Node oracle
(`_aiinfodocs/simulation/simulate.js`) into `fixtures/oracle/`.

Built at **M1**, when there is a C# implementation to compare against. Until then the
only thing that exists is the pin itself, verified by `ORC-02`.

Rules that the exporter must honour when it is written:

- Read the oracle revision from `fixtures/oracle/oracle-pin.json` and refuse to run if the
  file on disk does not hash to the pinned digest.
- Emit the full contract: rules version, exact content bytes and hash, seed, 24 PLAYER
  commands, 24 SNAPSHOT commands, and on the output side lane modifiers, 72 offers,
  per-turn canonical states, ordered events, final result and canonical hashes.
- Never overwrite an existing fixture as a side effect of a comparison run. Regeneration
  is an explicit, separate, documented act.
