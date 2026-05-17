# Final Gameplay Screen Concept Reference

Created: 2026-05-17

## Layout decision

The final gameplay screen should use a **dungeon-dominant split layout** rather than a 50:50 split or separate mutually exclusive screens.

```text
┌──────────────────────────── Global UI ────────────────────────────┐
│ Shared currencies / Frontline level / Dungeon depth / alerts       │
├──────────────────────────────┬─────────────────────────────────────┤
│                              │                                     │
│ Dungeon main viewport        │ Persistent defense side panel       │
│ about 68% width               │ about 32% width                     │
│                              │                                     │
├──────────────────────────────┴─────────────────────────────────────┤
│ Action bar: hero HP / skills / potion / latest loot / inventory    │
└────────────────────────────────────────────────────────────────────┘
```

## Why this shape fits the game

- **Dungeon combat needs the most screen space** because it has the highest interaction density: click movement, targeting, evasion, loot reading, and room navigation.
- **Ground defense should stay visible at all times** because the game is built around one live runtime where both loops continue together, but it does not need equal space because it is mostly automatic and decision-light.
- **Top-level resources belong in a shared global bar** because Gold, Scrap, Frontline level, and Dungeon depth describe the whole run rather than only one subsystem.
- **Moment-to-moment actions belong on the bottom bar** because hero HP, skills, potions, recent loot, inventory, and crafting are the controls the player reaches for during play.
- **Inventory and crafting should open as overlays**, not occupy permanent screen real estate, so the game does not collapse into a management dashboard.

## Reference image

![Final gameplay screen concept](2026-05-17_FinalGameplayScreenConcept.png)

## Guardrails

- Keep the dungeon area visually dominant.
- Keep the defense lane alive and readable even while the player is in the dungeon.
- Do not split the game into mutually exclusive player-facing modes by default.
- Avoid RTS-like unit micromanagement, tower-placement puzzles, and always-open inventory panes.
