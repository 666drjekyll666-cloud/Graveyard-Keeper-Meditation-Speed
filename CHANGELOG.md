# Changelog

All notable user-facing changes will be documented here.

## Unreleased

### 0.1.0 candidate
- Add meditation speed selection: 1×, 2×, and 4×.
- Use Graveyard Keeper's native keyboard/gamepad SliderDec/SliderInc controls.
- Show the current speed in the existing meditation tip with larger presentation than the research harness.
- Preserve vanilla wake/Back behavior.
- Scale `Time.fixedDeltaTime` proportionally with meditation speed to keep fixed-step demand close to vanilla meditation.
- Suppress duplicate WaitingGUI D-pad Left/Right navigation while the D-pad is changing meditation speed.
- Add narrow cleanup for abnormal WaitingGUI hide/plugin teardown.
- Do not modify save data, sleep speed, ordinary gameplay timing, or `EnvironmentEngine`.
