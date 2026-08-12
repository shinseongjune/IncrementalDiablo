# Data, Save, And Balance

## World checkpoint v2 ownership

- `GameProfileSave` v2 is one checksummed, generation-numbered envelope. It separates `AccountSnapshot` (currencies, defense progression, expedition lifecycle, hero equipment, inventory, UI settings), `DefenseWorldSnapshot`, and the optional open `DungeonWorldSnapshot`.
- Defense world captures the wall building and every generated defender/enemy by stable ID, faction, transform, home position, health, action, and target. Dungeon world captures the loaded template/room seed, combat lifecycle, hero, and every spawned dungeon enemy with the same physical state.
- UI result messages and presentation summaries are runtime-only projections; the checkpoint contains no last-result or descriptive text fields.
- `DefenseSaveManager` writes `incremental_diablo_world_v2.json` after a stable tick barrier only. A manual save briefly holds new frontline actions while already-defeated units finish replacing, then writes the settled roster. Additive-room loads, room start countdowns, actor rebuilds, and active restore projections still reject checkpoint capture instead of normalizing data during save.
- The old `incremental_diablo_profile_v1.json` and `incremental_diablo_save.json` remain untouched evidence. v2 never reads, migrates, or overwrites either file.

## Write, recovery, and restore rules

- Every write seals the payload with SHA-256, flushes a temporary file, rereads and validates that file, then atomically promotes it. A corrupt primary is quarantined; primary and `.bak` are both validated and the highest valid generation wins.
- Restore is two-pass: validate all account/world snapshots and room catalog first, then enter `GameRuntimeRestoreGate` while account owners, defense actors, additive room, hero, dungeon enemies, and combat state are projected. Autosave, combat, spawning, and simulation remain paused until projection finishes.
- `Running` resumes the same loaded template with hero/enemy positions, health, action state, and target identities. `AwaitingExit` resumes the open cleared room and portal choice. Return banks reward, deeper retains it, and death discards it before `Ready`.
- Offline simulation is deliberately skipped for a v2 world checkpoint: it would alter an actual saved battlefield between capture and resume.

## Acceptance checks

- Fresh v2 profile -> save -> restart/load: wall and all visible defense actors retain count, position, and health.
- During a frontline unit's defeated-body or replacement interval, manual save queues the short roster-settle barrier; it must not serialize a missing or newly recreated actor.
- In a running room, save after a partial hit exchange -> restart/load: the same template, hero, enemy count, transforms, health, and combat action resume.
- Make a second save, corrupt only the primary, then load: the highest valid primary/backup generation is selected without touching the v1 files.
- Do not accept E3-D until the return, deeper, death, `Running`, and `AwaitingExit` paths pass this checklist in Play Mode.
