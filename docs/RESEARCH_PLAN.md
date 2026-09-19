# Research Plan

## Goal

Close the remaining evidence gates before production implementation.

## Phase 1 — fixed timestep

Determine the least-sufficient `Time.fixedDeltaTime` policy for meditation speeds 1×/2×/4×.

Static checks:
1. inspect meaningful Graveyard Keeper `FixedUpdate` consumers that remain active during meditation;
2. identify any logic whose correctness depends materially on fixed-step size rather than elapsed scaled time;
3. confirm whether scaling the fixed timestep with the meditation multiplier preserves vanilla semantics or causes skipped/oversized simulation steps.

Runtime acceptance should compare at minimum:
- vanilla 1×;
- 2×;
- 4×;
- crossing midnight/day rollover;
- waking manually after changing speed;
- repeated open/change/close cycles;
- scene/save lifecycle recovery if it can leave time settings modified.

Measure/log effective `timeScale`, `fixedDeltaTime`, and observed frame/fixed-update behavior only as needed. Do not add permanent diagnostic polling to production.

## Phase 2 — input/UI seam

Prove the narrowest way to bind `GameKey.SliderDec` / `GameKey.SliderInc` to `WaitingGUI`.

Preference order:
1. reuse/replace the active GUI's native key delegate at meditation lifecycle initialization if practical and robust;
2. intercept the existing semantic slider-key callbacks only for `WaitingGUI`;
3. use a narrow `WaitingGUI.Update()` input check only if the event/delegate seams are materially worse.

The selected design must:
- work on keyboard and gamepad;
- consume the input only while meditation is the active GUI;
- preserve the vanilla wake/exit key;
- update the visible speed indicator immediately;
- avoid global recurring work.

## Phase 3 — lifecycle safety

Prove that all exits restore ordinary timing:
- normal wake;
- Back/close path;
- repeated meditation sessions;
- save/load or scene transition edge cases that can occur while waiting;
- plugin failure/unload behavior where practical.

Prefer vanilla restoration ownership. Add fallback cleanup only for a proven lifecycle gap; do not create a second timing owner unnecessarily.

## Phase 4 — implementation candidate

After phases 1–3 are closed:
- create `dev/0.1.0` from the accepted research state;
- implement only the proven mechanism;
- add the minimal UI;
- perform a clean Release build;
- record exact source SHA and artifact hash in `docs/TEST_BUILD_LOG.md`;
- hand the numbered candidate to the user for real-game acceptance.

Hosted CI is not required for the research phases. Use it only when a concrete candidate build property needs proving.
