# Changelog

All notable user-facing changes are documented here.

## Unreleased — 1.0.0 candidate

- Harden Harmony startup so the plugin patches its explicit assembly rather than relying on stack-frame assembly detection.
- If Harmony initialization fails partway through on an unsupported/drifted runtime, roll back any patches already installed by this plugin before disabling it.
- Meditation timing, controls, UI, save behavior, and fixed-timestep policy are unchanged from the accepted 0.1.2 runtime behavior.

## 0.1.2 — 2026-09-20

First stable release.

- Add in-meditation speed selection: 1×, 2×, and 4×.
- Use Graveyard Keeper's native keyboard/gamepad slider controls.
- Make one physical press change the speed by exactly one step.
- Show the current speed in the existing meditation tip.
- Hide unavailable directions at the 1× and 4× boundaries without shifting the rest of the tip.
- Preserve vanilla wake/Back behavior.
- Scale `Time.fixedDeltaTime` proportionally with meditation speed to keep real-time fixed-step demand close to vanilla meditation.
- Preserve native scaled-time behavior for world progression, recovery, crops, NPCs, crafting, and other simulation systems.
- Keep sleep speed, ordinary gameplay timing, and save data unchanged.
