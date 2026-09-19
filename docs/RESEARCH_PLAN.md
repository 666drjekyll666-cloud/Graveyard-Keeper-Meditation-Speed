# Research Plan

## Status

**Closed and accepted for stable 0.1.2 on 2026-09-20.**

Static evidence and Research Harness 0.0.1 runtime evidence are recorded in:
- `docs/FIXED_TIMESTEP_AND_UI_RESEARCH.md`;
- `docs/VERIFIED_RUNTIME_DATA.md`;
- `docs/TEST_BUILD_LOG.md`.

## Accepted production contract

Stable 0.1.2:
- starts each meditation session at 1×;
- maps 1×/2×/4× to timeScale 10/20/40;
- scales fixedDeltaTime proportionally to 0.083333336/0.16666667/0.33333334;
- uses native SliderDec/SliderInc semantic callbacks;
- uses native `LazyInput.WaitForRelease` so one physical press produces one speed step even under accelerated scaled time;
- suppresses paired gamepad Left/Right navigation only while WaitingGUI owns those D-pad inputs;
- initializes from the exact WaitingGUI button-tip event after vanilla waiting timing is installed;
- preserves vanilla wake/Back behavior;
- uses the existing waiting-tip surface, with boundary-aware `1×/2×/4×` presentation;
- restores ordinary timing through vanilla StopWaiting, with narrow defensive cleanup for abnormal hide/plugin teardown;
- does not mutate save data, patch EnvironmentEngine for acceleration, change sleep speed, change ordinary-gameplay timing, or introduce permanent polling.

## Acceptance

The 0.1.2 candidate was tested in the installed game and explicitly accepted by the user for stable promotion on 2026-09-20.

No pre-release research gate remains open.
