# Changelog

All notable user-facing changes are documented here.

## 1.0.0 — 2026-09-20

First official stable release.

- Add in-meditation speed selection: 1×, 2×, and 4×.
- Use Graveyard Keeper's native keyboard/gamepad slider controls.
- Make one physical press change the speed by exactly one step.
- Show only currently available speed directions at the 1× and 4× boundaries.
- Preserve vanilla wake/Back behavior.
- Scale `Time.fixedDeltaTime` proportionally with meditation speed to keep real-time fixed-step demand close to vanilla meditation.
- Preserve native scaled-time behavior for world progression, recovery, crops, NPCs, crafting, and other simulation systems.
- Keep sleep speed, ordinary gameplay timing, and save data unchanged.
- Harden Harmony startup with explicit plugin-assembly patching and rollback of partial patches on initialization failure.

## 0.1.2 — 2026-09-20

Pre-1.0 accepted build; superseded by 1.0.0.

- Add in-meditation speed selection: 1×, 2×, and 4×.
- Use Graveyard Keeper's native keyboard/gamepad slider controls.
- Make one physical press change the speed by exactly one step.
- Show the current speed in the existing meditation tip.
- Hide unavailable directions at the 1× and 4× boundaries without shifting the rest of the tip.
- Preserve vanilla wake/Back behavior.
- Scale `Time.fixedDeltaTime` proportionally with meditation speed to keep real-time fixed-step demand close to vanilla meditation.
- Preserve native scaled-time behavior for world progression, recovery, crops, NPCs, crafting, and other simulation systems.
- Keep sleep speed, ordinary gameplay timing, and save data unchanged.
