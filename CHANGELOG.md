# Changelog

All notable user-facing changes will be documented here.

## Unreleased

### 0.1.2 candidate
- Hide the unavailable left arrow at 1× and the unavailable right arrow at 4×.
- Preserve fixed-width spacing so the rest of the meditation tip does not jump when speed changes.
- Keep the accepted 0.1.1 timing, input release-gating, and lifecycle behavior unchanged.

### 0.1.1 candidate
- Make one physical SliderDec/SliderInc press produce exactly one speed step by using Graveyard Keeper's native `LazyInput.WaitForRelease` gate.
- Block the paired Left/Right logical key until the same physical direction is released, preventing D-pad navigation leakage without polling.
- Replace raw `(DLeft)/(DRight)` speed-tip tokens with compact language-neutral chevrons: `< 1× >`, `< 2× >`, `< 4× >`.
- Keep the accepted timing model, wake behavior, larger tip presentation, and lifecycle cleanup unchanged.

### 0.1.0 candidate
- Add meditation speed selection: 1×, 2×, and 4×.
- Use Graveyard Keeper's native keyboard/gamepad SliderDec/SliderInc controls.
- Show the current speed in the existing meditation tip with larger presentation than the research harness.
- Preserve vanilla wake/Back behavior.
- Scale `Time.fixedDeltaTime` proportionally with meditation speed to keep fixed-step demand close to vanilla meditation.
- Suppress duplicate WaitingGUI D-pad Left/Right navigation while the D-pad is changing meditation speed.
- Add narrow cleanup for abnormal WaitingGUI hide/plugin teardown.
- Do not modify save data, sleep speed, ordinary gameplay timing, or `EnvironmentEngine`.
