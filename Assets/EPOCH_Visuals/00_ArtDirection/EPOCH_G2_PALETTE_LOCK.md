# EPOCH Group 2 — Locked palette (paste for MJ packs)

**Status:** Locked from Stage-0 DNA (bottom-right MJ pick) + `board_carved_sandstone.png`. GoldWire is a restrained target (DNA band currently reads ~`#B4826E` bronze — ChatGPT refine should push thin inlays toward `#C4A35A`).

## Named roles
| Role | Hex | Layer | Use |
|------|-----|-------|-----|
| **NavyWire** | `#0F2D46` | Capital chrome | Primary navy surface / wireframe body |
| **NavyMid** | `#193C55` | Capital chrome | Lit navy midtone on props |
| **GoldWire** | `#C4A35A` | Capital chrome | Thin geometric edge/inlay only — NOT currency |
| **GoldWireDeep** | `#8E6B2A` | Capital chrome | Gold wire shadow / recessed inlay |
| **SandBase** | `#EBD4B3` | Prop fill | Main sandstone fill |
| **SandRing** | `#CD9E8C` | Prop fill | Plinth / carved ring stone |
| **OchreWarm** | `#E8A969` | Prop fill | Warm ochre highlight |
| **TurquoiseAccent** | `#30C3C3` | Prop fill | Inlay / accent on navy or sand |
| **TurquoiseDeep** | `#297673` | Prop fill | Recessed turquoise |
| **CoralAccent** | `#DE7846` | Prop fill | Coral accent |
| **TerracottaAccent** | `#BD7439` | Prop fill | Terracotta mass / warm earth |
| **MatteShadow** | `#0A0A0F` | Shared | Contact shadow under props |
| **GoldCurrency** | `#E0B84A` | Currency only | Gold icon + word Gold only; never on stage props |

## Capital chrome vs prop fill
- **Capital chrome (stage ladder + modal):** `NavyWire`, `NavyMid`, `GoldWire`, `GoldWireDeep` — dark navy body, thin gold geometric edges only.
- **Prop fill (tabletop materials):** `SandBase`, `SandRing`, `OchreWarm`, `TurquoiseAccent`, `TurquoiseDeep`, `CoralAccent`, `TerracottaAccent`.
- **Shared:** `MatteShadow` under props.
- **Currency only:** `GoldCurrency` — Gold icon + label **Gold**. Never paint `GoldCurrency` onto landmark stages (collides with `GoldWire`). Never put Gold chrome in Battle.

## MJ paste line (append after UX tabletop prefix)
```
dark navy NavyWire #0F2D46 surfaces with restrained GoldWire #C4A35A geometric edge inlays, SandBase #EBD4B3 and SandRing #CD9E8C carved stone, OchreWarm #E8A969, TurquoiseAccent #30C3C3, CoralAccent #DE7846, TerracottaAccent #BD7439, soft MatteShadow #0A0A0F, matte miniature, no metal-chrome bling, no neon, no yellow-metal gold paint flooding the prop
```

## Stage ladder reminder
0 empty plinth → 5 complete same DNA; mass/fill changes only; phone ~72pt stage-vs-stage; five-across scannable.