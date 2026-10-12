# Rust Week on PEAK

A mashup that plays **PEAK** with **Rust**'s items and survival: every PEAK item is
renamed to a matching Rust item and draws that item's real icon (read from *your own*
Rust install), and a small Rust survival layer runs on top — thirst, radiation, thirst
that refills when you drink, plus gathering and crafting with a Rock.

- **Primary game:** PEAK
- **Companion game:** Rust (only read, never copied)
- **Loader:** BepInEx (installed automatically by Melty)
- **Mode:** solo / offline

## What it does

| System | What happens |
| --- | --- |
| Item mapping | 197 PEAK items get a curated Rust item + icon (FireWood → Wood, Stone → Rock, Bandage → Bandage, …). |
| Survival HUD | Hunger, Thirst, Radiation and Warmth meters, day clock, Rust-styled panel. |
| Drinking | Drinking a renamed water bottle refills Thirst; anti-rad items clear Radiation. |
| Gathering | Hold the **Rock** and press **primary use** to knock Wood out of trees, Stone out of rocks and Plant Fibre out of plants. |
| Crafting | Hold the **Rock** and press **secondary use** to open the crafting menu; scroll to pick, primary use to craft. |
| Base Camp | The first minute of an island gives you a **Rock, Water and Food** plus a small supply drop. |

## Controls

- `1` / `2` / `3` — quick slots (slot 1 is the Rock from Base Camp)
- **Primary use** — gather (with the Rock) / craft (menu open)
- **Secondary use** — open or close the crafting menu (with the Rock)
- **Scroll** — pick a recipe

### Recipes

| Item | Cost |
| --- | --- |
| Torch | 2 Wood |
| Flare | 1 Wood + 2 Plant Fibre |
| Bandage | 3 Plant Fibre |
| First Aid Kit | 2 Bandage + 1 Honey |
| Campfire | 4 Wood + 2 Stone |

## Install

Melty installs PEAK's BepInEx loader and drops the plugin into
`…/PEAK/BepInEx/plugins/RustWeekOnPEAK/`. The mod finds your Rust install by itself
(Steam library files / registry). If Rust is missing it simply runs PEAK unchanged.

## Notes

- Your Rust install is only ever **read**; nothing from Rust is shipped or copied.
- Works in solo/offline. A host still gets the Base Camp kit; other players keep PEAK.

## Licence

MIT — see `LICENSE`.
