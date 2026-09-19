# Fixed Timestep and UI/Input Research — accepted 2026-09-20

Status: **accepted for production implementation**.

Target: Graveyard Keeper 1.407.

Primary static evidence: `Kupie/GYK_DECOMP` commit `6abf79199d92482af1c7573870dd9a20ec2270b9`.

Runtime evidence: Research Harness 0.0.1, source SHA `3e5fd476584eb8749a98ca58b1b3ffa575b1d9bc`, DLL SHA-256 `b5dcffd79c099d33b068e9345ec30f5024ec33ee4babde7af884f84eccf83b92`.

No decompiled game source is copied into this repository.

## Accepted fixed-timestep policy

Production mapping:

| UI speed | timeScale | fixedDeltaTime | Target fixed callbacks / real second |
| --- | ---: | ---: | ---: |
| 1× | 10 | 0.083333336 | ~120 |
| 2× | 20 | 0.16666667 | ~120 |
| 4× | 40 | 0.33333334 | ~120 |

Static inspection found that the meaningful direct fixed-step surface in 1.407 is narrow:
- `CustomUpdateManager.FixedUpdate()` -> active WGO `CustomFixedUpdate()`;
- fixed WGO components: `MovementComponent` and `KickComponent`;
- separate `DropsList.FixedUpdate()`.

Some movement/kick/drop behavior is per fixed call rather than fully delta-normalized, so increasing the fixed step is a real simulation trade-off rather than only a CPU optimization.

The runtime comparison resolved that trade-off:

| Probe | Measured fixed callbacks / real second | User-visible result |
| --- | ---: | --- |
| 4× proportional, fixed 0.33333334 | ~119.94 / ~120.18 | no anomaly reported |
| 4× bounded, fixed 0.16666667 | ~239.92 | no anomaly reported |
| 4× vanilla med step, fixed 0.083333336 | ~480.06 | no anomaly reported |

The proportional candidate preserves vanilla-meditation callback demand instead of multiplying fixed-step work by 2×/4×. The highest/coarsest boundary (4×) was exercised twice without a reported movement, collision, NPC, or general simulation problem. Under the test-matrix boundary rule, a separate long 2× probe is not required because 2× uses the same control path and a less coarse fixed step.

Decision: **accept proportional fixedDeltaTime scaling**.

## World-time behavior and Longer Days

The runtime environment had Longer Days 1.7.1 configured to a 675-second day instead of vanilla 450 seconds.

Therefore the harness's vanilla-only `expected_world_rate` field is not the correct absolute expectation for that runtime. With a 1.5× longer day, expected effective world progression is reduced by 450/675 = 2/3.

At 4× the expected effective rate is therefore ~26.6667. Runtime samples measured:
- 26.6164;
- 26.6666;
- 26.6773;
- 26.6415.

A later clean 1× sample measured 6.6613 against a Longer-Days-adjusted target of ~6.6667.

Decision: changing only meditation's native time scale preserves Longer Days semantics. This is positive runtime compatibility evidence for Longer Days 1.7.1.

## Accepted input seam

`BaseGUI.Init()` routes:
- `GameKey.SliderDec -> OnPressedSliderDec`;
- `GameKey.SliderInc -> OnPressedSliderInc`.

Production may patch those semantic handlers, gated to the active `WaitingGUI` in `Waiting` state.

Runtime proved:
- keyboard SliderInc changed 1× -> 2× -> 4×;
- gamepad D-pad changed the same native SliderInc/SliderDec path;
- no independent keyboard polling is required.

### Paired Left/Right finding

The same physical controls also emit logical Left/Right.

Runtime:
- keyboard paired Right returned false;
- gamepad paired Left/Right returned true, meaning WaitingGUI's gamepad navigation path also ran.

No visible defect was reported, but production should not allow a hidden second action.

Decision: in active WaitingGUI, suppress paired gamepad Left/Right navigation while SliderDec/SliderInc owns D-pad Left/Right.

This remains event-driven and adds no permanent polling.

## Accepted waiting-start / presentation seam

WaitingGUI prints its vanilla wake tip after it:
1. enters `Waiting`;
2. installs vanilla `timeScale=10`;
3. installs vanilla `fixedDeltaTime=0.083333336`.

The Research Harness successfully used the single-tip `ButtonTipsStr.Print(GameKeyTip)` event to initialize speed control and redraw the waiting tip without recurring UI work.

Important harness finding: resolving `WaitingGUI.Open` by inherited method name patched the base `BaseGUI.Open` implementation and produced unrelated `event=open` observations for other GUIs. Production must **not** copy that broad Open hook.

Production start detection should instead use the button-tip event and gate it by:
- current `BaseGUI.active_gui` being `WaitingGUI`;
- WaitingGUI state being `Waiting`;
- the printed `ButtonTipsStr` being that active WaitingGUI's own `button_tips`.

User feedback: the harness indicator functioned correctly but its text was too small.

Decision: keep the native button-tip surface, but make the production waiting tip more readable (target ~25% larger than its original waiting-tip text) and use language-neutral speed content such as native left/right icons plus `1×/2×/4×`.

## Lifecycle acceptance

Two tested normal exits recorded:
- pre-stop accelerated timing;
- post-stop `timeScale=1`;
- post-stop `fixedDeltaTime=0.016666668`;
- `restore_ok=true`.

Decision:
- vanilla `WaitingGUI.StopWaiting()` remains the primary restoration owner;
- do not add a recurring watchdog;
- add only narrow defensive cleanup for an active WaitingGUI being hidden without StopWaiting and for plugin teardown, so modified global timing cannot leak outside the session.

## Compatibility notes

### Exhaust-less

Current Exhaust-less adds HP/energy directly from scaled `Time.deltaTime` in `WaitingGUI.Update()`.

Higher meditation timeScale will also accelerate that extra recovery. Do not claim transparent compatibility with Exhaust-less's meditation speed-up enabled.

### Where's Ma Storage

Its current `WaitingGUI.Open` prefix invalidates only its own inventory cache. No timing mutation found.

No direct conflict indicated.

### Back From The Grave

Its current fast-forward timing ownership is attached to `SleepGUI`; no `WaitingGUI` patch was found.

No direct meditation-clock conflict indicated.

### Longer Days

Runtime evidence with Longer Days 1.7.1 / day length 675 showed the expected longer-day world progression at all completed boundary probes. The meditation multiplier composes correctly with its changed day length.

## Production constraints carried forward

- each meditation session starts at vanilla-relative 1×;
- no save-data mutation;
- no global/per-frame input polling;
- no EnvironmentEngine patch;
- no sleep or ordinary-gameplay timing changes;
- wake/Back remain vanilla;
- proportional fixedDeltaTime mapping is frozen unless new runtime evidence disproves it.
