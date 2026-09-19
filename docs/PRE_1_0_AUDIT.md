# Pre-1.0 Engineering Audit

Date: 2026-09-20

Status: **runtime architecture accepted; startup fail-closed hardening implemented and cleanly built as the 1.0.0 release candidate. Runtime smoke acceptance remains.**

Target: Graveyard Keeper 1.407.

## Evidence reviewed

- production source and release documentation on the current stable line;
- accepted Research Harness 0.0.1 evidence and 0.1.0–0.1.2 runtime acceptance history;
- Graveyard Keeper 1.407 evidence from `Kupie/GYK_DECOMP` commit `6abf79199d92482af1c7573870dd9a20ec2270b9`;
- current relevant p1xel8ted mod source at `Graveyard-Keeper-Mods` commit `ebac55b3fe58402ae7cd5c061d9eb5b0c8e610eb`;
- HarmonyX `PatchAll` behavior and documentation.

## Architecture

Verdict: **accepted; no redesign recommended.**

The production path remains the least-sufficient host-native mechanism:

`WaitingGUI owns meditation -> Meditation Speed selects timeScale/fixedDeltaTime -> vanilla systems consume scaled time`.

The mod does not replace EnvironmentEngine progression, meditation recovery formulas, crop logic, NPC schedules, crafting, sleep, or ordinary gameplay time.

The accepted speed mapping remains:
- 1× = timeScale 10 / fixedDeltaTime 0.083333336;
- 2× = timeScale 20 / fixedDeltaTime 0.16666667;
- 4× = timeScale 40 / fixedDeltaTime 0.33333334.

No additional simulation patch is justified.

## Fixed timestep and performance

Verdict: **accepted; proportional policy remains the best production trade-off within current evidence.**

Runtime measurements at 4×:
- proportional fixed step: ~120 fixed callbacks per real second;
- bounded 0.16666667 step: ~240/s;
- vanilla meditation 0.083333336 step: ~480/s.

Static inspection confirms that fixed-step behavior includes movement/pathing, kicks, and drops, with some per-call behavior that is not fully delta-normalized. Therefore no fixed-step policy is semantically free.

The accepted proportional policy:
- keeps real-time FixedUpdate demand close to vanilla meditation;
- avoids a 2× or 4× multiplication of fixed-update workload;
- was exercised at the 4× boundary in the real game without a reported simulation anomaly.

Changing to the bounded or vanilla-step alternatives before 1.0.0 would increase recurring CPU work without new evidence that the accepted proportional policy is failing.

The mod itself adds no custom Update/FixedUpdate loop, no polling, no recurring scans, and no background work. Its Harmony hooks run on discrete GUI/input/lifecycle events. Reflection targets are resolved and cached at initialization; reflection invocation occurs only on user speed changes or GUI lifecycle events.

The unavoidable increase in game-world event density per real second at 2×/4× is the requested behavior of accelerating the native simulation, not additional mod-side work.

## Input

Verdict: **accepted.**

Production uses the game's semantic SliderDec/SliderInc handlers. The native hold-repeat problem was traced to `LazyInput.UpdateHolded(Time.deltaTime)`, where the 0.3-second initial delay and 0.07-second slider repeat interval are scaled by accelerated time.

Production reuses native `LazyInput.WaitForRelease` for the slider key and paired Left/Right key. This gives one physical press -> one speed step without a custom debounce timer or per-frame polling.

WaitingGUI-only Left/Right suppression remains justified as an ordering guard because the same physical D-pad/keyboard direction can emit both slider and navigation semantic keys.

## UI

Verdict: **accepted.**

The UI uses WaitingGUI's existing button-tip label after vanilla installs meditation timing. It does not create a new canvas, texture atlas, image resource, or recurring redraw path.

The accepted presentation is:
- 1×: right direction only;
- 2×: both directions;
- 4×: left direction only.

The user accepted the size, readability, boundary behavior, and keyboard/gamepad presentation in the installed game.

## Lifecycle and global timing safety

Verdict: **accepted for the supported runtime.**

Vanilla `WaitingGUI.StopWaiting()` restores `timeScale=1` and `fixedDeltaTime=0.016666668` synchronously before clearing tips or starting the fade-out. Runtime evidence verified normal restoration.

Production additionally:
- restores normal timing on a WaitingGUI hide that bypasses StopWaiting when the current timing still matches the mod-owned selected pair;
- hands an active meditation session back to vanilla 1× timing if the plugin is destroyed while WaitingGUI is still active;
- otherwise restores ordinary timing when tearing down a non-active owned session.

