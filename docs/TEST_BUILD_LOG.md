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
