# Runtime Harness 0.0.1 — Test Matrix

Status: source candidate. Do not treat results as accepted until the exact built DLL is run in Graveyard Keeper 1.407.

## Purpose

This research-only harness answers four remaining questions:

1. Which `Time.fixedDeltaTime` policy is acceptable at 4× meditation?
2. Does the native BaseGUI SliderDec/SliderInc seam work for keyboard and gamepad without unwanted paired Left/Right behavior?
3. Does the existing WaitingGUI button-tip surface support a readable live speed indicator without recurring UI polling?
4. Does vanilla StopWaiting restore ordinary timing after speed changes?

The harness does not write save data.

## Automatic measurements

Each timing mode starts a five-real-second sample and logs:

- effective `Time.timeScale`;
- effective `Time.fixedDeltaTime`;
- rendered Update frames / real second;
- actual `CustomUpdateManager.FixedUpdate()` callbacks / real second;
- world-time progress / real second;
- day rollover if it occurs.

A completed sample is visible in the meditation tip as `sample:done`.

## Controls

While meditation is active:

- normal native `SliderDec` / `SliderInc` controls change 1× <-> 2× <-> 4×;
- at 4× only, keyboard `F8` cycles the research fixed-step policy:
  - `proportional` -> fixedDeltaTime ~0.33333334;
  - `vanilla-step` -> fixedDeltaTime 0.083333336;
  - `bounded-step` -> fixedDeltaTime ~0.16666667.

F8 is research-only and is not a proposed production control.

## Minimum user run

1. Start meditation and wait until `sample:done` at 1×.
2. Increase to 2× and wait for `sample:done`.
3. Increase to 4× proportional and wait for `sample:done`.
4. Press F8 -> 4× vanilla-step; wait for `sample:done`.
5. Press F8 -> 4× bounded-step; wait for `sample:done`.
6. Use the normal wake control.
7. Start meditation again with a gamepad and press D-pad Right then D-pad Left once; wait for at least one completed sample and wake normally.
8. Return the BepInEx `LogOutput.log`.

If a visually obvious movement/collision/path anomaly occurs in any 4× policy, mention which policy was shown in the tip. No other manual diagnosis is required.

## Save safety

The harness changes only session timing/UI state.

If WaitingGUI is hidden without its normal StopWaiting path and timing remains modified, the harness first logs the native stuck values and then performs a safety reset to `timeScale=1` / `fixedDeltaTime=0.016666668`.

On plugin destruction while a test is active, it also performs the same safety cleanup.
