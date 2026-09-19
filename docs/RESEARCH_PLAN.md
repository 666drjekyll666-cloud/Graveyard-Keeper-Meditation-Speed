# Research Plan

## Goal

Close the remaining runtime evidence gates before production implementation.

Static research is recorded in `docs/FIXED_TIMESTEP_AND_UI_RESEARCH.md`.

## Phase 1 — fixed timestep

Static inspection is complete.

Leading candidate:
- 1×: `timeScale 10`, `fixedDeltaTime 0.083333336`
- 2×: `timeScale 20`, `fixedDeltaTime ~0.16666667`
- 4×: `timeScale 40`, `fixedDeltaTime ~0.33333334`

Reason: this preserves approximately the vanilla-meditation real-time fixed callback rate (~120/s).

Open risk: the 4× fixed step is four times coarser in game time than vanilla meditation. Movement/path/kick/drop code contains fixed-step-sensitive behavior.

A research-only runtime harness must compare:
1. vanilla 1× baseline;
2. 2× proportional;
3. 4× proportional;
4. 4× with vanilla meditation fixed step;
5. 4× with an intermediate bounded step (~0.16666667) only if needed to distinguish the trade-off.

For each short probe record:
- effective `timeScale`;
- effective `fixedDeltaTime`;
- actual FixedUpdate callbacks per real second;
- rendered/update frames per real second;
- world-time delta over real time;
- any obvious movement/path/physics anomaly.

Avoid a long matrix if proportional 4× is already clean and stable.

## Phase 2 — input/UI seam

Static-preferred input seam:
- patch `BaseGUI.OnPressedSliderDec()` / `OnPressedSliderInc()`;
- effect only active `WaitingGUI`;
- return true only when a speed change/handled bound occurs.

Runtime must prove:
- A / Left and D / Right change speed;
- D-pad Left/Right changes speed;
- wake Interaction remains unchanged;
- paired logical Left/Right does not produce an unwanted WaitingGUI navigation action;
- no input leaks to unrelated GUIs.

Static-preferred presentation seam:
- use the exact WaitingGUI `ButtonTipsStr.Print(GameKeyTip)` event that occurs after vanilla installs the waiting clock;
- preserve the localized vanilla wake text;
- append/prepend native SliderDec/SliderInc icons and current speed;
- redraw only on speed change.

Runtime must prove layout/font/readability and no unrelated button-tip mutation.

## Phase 3 — lifecycle safety

Runtime must prove:
- Interaction wake restores `timeScale=1` and `fixedDeltaTime=0.016666668`;
- Back wake restores the same values;
- repeated meditation sessions begin and end cleanly;
- a speed change does not survive outside waiting;
- one safe load/scene edge is checked if it can bypass `StopWaiting()`.

Do not add a global timing watchdog. Add fallback cleanup only if a real bypass is demonstrated.

## Phase 4 — implementation candidate

After phases 1–3 are closed:
- freeze the accepted research conclusion;
- create `dev/0.1.0`;
- implement only the proven mechanism;
- add the minimal UI;
- perform a clean Release build;
- record exact source SHA and artifact hash in `docs/TEST_BUILD_LOG.md`;
- hand the numbered candidate to the user for real-game acceptance.

Hosted CI is justified for a research harness only when it is needed to produce the exact runtime artifact. Routine research/docs still do not trigger hosted builds.
