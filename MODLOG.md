# Mod log — Rust Week on PEAK

## 1.0.0 — unreleased

### Fixed (player reports)
- **No Rock / could not gather.** PEAK's `Stone` prefab had no curated mapping and fell
  through to a keyword rule (`stones`), so the Rock never existed as an item. Mapped
  `Stone → rock`, so Base Camp can hand it out and gathering works. Base Camp now grants
  a Rock, Water and Food into the player's quick slots on the first island.
- **Could not craft.** Added a crafting layer (Rock + secondary use to open, scroll to
  pick, primary use to craft) with five recipes. Outputs are real PEAK items, so they can
  be used or dropped normally.
- **Wrong Rust item per PEAK item.** Replaced the keyword guesser with a curated table
  keyed by exact PEAK prefab name (full 197-item coverage), with keyword rules only as a
  fallback.
- **Could not refill thirst by drinking.** `Item.Consume` is now observed; a consumed
  drink refills Thirst and anti-rad items clear Radiation, applied only to the local
  player (solo fallback included).

### Added
- Gathering: hold the Rock and use primary — trees give Wood, rocks give Stone, plants
  give Plant Fibre (unknown surfaces split 50/50 so both stay reachable).
- Shared `RustItems` helpers (Count / Take / Give) that fall back to dropping items on
  the ground when the three quick slots are full.
- Survival toasts for gather / craft / drink events.
- A developer self-test gated behind `%LOCALAPPDATA%\RustWeekOnPEAK\selftest.txt` (inert
  for players) that starts a solo run and writes kit / gather / craft / thirst results to
  the BepInEx log.

### Verified (real solo run, via the self-test log)
- Base Camp slots: `[rock][smallwaterbottle][granolabar]`
- `rock prefab resolved: True`
- Craft: `CRAFTED TORCH`
- Drinking: Thirst 40 → 75; anti-rad: Radiation 50 → 5

### Notes
- Rust content is read from the player's own install only; nothing from Rust is shipped.
- Solo/offline is the supported mode; the host still gets the starter kit.