No global watchdog is justified. It would add recurring work and new timing ownership for a failure mode not observed in the supported runtime.

A further StopWaiting override/fallback was considered and rejected: in 1.407 the native method performs both timing restores before later side effects, and the inspected current interacting mods do not skip or replace StopWaiting. Adding unconditional restoration would increase conflict risk with unrelated global-speed mods without improving the proven path.

## Save safety

Verdict: **accepted.**

The plugin has no save schema, serialized state, configuration-backed gameplay state, inventory mutation, or save-data write.

Removing the DLL does not require migration. Selected meditation speed is session-local and resets to 1× on the next meditation.

## Compatibility

Verdict: **accepted for documented/current evidence; no universal compatibility claim.**

- Longer Days 1.7.1 was runtime-tested at Day Length 675. World progression matched the Longer-Days-adjusted expected rate, so the multiplier composes with its day-length change.
- Current p1xel8ted source has no SliderDec/SliderInc or ButtonTipsStr patch competing with this mod.
- Where's Ma Storage has a BaseGUI.Hide postfix and WaitingGUI.Open prefix but no meditation timing ownership; no direct conflict is indicated.
- Exhaust-less patches WaitingGUI.Update and can add HP/energy using scaled Time.deltaTime. Its meditation speed-up therefore compounds recovery under higher meditation timeScale; this remains a documented behavioral interaction.
- Pray The Day Away writes Time.timeScale around prayer flow, not WaitingGUI meditation. It is not a direct meditation seam conflict.

Compatibility statements remain scoped to inspected versions/evidence.

## Reflection and version-specific coupling

Verdict: **appropriate and isolated.**

The code intentionally avoids shipping or compiling against copied proprietary Assembly-CSharp material. Version-specific private/internal identifiers are concentrated in `RuntimeBindings`.

All reflection metadata is resolved once at startup. There is no repeated type/member discovery in gameplay loops.

## Startup/failure handling finding

Verdict before hardening: **one real 1.0-quality improvement required.**

0.1.2 used parameterless `Harmony.PatchAll()`. HarmonyX itself documents that this overload can identify the wrong assembly when the caller is inlined and recommends the explicit assembly overload when in doubt.

HarmonyX's assembly overload iterates patch classes; it is not a transactional all-or-nothing operation. If a later target failed on a drifted/unsupported runtime, the 0.1.2 catch disabled the MonoBehaviour but did not immediately unpatch any earlier patch classes that had already succeeded.

Accepted 1.0.0 hardening:
- call `PatchAll(typeof(MeditationSpeedPlugin).Assembly)`;
- on initialization failure, call `UnpatchSelf()` before disabling the plugin;
- log rollback failure separately if rollback itself fails.

This changes failure behavior only. Successful 1.407 meditation behavior is unchanged.

## Build and release engineering

Verdict: **accepted.**

- net472 is appropriate for the game's Mono runtime;
- the accepted 0.1.2 candidate built Release with 0 warnings / 0 errors;
- stable 0.1.2 release reused the exact tested DLL and verified its SHA-256 instead of rebuilding different bytes;
- no recurring hosted CI workflow remains after release;
- no debug/probe harness code is present in production.

For 1.0.0, the exact candidate source must receive one clean Release build and the resulting DLL must be runtime-smoke-tested because startup code changed.

## Repository hygiene

Verdict: **accepted with one non-runtime note.**

The public README is user-facing; research details are kept in dedicated docs. No TODO/FIXME/debug logging residue was found in production source.

There is currently no LICENSE file. This is not a runtime/release-safety defect: without an explicit license, source reuse is simply not granted by default. Add a license only if explicit reuse permissions are desired.

## Final audit decision

After the startup hardening above, there is no evidence-backed architecture, performance, save-safety, lifecycle, input, UI, or compatibility change that should be made before 1.0.0.

Do **not**:
- add polling/watchdogs;
- patch EnvironmentEngine;
- add custom image/UI infrastructure for the accepted indicator;
- increase fixed-update frequency without contrary runtime evidence;
- persist meditation speed into save/config state without a new product decision;
- broaden scope into sleep/general game speed.

The 1.0.0 candidate built Release with 0 warnings / 0 errors from source SHA `37964f2e17d52d2d81df9265778ff2b5ef21fe80`; DLL SHA-256 is `54888d085cefe957fc913e013b577da3890d31345956e62baeaf783c4af2bdfa`.

The only remaining acceptance gate is a short installed-game smoke test of that exact DLL to confirm plugin load, one-step input, 1×/2×/4× timing, UI, and wake restoration after the startup-only hardening.
