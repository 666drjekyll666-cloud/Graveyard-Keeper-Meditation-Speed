# Fixed Timestep and UI/Input Static Research — 2026-09-19

Status: **static evidence complete; runtime validation still required for the fixed-timestep policy and the proposed input/UI seams.**

Game target: Graveyard Keeper 1.407.

Primary game-code evidence: `Kupie/GYK_DECOMP` commit `6abf79199d92482af1c7573870dd9a20ec2270b9`.

No decompiled game source is copied into this repository.

## 1. FixedUpdate ownership in 1.407

The game's direct fixed-step surface is much smaller than a broad Unity-wide assumption would suggest.

`CustomUpdateManager.FixedUpdate()`:
- runs only while the game is started and not paused;
- iterates active `WorldGameObject` instances;
- calls `WorldGameObject.CustomFixedUpdate()`.

`WorldGameObject.CustomFixedUpdate()` delegates to `ComponentsManager.FixedUpdate()`.

Only two world-object components override `HasFixedUpdate()`:
- `MovementComponent`;
- `KickComponent`.

A separate `DropsList.FixedUpdate()` advances active drop objects.

### MovementComponent

Movement uses the fixed-step argument in actual displacement and pathfinding thresholds. Some acceleration/friction operations are per fixed call rather than fully normalized by elapsed time.

Consequences:
- keeping the vanilla meditation fixed step while raising `timeScale` preserves vanilla per-step spatial granularity but increases real-time FixedUpdate frequency;
- increasing `fixedDeltaTime` with the speed multiplier preserves the real-time callback budget but makes each simulated movement step coarser.

### KickComponent / DropsList

`KickComponent` contains per-step friction such as `delta_vec *= 0.96f`, then applies displacement using the fixed delta.

`DropResGameObject.FixedUpdateMe()` also performs some per-fixed-call state changes, including collider-radius growth.

Therefore `fixedDeltaTime` is not merely a CPU knob. Different policies change both scheduling cost and some transient physics behavior.

## 2. Unity scheduling semantics

Unity documents that `fixedDeltaTime` is an interval in **game time affected by `timeScale`**.

Unity's timeScale guidance also demonstrates multiplying `fixedDeltaTime` by the time scale when the goal is to keep the number of FixedUpdate callbacks per real-time interval approximately constant. Unity explicitly notes that whether this adjustment is desirable is game-specific.

For Graveyard Keeper:

| State | timeScale | fixedDeltaTime | Approx. fixed calls / real second |
| --- | ---: | ---: | ---: |
| Normal gameplay | 1 | 0.016666668 | 60 |
| Vanilla meditation | 10 | 0.083333336 | 120 |
| 2×, unchanged med step | 20 | 0.083333336 | 240 |
| 4×, unchanged med step | 40 | 0.083333336 | 480 |
| 2×, proportional step | 20 | 0.16666667 | 120 |
| 4×, proportional step | 40 | 0.33333334 | 120 |
| 4×, bounded middle step | 40 | 0.16666667 | 240 |

The proportional policy is the leading candidate because it preserves vanilla-meditation callback demand and keeps per-fixed-call effects approximately stable in real time.

It is **not yet accepted** because the 4× step of ~0.3333 game seconds is four times coarser than vanilla meditation and may affect movement/path/collision granularity.

Runtime comparison is required before freezing the policy.

## 3. World-time semantics

`EnvironmentEngine.Update()` advances `_cur_time` from scaled `Time.deltaTime` and performs normal end-of-day processing.

The fixed-step policy therefore should not alter the intended world-time multiplier directly. A runtime probe will still verify that measured world progression tracks the selected 1×/2×/4× meditation multiplier and survives a day rollover.

## 4. Native input seam

`BaseGUI.Init()` creates a native `gamekey_delegates` map. It includes:
- `GameKey.SliderDec -> OnPressedSliderDec`;
- `GameKey.SliderInc -> OnPressedSliderInc`.

The base slider handlers are virtual and currently return `false`. No stock override was found.

This gives a low-recurring-cost candidate seam:

- Harmony-patch `BaseGUI.OnPressedSliderDec()` and `BaseGUI.OnPressedSliderInc()`;
- act only when `__instance` is the active `WaitingGUI` in state `Waiting`;
- change the selected meditation speed;
- return `true` so BaseGUI consumes the corresponding slider logical key.

