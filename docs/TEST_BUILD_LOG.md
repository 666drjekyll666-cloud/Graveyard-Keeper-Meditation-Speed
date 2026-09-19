# Test Build Log

This file records numbered production candidates and research harnesses handed to the user.

## Research harnesses

### Research Harness 0.0.1 — 2026-09-20

- Status: accepted for its research purpose
- Source branch: `research/fixed-timestep-and-ui-seam`
- Source SHA: `3e5fd476584eb8749a98ca58b1b3ffa575b1d9bc`
- Build mode: Release
- Build result: 0 warnings, 0 errors
- Workflow run: `35471861271`
- Artifact ID: `10593250043`
- Artifact ZIP digest: `sha256:6db7ee732e1b3dc930f6ce4aef13d3f0ff4303a1d788e1cefddd0c327c9bfa1b`
- DLL: `MeditationSpeedResearchHarness.dll`
- DLL SHA-256: `b5dcffd79c099d33b068e9345ec30f5024ec33ee4babde7af884f84eccf83b92`
- Purpose: close the fixed-timestep, native slider-input, visible-speed-indicator, and normal WaitingGUI restoration evidence gates.
- Save safety: no save-data mutation.
- Runtime result:
  - 4× proportional fixed step: ~119.94 / ~120.18 fixed callbacks per real second;
  - 4× bounded step: ~239.92/s;
  - 4× vanilla meditation step: ~480.06/s;
  - normal exits restored `timeScale=1` and `fixedDeltaTime=0.016666668`;
  - native keyboard and gamepad slider input worked;
  - gamepad also fired paired Left/Right navigation, requiring production suppression;
  - Longer Days 1.7.1 at Day Length 675 composed correctly with the multiplier;
  - no visual/simulation problem reported;
  - UI feedback: harness indicator text was too small.
- Production promotion: harness code itself is not production code; accepted conclusions are carried into `dev/0.1.0`.

## Production candidates

### 0.1.0 — 2026-09-20

- Status: superseded
- Source branch: `dev/0.1.0`
- Source SHA: `8c903a317c041d6424c426c3033345a580b8d406`
- Build mode: Release
- Build result: 0 warnings, 0 errors
- Workflow run: `35473451741`
- Artifact ID: `10593191648`
- Artifact ZIP digest: `sha256:8c5555b656fb2799e685efac9434d05b8d7a38317cff5d6a95bb27960f5addbf`
- Artifact: `MeditationSpeed.dll`
- SHA-256: `083095c80aecc4ef3a27eed4bb47e04d3a10c18daefbc2502156fbaa8a46579b`
- Purpose: first production candidate implementing accepted 1×/2×/4× meditation speed control.
- Requested runtime checks:
  - replace/remove Research Harness 0.0.1; do not run both;
  - start meditation and confirm it begins at 1×;
  - keyboard A/Left and D/Right cycle 1× <-> 2× <-> 4×;
  - gamepad D-pad Left/Right cycles speed without visible navigation side effects;
  - speed text is more readable than the research harness;
  - wake/Back remain normal;
  - after wake, ordinary gameplay timing is normal;
  - repeat one meditation session to confirm clean reset to 1×.
- Result:
  - core 1×/2×/4× timing behavior worked;
  - tip size/readability was acceptable;
  - user observed rapid held-input repeat that made 2× difficult to select;
  - gamepad speed controls displayed raw `(DLeft)/(DRight)` tokens and were visually unacceptable.
- Stable promotion: no; superseded by 0.1.1

### 0.1.1 — 2026-09-20

- Status: candidate
- Source branch: `dev/0.1.1`
- Source SHA: `3c0080040c080e16804243fc1061d9be017476ba`
- Build mode: Release
- Build result: 0 warnings, 0 errors
- Workflow run: `35474868310`
- Artifact ID: `10593742451`
- Artifact ZIP digest: `sha256:41fb59cda4d836b5654a217c2d2087ddc59a9b306539f6d38cdcdb66b43cf7e1`
- Artifact: `MeditationSpeed.dll`
- SHA-256: `fb92c10fec47b37d2174b713f060639bf1700ecae886faa68cde15c6ede28c30`
- Purpose: fix scaled LazyInput hold-repeat and replace raw D-pad token presentation.
- Input mechanism:
  - after one native SliderDec/SliderInc action, use `LazyInput.WaitForRelease` for that slider key;
  - gate the paired Left/Right logical key until the same physical direction is released;
  - retain WaitingGUI-only Left/Right suppression as an ordering guard;
  - no per-frame input polling and no arbitrary debounce timer.
- UI: `< 1× >` / `< 2× >` / `< 4× >`; existing accepted tip scale retained.
- Requested runtime checks:
  - replace 0.1.0 with 0.1.1;
  - keyboard and gamepad: each short press moves exactly one step 1× -> 2× -> 4× and back;
  - holding one direction does not auto-repeat until it is released and pressed again;
  - no `(DLeft)/(DRight)` text remains;
  - wake/Back remain normal and a new meditation begins at 1×.
