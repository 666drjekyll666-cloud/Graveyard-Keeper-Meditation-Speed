# Meditation Speed

A small quality-of-life mod for **Graveyard Keeper 1.407** that lets you change meditation speed without leaving the meditation interface.

## Features

- **1×** — vanilla meditation speed
- **2×**
- **4×**
- Keyboard and gamepad support through Graveyard Keeper's native input system
- Current speed shown directly in the meditation tip
- Unavailable directions disappear at the 1×/4× limits
- Each meditation session starts at 1×

The mod accelerates Graveyard Keeper's existing meditation simulation rather than reimplementing individual systems. World time, recovery, crops, NPC schedules, crafting, and other systems continue to use the game's normal scaled-time path.

It does **not** change sleep speed or ordinary gameplay speed, and it does not modify save data.

## Controls

- **Decrease speed:** A / Left Arrow / gamepad D-pad Left
- **Increase speed:** D / Right Arrow / gamepad D-pad Right
- **Wake / exit:** unchanged vanilla control

The indicator is shown as:

`1×  >` → `<  2×  >` → `<  4×`

One physical press changes the speed by one step.

## Installation

1. Install BepInEx 5 for Graveyard Keeper.
2. Download `MeditationSpeed.dll` from the latest GitHub Release.
3. Put `MeditationSpeed.dll` in `Graveyard Keeper/BepInEx/plugins/`.
4. Start the game.

## Compatibility

- **Graveyard Keeper 1.407:** supported.
- **Longer Days 1.7.1:** runtime-tested with a 675-second day; meditation acceleration composes with its changed day length.
- Mods that independently add meditation HP/energy recovery from scaled `Time.deltaTime` can compound recovery while this mod is active.

## Current release

**0.1.2**

The accepted timing mapping is:

| Speed | timeScale | fixedDeltaTime |
| --- | ---: | ---: |
| 1× | 10 | 0.083333336 |
| 2× | 20 | 0.16666667 |
| 4× | 40 | 0.33333334 |
