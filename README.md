# Meditation Speed

A small quality-of-life mod for **Graveyard Keeper 1.407** that lets the player change meditation speed while the meditation interface is open.

## Speeds

- **1×** — vanilla meditation speed
- **2×**
- **4×**

Each meditation session starts at 1×.

The mod accelerates Graveyard Keeper's existing meditation simulation. It does not reimplement world time, recovery, crops, NPC schedules, crafting, sleep, or ordinary gameplay timing.

## Controls

Uses Graveyard Keeper's native input abstraction:

- decrease speed: A / Left Arrow / gamepad D-pad Left
- increase speed: D / Right Arrow / gamepad D-pad Right
- wake/exit: unchanged vanilla control

The selected speed is shown in the existing meditation tip. Only available directions are shown: `1×  >` / `<  2×  >` / `<  4×`.

## Architecture

Accepted 1.407 timing mapping:

| Speed | timeScale | fixedDeltaTime |
| --- | ---: | ---: |
| 1× | 10 | 0.083333336 |
| 2× | 20 | 0.16666667 |
| 4× | 40 | 0.33333334 |

The proportional fixed timestep keeps real-time FixedUpdate demand close to vanilla meditation instead of multiplying it at 2×/4×.

No save data is modified.

## Status

**0.1.2 development candidate. No public release yet.**

Accepted research is recorded in:
- `docs/VERIFIED_RUNTIME_DATA.md`
- `docs/FIXED_TIMESTEP_AND_UI_RESEARCH.md`
- `docs/TEST_BUILD_LOG.md`

This repository follows `666drjekyll666-cloud/DevRules`.