- Result:
  - user confirmed keyboard/gamepad speed selection, one-press-one-step behavior, wake flow, and overall 0.1.1 behavior work correctly;
  - user requested one final presentation refinement: hide the unavailable left arrow at 1× and the unavailable right arrow at 4×.
- Stable promotion: no; behavior accepted, superseded by 0.1.2 presentation refinement

### 0.1.2 — 2026-09-20

- Status: accepted stable
- Source branch: `dev/0.1.2`
- Source SHA: `e0956b732fb73620b6d2942e8538eec18bcd6160`
- Build mode: Release
- Build result: 0 warnings, 0 errors
- Workflow run: `35475192930`
- Artifact ID: `10593268313`
- Artifact ZIP digest: `sha256:7f28eaea024762bffad47ca682a8e4aabaa5e104e0d4555ba35fa15b8f1bdcc5`
- Artifact: `MeditationSpeed.dll`
- SHA-256: `c59d8f29ad97ffb0ff38b7f420b128a0b31c2850bc168c7e597031ffc6de88f0`
- Purpose: final boundary-aware speed-indicator refinement on top of accepted 0.1.1 behavior.
- UI:
  - 1×: no left arrow; right arrow remains;
  - 2×: both arrows remain;
  - 4×: left arrow remains; no right arrow;
  - missing boundary arrows are replaced by spacing so adjacent wake-tip content does not shift.
- Runtime behavior: unchanged from accepted 0.1.1.
- Requested runtime check:
  - replace 0.1.1 with 0.1.2;
  - confirm the three indicator states visually;
  - confirm switching still works one press per step.
- Result:
  - user confirmed all three boundary-aware indicator states are visually correct;
  - switching remains exactly one step per physical press;
  - controls are comfortable on keyboard and gamepad;
  - overall behavior was explicitly accepted as stable.
- Stable promotion: completed 2026-09-20.
- Stable branch: `main`.
- GitHub Release: `v0.1.2` (release ID `392249903`).
- Release target commit: `fcdaa777a7118b62aa9f7a2a47948468b158ec73`.
- Stable distribution filename: `MeditationSpeed.dll`.
- Stable binary SHA-256: `c59d8f29ad97ffb0ff38b7f420b128a0b31c2850bc168c7e597031ffc6de88f0`.
- Release asset digest reported by GitHub: `sha256:c59d8f29ad97ffb0ff38b7f420b128a0b31c2850bc168c7e597031ffc6de88f0`.
- Publication reused the exact accepted CI artifact; no rebuild was performed.

### 1.0.0 — 2026-09-20

- Status: accepted stable
- Source branch: `dev/1.0.0`
- Source SHA: `37964f2e17d52d2d81df9265778ff2b5ef21fe80`
- Build mode: Release
- Build result: 0 warnings, 0 errors
- Workflow run: `35476504005`
- Artifact ID: `10594291711`
- Artifact ZIP digest: `sha256:139aaf2fa64bf9967ff59abe4b97f4b62ba0f2d88410f1ea6ee649c1b8d40216`
- Artifact: `MeditationSpeed.dll`
- DLL SHA-256: `54888d085cefe957fc913e013b577da3890d31345956e62baeaf783c4af2bdfa`
- Purpose: pre-1.0 audit hardening plus semantic version promotion.
- Runtime behavior changes from accepted 0.1.2:
  - none in meditation timing, input, UI, or lifecycle behavior;
  - startup now patches the explicit plugin assembly;
  - initialization failure rolls back this plugin's partial Harmony patches before disabling.
- Requested runtime smoke test:
  - replace 0.1.2 with this 1.0.0 candidate;
  - confirm the log reports `Meditation Speed 1.0.0 loaded`;
  - start meditation and make one complete 1× -> 2× -> 4× -> 2× -> 1× pass;
  - confirm one press remains one step and the boundary arrows remain correct;
  - wake normally and confirm ordinary gameplay timing is normal.
- Result:
  - the user confirmed the exact 1.0.0 candidate works correctly in the installed game;
  - one-press-one-step input remains correct and comfortable;
  - 1×/2×/4× presentation remains correct;
  - normal meditation exit and ordinary gameplay timing remain correct;
  - supplied runtime log confirms `Meditation Speed 1.0.0 loaded` and normal WaitingGUI open/close flow.
- Stable promotion: authorized 2026-09-20; publish this exact tested DLL without rebuilding.
- Stable distribution filename: `MeditationSpeed.dll`.
- Stable binary SHA-256: `54888d085cefe957fc913e013b577da3890d31345956e62baeaf783c4af2bdfa`.

## Production candidate template

### X.Y.Z — YYYY-MM-DD

- Status: candidate / accepted / rejected
- Source branch:
- Source SHA:
- Build mode: Release
- Artifact:
- SHA-256:
- Purpose:
- Requested runtime checks:
- Result:
- Stable promotion:
