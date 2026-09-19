# Research Plan

## Status

**Closed for production implementation on 2026-09-20.**

Static evidence and Research Harness 0.0.1 runtime evidence are recorded in:
- `docs/FIXED_TIMESTEP_AND_UI_RESEARCH.md`;
- `docs/VERIFIED_RUNTIME_DATA.md`;
- `docs/TEST_BUILD_LOG.md`.

## Accepted implementation contract

Production 0.1.0 should:
- start each meditation session at 1×;
- map 1×/2×/4× to timeScale 10/20/40;
- scale fixedDeltaTime proportionally to 0.083333336/0.16666667/0.33333334;
- use native SliderDec/SliderInc semantic callbacks;
- suppress paired gamepad Left/Right navigation only while WaitingGUI owns those D-pad inputs;
- initialize from the exact WaitingGUI button-tip event after vanilla waiting timing is installed;
- preserve vanilla wake/Back behavior;
- use the existing waiting-tip surface with improved readability;
- restore ordinary timing through vanilla StopWaiting, with narrow defensive cleanup only for abnormal hide/plugin teardown;
- avoid save mutation, EnvironmentEngine patches, sleep changes, ordinary-gameplay timing changes, and permanent polling.

## Next step

Create `dev/0.1.0` from the stable line, copy the accepted research conclusions, implement the minimal production mechanism, perform a clean Release build, record source SHA/artifact hash, and hand the candidate to the user for final in-game acceptance.
