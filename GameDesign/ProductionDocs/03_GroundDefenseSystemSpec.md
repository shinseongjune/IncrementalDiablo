# Ground Defense System Specification

## Purpose

The ground layer supplies automatic, persistent pressure and basic resources while the player is in or out of dungeons. It must communicate an ongoing defense without becoming a controllable RTS.

## Core contract

```text
incoming pressure + automatic defenders
-> Hold or Push state
-> wall risk, resources, progression
-> dungeon preparation and long-term upgrades
```

- `DefenseRuntimeState` is the authority for pressure, wall health, state, resources, frontline progression, save/load, and offline simulation.
- `DefenseDirector` owns runtime progression and applies visible wall damage through the authoritative state.
- `GroundDefenseBalanceModel` owns formula-driven scaling and exports; it must not be replaced by hand-authored wave rows.
- `GroundDefenseNavMeshBattlefield` is the live visual layer. It creates autonomous defenders/enemies with the shared character stats, health, motor, combat, team, NavMesh agent, collider, targeting, death, reinforcement, and wall-attack behavior.
- When save data is loaded, `DefenseDirector.SaveDataApplied` forces the live battlefield to rebuild from the restored `DefenseRuntimeState`. Visual actors must not keep attacking when the restored defense state is idle, waiting for repair, or breached.
- `GroundDefenseBillboardUtility` creates the live role sprites and faction readability treatment used by the NavMesh battlefield.

## Player-visible rules

- Friendly and enemy identity must be legible at full and compressed defense-panel scale.
- Every attack must read as `Unit -> action -> target`; wall damage must originate from an enemy at the wall and apply to the authoritative wall state.
- Enemy pressure begins from the far/top side of the defense view; defenders and the protected wall sit toward the lower side.
- The battlefield is automatic. No unit selection, manual movement, focus fire, production queues, workers, free placement, or manual wave schedule is allowed.
- Frontline bands may change force count, role mix, and reinforcement cadence only through current formula profiles. No review-only level override belongs in normal code or HUD text.

## Failure, recovery, and reward

- Hold stabilizes pressure and preserves the wall; Push converts safety into progress pressure.
- A breach must remain visible and must not silently stop recovery income or corrupt save/load state.
- Save/load must visibly restore Frontline Level, Hold/Push state, wall health, pressure/progress, and upgrade levels; actor positions are presentation-only and may be rebuilt from the restored state.
- Ground resources and milestones feed hero, equipment, defense, and future dungeon decisions. They may not form a separate reward economy.
- 2026-07-06 E2-B upgrade comparison is HUD decision support over the existing upgrade model: stressed wall/pressure can recommend Wall, otherwise affordable Tower/Defenders compare DPS gain. It changes no upgrade cost, formula, save field, or scene wiring.

## Scale and balance

- Use reusable formula bands and deterministic `GroundDefenseBalance.csv` exports.
- Clamp values for runtime safety but treat clamps as warning boundaries, not content goals.
- Add defender/role variety only with an identifiable function, a feedback path, a balance knob, and a save/load statement.

## Verification

- `Tools/Automation/Export-GroundDefenseBalance.ps1 -CheckOnly` verifies the formula export.
- `Tools/Automation/Invoke-IncrementalDiabloChecks.ps1` verifies the live NavMesh battlefield, authoritative wall bridge, and removal of superseded presentation components.
- A visual change requires a short Play Mode check for faction identity, attack ownership, death/reinforcement, and wall damage. The accepted E0 battlefield and composition are regression-only.
