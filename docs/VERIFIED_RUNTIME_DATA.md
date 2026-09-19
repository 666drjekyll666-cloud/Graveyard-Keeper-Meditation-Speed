# Verified Runtime Data

## Target

- Game: Graveyard Keeper 1.407
- Runtime: PC / Unity / BepInEx + Harmony
- Evidence snapshot: 2026-09-19

## Version evidence

The inspected 1.407 decompilation reports:
- `LazyConsts.VERSION = 1.407f`
- `LazyConsts.VERSION_INT = 1407f`

Primary inspected evidence repository: `Kupie/GYK_DECOMP`, commit `6abf79199d92482af1c7573870dd9a20ec2270b9`.

This repository does not contain copied game source.

## Meditation owner and lifecycle

Verified in Graveyard Keeper 1.407:

- meditation waiting is owned by `WaitingGUI`;
- when waiting becomes active, vanilla sets `Time.timeScale = 10f`;
- vanilla sets `Time.fixedDeltaTime = 0.083333336f`;
- `WaitingGUI.StopWaiting()` restores `Time.timeScale = 1f`;
- `WaitingGUI.StopWaiting()` restores `Time.fixedDeltaTime = 0.016666668f`;
- the normal wake/exit input is `GameKey.Interaction`;
- the Back handler also routes through `StopWaiting()`.

## Native simulation behavior

`WaitingGUI.Update()` restores energy and HP from scaled `Time.deltaTime`.

`EnvironmentEngine.Update()` advances world time using scaled `Time.deltaTime` (`_cur_time += deltaTime / 225f`) and performs normal end-of-day processing when the day rolls over.

Therefore the preferred architecture is to change meditation's native time scale rather than separately rewriting world time, resource recovery, crops, NPC schedules, crafting, weather, or other simulation systems.

User-facing speed mapping accepted for research:

| UI speed | Unity timeScale |
| --- | ---: |
| 1× | 10 |
| 2× | 20 |
| 4× | 40 |

## Fixed-step static evidence

The meaningful direct fixed-step surface found in 1.407 is narrow:
- `CustomUpdateManager.FixedUpdate()` -> active WGO `CustomFixedUpdate()`;
- world-object fixed components: `MovementComponent` and `KickComponent`;
- separate `DropsList.FixedUpdate()`.

Some movement/kick/drop behavior is per fixed call rather than fully delta-normalized. Therefore `fixedDeltaTime` changes both scheduling cost and some transient physics behavior.

Approximate scheduling:

| State | timeScale | fixedDeltaTime | FixedUpdate / real sec |
| --- | ---: | ---: | ---: |
| Normal | 1 | 0.016666668 | ~60 |
| Vanilla meditation | 10 | 0.083333336 | ~120 |
| 2× unchanged fixed step | 20 | 0.083333336 | ~240 |
| 4× unchanged fixed step | 40 | 0.083333336 | ~480 |
| 2× proportional | 20 | 0.16666667 | ~120 |
| 4× proportional | 40 | 0.33333334 | ~120 |

The proportional policy is now the leading candidate, but remains runtime-unaccepted because the 4× game-time fixed step is coarser.

Detailed evidence: `docs/FIXED_TIMESTEP_AND_UI_RESEARCH.md`.

## Input evidence

`BaseGUI` routes native game keys while a GUI is active through `gamekey_delegates`.

Candidate meditation speed controls:
- `GameKey.SliderDec` = A / Left Arrow / D-pad Left
- `GameKey.SliderInc` = D / Right Arrow / D-pad Right

The base `OnPressedSliderDec/Inc` handlers are virtual and currently return false; no stock overrides were found.

Current static-preferred seam: patch those two semantic handlers and gate behavior to active `WaitingGUI` state. This avoids independent per-frame input polling and reflection into the delegate dictionary.

The same physical controls also emit logical Left/Right. Runtime must verify that WaitingGUI's gamepad navigation does not act on the paired logical key.

## UI evidence

WaitingGUI prints its vanilla wake tip only after entering `Waiting` and setting vanilla timeScale/fixedDeltaTime.

Current static-preferred one-shot presentation seam: exact-instance-gated postfix on the single-tip `ButtonTipsStr.Print(GameKeyTip)` call used by WaitingGUI, preserving the already rendered wake text and adding native SliderDec/SliderInc icons plus the current speed.

This avoids a permanent UI poll but remains runtime-unaccepted because `ButtonTipsStr` is shared infrastructure.

## Compatibility evidence

### Exhaust-less
Current `p1xel8ted/Graveyard-Keeper-Mods` patches `WaitingGUI.Update()` and directly adds HP/energy using scaled `Time.deltaTime`. Higher meditation timeScale will therefore also accelerate its added recovery.

### Where's Ma Storage
Current source patches `WaitingGUI.Open` only to invalidate its own inventory cache. No timing mutation found.

### Back From The Grave
Current source owns fast-forward timing for `SleepGUI`. No `WaitingGUI` patch was found.

## Acceptance status

Accepted:
- native owner and normal lifecycle;
- 1×/2×/4× timeScale mapping as the research target;
- host-native architecture direction;
- native input candidates;
- static inventory of relevant fixed-step consumers;
- static compatibility findings above.

Runtime-open:
- final fixed timestep policy;
- exact UI/input seam acceptance;
- paired gamepad Left/Right behavior;
- whether abnormal WaitingGUI exits require any fallback cleanup.
