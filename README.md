# Meditation Speed

A small quality-of-life mod for **Graveyard Keeper 1.407** that lets the player change meditation speed while the meditation interface is open.

Planned initial speeds:

- **1×** — vanilla meditation speed
- **2×**
- **4×**

The design goal is to accelerate Graveyard Keeper's existing meditation simulation rather than reimplement time progression, recovery, crops, NPCs, crafting, or other world systems.

## Controls

Planned controls use Graveyard Keeper's native input abstraction:

- decrease speed: `GameKey.SliderDec` — A / Left Arrow / gamepad D-pad Left
- increase speed: `GameKey.SliderInc` — D / Right Arrow / gamepad D-pad Right
- wake/exit: unchanged vanilla control

The current speed should be visible in the meditation UI.

## Status

**Research/bootstrap. No public release yet.**

The vanilla owner and time-scale path are verified. The remaining pre-implementation research item is the correct `Time.fixedDeltaTime` policy for 2×/4× meditation and the narrowest UI/input integration seam.

## Development

This repository follows the engineering contract in `666drjekyll666-cloud/DevRules`.

Long-lived technical evidence is recorded in `docs/VERIFIED_RUNTIME_DATA.md`; numbered test builds will be recorded in `docs/TEST_BUILD_LOG.md`.
