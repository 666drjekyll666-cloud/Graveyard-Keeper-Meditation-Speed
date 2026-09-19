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
- the normal wake/exit input is `GameKey.Interaction`.

## Native simulation behavior

`WaitingGUI.Update()` restores energy and HP from scaled `Time.deltaTime`.

`EnvironmentEngine.Update()` advances world time using scaled `Time.deltaTime` (`_cur_time += deltaTime / 225f`) and performs normal end-of-day processing when the day rolls over.

Therefore the preferred architecture is to change meditation's native time scale rather than separately rewriting world time, resource recovery, crops, NPC schedules, crafting, weather, or other simulation systems.

User-facing speed mapping currently accepted for research:

| UI speed | Unity timeScale |
| --- | ---: |
| 1× | 10 |
| 2× | 20 |
| 4× | 40 |

## Input evidence

`BaseGUI` routes native game keys while a GUI is active.

Candidate meditation speed controls:
- `GameKey.SliderDec` = A / Left Arrow / D-pad Left
- `GameKey.SliderInc` = D / Right Arrow / D-pad Right

`WaitingGUI` does not currently assign meditation-specific behavior to these controls.

The exact narrowest production integration seam is still under investigation; do not lock a broad `WaitingGUI.Update()` input poll merely because it is easy.

## Prior art

Current `Exhaust-less` from `p1xel8ted/Graveyard-Keeper-Mods` patches `WaitingGUI.Update()` and adds HP/energy directly. That is not the same semantic model as accelerating vanilla meditation's whole scaled-time simulation.

Do not copy that implementation unless scope changes.

## Open evidence gate: fixed timestep

Normal gameplay:
- `timeScale = 1`
- `fixedDeltaTime = 0.016666668`

Vanilla meditation:
- `timeScale = 10`
- `fixedDeltaTime = 0.083333336`

Approximate FixedUpdate demand in real time is therefore:
- normal: ~60/s
- vanilla meditation: ~120/s

If 2×/4× changed only `timeScale`, the theoretical demand would rise to roughly:
- 2×: ~240/s
- 4×: ~480/s

A candidate policy is to scale `fixedDeltaTime` with the selected meditation multiplier:
- 1×: 0.083333336
- 2×: ~0.16666667
- 4×: ~0.33333334

That would preserve approximately the vanilla-meditation FixedUpdate frequency (~120 real-time calls/s), but this is **not yet accepted runtime behavior**. It must be checked against relevant game systems before production release.

## Acceptance status

Accepted:
- native owner and lifecycle;
- 1×/2×/4× timeScale mapping as the research target;
- host-native architecture direction;
- native input candidates.

Open:
- fixed timestep policy;
- exact UI/input Harmony seam;
- lifecycle fallback needed for abnormal GUI/session exits;
- compatibility behavior with other time/meditation mods.
