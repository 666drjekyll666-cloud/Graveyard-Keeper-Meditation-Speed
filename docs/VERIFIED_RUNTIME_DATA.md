# Verified Runtime Data

## Target

- Game: Graveyard Keeper 1.407
- Runtime: PC / Unity / BepInEx + Harmony
- Evidence accepted: 2026-09-20

Primary static evidence repository: `Kupie/GYK_DECOMP`, commit `6abf79199d92482af1c7573870dd9a20ec2270b9`.

Runtime acceptance artifact: Research Harness 0.0.1, source SHA `3e5fd476584eb8749a98ca58b1b3ffa575b1d9bc`, DLL SHA-256 `b5dcffd79c099d33b068e9345ec30f5024ec33ee4babde7af884f84eccf83b92`.

This repository does not contain copied game source.

## Meditation owner and lifecycle

Verified:
- meditation waiting is owned by `WaitingGUI`;
- vanilla waiting sets `Time.timeScale = 10f`;
- vanilla waiting sets `Time.fixedDeltaTime = 0.083333336f`;
- `WaitingGUI.StopWaiting()` restores `Time.timeScale = 1f`;
- `WaitingGUI.StopWaiting()` restores `Time.fixedDeltaTime = 0.016666668f`;
- Interaction wakes normally;
- Back routes through `StopWaiting()`.

Runtime normal-exit probes returned `restore_ok=true`.

## Accepted production speed mapping

| UI speed | timeScale | fixedDeltaTime |
| --- | ---: | ---: |
| 1× | 10 | 0.083333336 |
| 2× | 20 | 0.16666667 |
| 4× | 40 | 0.33333334 |

The proportional fixed-step policy preserves approximately the vanilla-meditation real-time fixed callback rate (~120/s).

Runtime at 4×:
- proportional: ~119.94 and ~120.18 fixed callbacks/s;
- bounded step: ~239.92/s;
- vanilla meditation step: ~480.06/s.

The user reported no visual/simulation anomaly under any tested policy. The 4× proportional boundary is accepted.

## Native simulation behavior

`WaitingGUI.Update()` restores energy and HP from scaled `Time.deltaTime`.

`EnvironmentEngine.Update()` advances world time from scaled `Time.deltaTime`.

Therefore production changes meditation's native timing inputs only. It does not reimplement world time, recovery, crops, NPCs, crafting, weather, or other simulation systems.

## Input

Accepted native controls:
- decrease: `GameKey.SliderDec` = A / Left Arrow / D-pad Left;
- increase: `GameKey.SliderInc` = D / Right Arrow / D-pad Right.

Accepted seam:
- semantic `BaseGUI.OnPressedSliderDec/Inc` patches;
- effect only for active WaitingGUI in Waiting state;
- no custom recurring keyboard/gamepad poll.

Runtime found paired gamepad Left/Right navigation also fires from the same D-pad input.

Follow-up static inspection after 0.1.0 user feedback found the root cause of rapid multi-step speed changes: `LazyInput` treats SliderDec/SliderInc as hold-repeat keys. It starts repeating after 0.3 and then every 0.07, while `UpdateHolded` subtracts scaled `Time.deltaTime`. During meditation those intervals become extremely short in real time.

Accepted mitigation for 0.1.1:
- after handling one SliderDec/SliderInc event, call native `LazyInput.WaitForRelease` for that slider key;
- also wait for release of the paired Left/Right key for the same physical direction;
- retain WaitingGUI-only Left/Right suppression as a defensive ordering guard;
- no custom polling or arbitrary debounce timer.

This gives one physical press -> one speed step and reuses the host's own release lifecycle.

## UI

Accepted lifecycle event:
- the WaitingGUI single-tip `ButtonTipsStr.Print(GameKeyTip)` occurs after vanilla installs waiting timing.

Production should:
- gate by active WaitingGUI + exact button-tips instance;
- preserve the localized vanilla wake text;
- show the current `1×/2×/4×` with language-neutral directional chevrons; raw SliderDec/SliderInc gamepad tokens render as `(DLeft)/(DRight)` in this UI font and are not suitable for final presentation;
- hide the unavailable direction at the speed boundaries: no left arrow at 1× and no right arrow at 4×; retain spacing so adjacent vanilla tip content does not shift;
- improve readability relative to the research harness; user feedback was that the harness text was too small;
- redraw only on speed changes.

Do not use the harness's broad inherited `Open` hook in production.

## Longer Days runtime compatibility

The acceptance run used Longer Days 1.7.1 with Day Length = 675.

Adjusted expected world rate:
- 1×: ~6.6667;
- 4×: ~26.6667.

Observed clean samples:
- 1×: 6.6613;
- 4×: 26.6164 / 26.6666 / 26.6773 / 26.6415.

This supports compatibility with Longer Days' changed day length: meditation acceleration composes with it rather than overriding it.

## Other compatibility evidence

- Exhaust-less: direct extra HP/energy uses scaled `Time.deltaTime`; enabling its meditation speed-up alongside this mod will compound recovery behavior.
- Where's Ma Storage: current WaitingGUI.Open patch only invalidates its own inventory cache; no timing conflict found.
- Back From The Grave: current fast-forward timing targets SleepGUI; no WaitingGUI timing patch found.

## Acceptance status

Research gates closed for production candidate implementation:
- fixed timestep: accepted;
- native slider input: accepted;
- gamepad duplicate navigation: mitigation defined;
- waiting-start UI seam: accepted with readability adjustment;
- normal restoration: accepted;
- narrow abnormal-exit cleanup: required defensively, no global watchdog.