Advantages:
- reuses Graveyard Keeper's own keyboard/gamepad abstraction;
- no independent keyboard hotkeys;
- no new `WaitingGUI.Update()` polling;
- no reflection into the protected `gamekey_delegates` field;
- effect is gated to one GUI type/state.

### Shared physical bindings

The same physical inputs also map to logical Left/Right:
- A / Left Arrow / D-pad Left -> both `Left` and `SliderDec`;
- D / Right Arrow / D-pad Right -> both `Right` and `SliderInc`.

BaseGUI will therefore see both logical keys in the same frame.

For keyboard input, BaseGUI's Left/Right navigation handlers return false when gamepad mode is not active.

For gamepad input, whether the paired Left/Right handler has any effect depends on the WaitingGUI gamepad-navigation state. This is a runtime acceptance item. Production must not rely on dictionary iteration order.

## 5. Visible speed indicator

WaitingGUI's vanilla transition into active waiting performs, in order:
1. state -> `Waiting`;
2. invokes the start callback;
3. sets vanilla `timeScale`;
4. sets vanilla `fixedDeltaTime`;
5. calls `button_tips.Print(GameKeyTip.Interaction/"wake up")`.

That final print is the first narrow event after vanilla has established the waiting clock.

A candidate one-shot presentation seam is a postfix on the single-tip `ButtonTipsStr.Print(GameKeyTip)` overload, gated by exact identity with `WaitingGUI.button_tips`.

The postfix can:
- preserve the vanilla localized wake text already rendered;
- add native `GameKeyTip.GetIcon(SliderDec/SliderInc)` icons;
- insert the current raw speed text;
- perform no recurring polling.

Speed changes can redraw the same label immediately from the slider handlers.

This target is shared infrastructure, so runtime validation must confirm the exact-instance gate has no unrelated UI effect. A broad per-frame UI patch is not justified while this event seam works.

## 6. Lifecycle findings

Vanilla `WaitingGUI.StopWaiting()` synchronously restores:
- `Time.timeScale = 1f`;
- `Time.fixedDeltaTime = 0.016666668f`;

before the fade-out and `Hide(false)`.

Normal start/load initialization also writes the normal `fixedDeltaTime`.

Static inspection does not show a universal normal-`timeScale` reset on every conceivable load/scene path. That does **not** justify a global watchdog.

Runtime evidence should first test:
- Interaction wake;
- Back wake;
- repeated open/change/close;
- exact values at `StopWaiting` completion and `WaitingGUI.Hide`;
- one safe load/scene lifecycle case if it can occur while waiting.

Only add fallback cleanup if an actual bypass of vanilla restoration is demonstrated.

## 7. Compatibility research

### Exhaust-less

Current `p1xel8ted/Graveyard-Keeper-Mods` patches `WaitingGUI.Update()` for:
- auto-wake;
- direct HP/energy acceleration.

Its speed-up path adds HP/energy using scaled `Time.deltaTime`.

Implication: when both mods are enabled, Meditation Speed's higher `timeScale` also scales Exhaust-less's extra recovery. This is a behavioral interaction even though the Harmony seams need not directly conflict.

Do not claim transparent compatibility with Exhaust-less Speed Up Meditation enabled.

### Where's Ma Storage

Current source has a `WaitingGUI.Open` prefix used only to invalidate its own inventory cache. No timing or meditation-state mutation was found.

No direct conflict is currently indicated.

### Back From The Grave

Current multiplayer timing ownership is attached to `SleepGUI`, including its own sleep `timeScale/fixedDeltaTime` policy.

No `WaitingGUI` patch was found in the inspected current source. No direct meditation-clock conflict is currently indicated.

## 8. Runtime gate

One research-only harness should now prove:
- actual fixed-callback rate under the candidate policies;
- frame stability during short probes;
- measured world-time progression ratio;
- 4× movement/physics does not show an obvious failure under the proportional candidate;
- native SliderDec/SliderInc dispatch works on keyboard and gamepad;
- paired Left/Right produces no unwanted WaitingGUI action;
- the speed indicator renders correctly;
- wake/Back restores normal timing.

Until that evidence exists:
- do not create `dev/0.1.0`;
- do not freeze the fixed-timestep policy;
- do not call the UI/input seam production-accepted.
