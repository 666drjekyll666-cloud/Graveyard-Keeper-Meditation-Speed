# Meditation Speed — Project Rules

This repository follows the canonical global engineering contract in `NikichMods/DevRules`.

Before substantive technical work, read the current:
- `ENGINEERING_RULES.md`
- `CI_POLICY.md`
- `GIT_WORKFLOW.md`
- `PROJECT_BOOTSTRAP.md`
- `RUNTIME_TEST_HARNESS.md` when installed-game runtime evidence is required.

Global DevRules are authoritative. This file contains only project-specific facts, constraints, and accepted decisions.

## Project identity

- Project: **Meditation Speed**
- Repository: `NikichMods/Graveyard-Keeper-Meditation-Speed`
- Game: **Graveyard Keeper 1.407**
- Platform: PC, BepInEx/Harmony
- Purpose: let the player change meditation speed from the meditation interface, initially using vanilla-relative 1×, 2× and 4× speeds.

Do not expand this into a general game-speed, sleep-speed, or time-control mod without explicit approval.

## Mandatory start-of-work checks

Before substantive implementation:
1. inspect the current repository and this `AGENTS.md`;
2. read current DevRules;
3. inspect accepted research/docs before repeating investigation;
4. verify Graveyard Keeper identifiers and lifecycle assumptions against 1.407 evidence;
5. inspect interacting mods before claiming compatibility or conflict.

Do not implement from chat memory when repository or runtime evidence can answer the question.

## User-operation boundary

Follow DevRules `ENGINEERING_RULES.md` section **User burden and decision closure**. Perform programming, repository, Git, build, decompilation, artifact retrieval, and repetitive mechanical diagnostics directly with tools when possible. Ask the user only for product decisions, permissions/credentials, or installed-game runtime evidence that genuinely requires the user's environment.

## Verified vanilla model

Current accepted Graveyard Keeper 1.407 evidence establishes:

- meditation is owned by `WaitingGUI`;
- vanilla waiting begins with `Time.timeScale = 10f`;
- vanilla waiting uses `Time.fixedDeltaTime = 0.083333336f`;
- `WaitingGUI.StopWaiting()` restores `Time.timeScale = 1f` and `Time.fixedDeltaTime = 0.016666668f`;
- meditation HP/energy restoration uses scaled `Time.deltaTime`;
- world-time progression in `EnvironmentEngine` also uses scaled `Time.deltaTime`;
- therefore changing the meditation time scale can accelerate the existing native simulation without separately modifying day progression, recovery, crops, NPCs, crafting, or other world systems.

Treat vanilla meditation as user-facing **1×**. Accepted stable mappings:
- 1× = timeScale 10, fixedDeltaTime 0.083333336
- 2× = timeScale 20, fixedDeltaTime 0.16666667
- 4× = timeScale 40, fixedDeltaTime 0.33333334

Do not change these mappings silently. The proportional fixed-timestep policy was accepted from real-game runtime evidence.

## Architecture contract

Follow the DevRules host-native-first gate.

Preferred end state:

`vanilla WaitingGUI owns meditation -> mod supplies selected acceleration -> vanilla systems consume scaled Time.deltaTime`

Do not:
- reimplement world-time progression;
- duplicate vanilla recovery formulas;
- patch `EnvironmentEngine.Update()` merely to accelerate meditation;
- alter sleep speed or ordinary gameplay time;
- introduce permanent global polling when a lifecycle/input seam can do the same job.

Use the narrowest proven Harmony seam. If a transpiler, reflection, or private/internal dependency is required, isolate it and validate the target structure explicitly.

The `Time.fixedDeltaTime` evidence gate is closed. Stable production scales it proportionally with meditation speed so real-time FixedUpdate demand stays close to vanilla meditation. Reopen this only if new runtime evidence contradicts the accepted policy.

## Input and UI

Prefer Graveyard Keeper's native input abstraction.

Verified candidate controls:
- `GameKey.SliderDec`: keyboard A / Left Arrow; gamepad D-pad Left
- `GameKey.SliderInc`: keyboard D / Right Arrow; gamepad D-pad Right

Accepted UX:

`1×  >` -> `<  2×  >` -> `<  4×`

One physical press must produce one speed step. Use the host's release-gating semantics rather than scaled-time debounce/polling. The existing wake/exit control must remain functional and vanilla-compatible.

Do not add mouse-only controls or regress gamepad support.

## Compatibility and safety

- Do not mutate save data merely to control meditation speed.
- Removing the DLL must leave saves valid and ordinary game timing vanilla.
- Closing meditation, save reloads, scene changes, failures, or other relevant exits must not leave `Time.timeScale` or `Time.fixedDeltaTime` modified.
- Inspect other mods that modify `WaitingGUI`, `Time.timeScale`, meditation recovery, or global game speed before documenting compatibility.
- `Exhaust-less` is relevant prior art: its meditation-speed feature directly increases HP/energy in `WaitingGUI.Update()`; do not copy that behavior unless project scope changes.

## Evidence and proprietary material

Reverse-engineered/decompiled game code may be inspected as research evidence but must not be copied into this production repository.

Preserve only minimum derived facts, identifiers, hashes, observations, and our own implementation needed for reproducibility.

For uncertain runtime behavior, use a narrow disposable diagnostic or user-operated runtime test rather than guessing.

## Git, CI and acceptance

- `main` is the accepted stable line.
- Current accepted stable version: **1.0.0**.
- Research and unaccepted runtime candidates stay off `main`.
- Use `research/<topic>` for evidence gathering and `dev/<version>` when a build-bearing implementation line exists.
- Do not consume a numbered version for research-only work.
- Do not spend hosted CI on routine research, documentation, bookkeeping, or intermediate commits.
- Before handing over a numbered DLL, require a clean Release build from the exact candidate source and record source SHA and artifact hash.
- A handed numbered binary is immutable.
- Accepted public releases follow the normal DevRules GitHub Release workflow.
- Stable source is MIT-licensed; reverse-engineered/decompiled game material remains research evidence only and is not covered as project-owned source.

## Long-lived sources of truth

Maintain:
- `AGENTS.md`
- `README.md`
- `CHANGELOG.md`
- `docs/VERIFIED_RUNTIME_DATA.md`
- `docs/RESEARCH_PLAN.md`
- `docs/TEST_BUILD_LOG.md`

When chat memory conflicts with accepted repository evidence, investigate the conflict before changing production code.

## Shared Graveyard Keeper research

Cross-project Graveyard Keeper 1.407 host/runtime research is centralized in `NikichMods/GraveyardKeeperResearch`.

Before starting a fresh investigation into vanilla/game-engine/UI/NGUI/data/lifecycle behavior:

1. read this repository's own canonical verified-data / architecture docs first;
2. consult `NikichMods/GraveyardKeeperResearch/docs/RESEARCH_INDEX.md` and the linked shared knowledge documents;
3. search accepted local/shared test evidence and relevant history if the result has not yet been promoted;
4. perform new static/runtime research or a probe only if the question remains open.

Project-specific mechanics, product/UX decisions, release state, and build acceptance remain canonical in this repository. Reusable host/runtime facts that can serve multiple Graveyard Keeper mods should be promoted back into the shared research repository after acceptance rather than left only in chat, commit history, or a test log.

