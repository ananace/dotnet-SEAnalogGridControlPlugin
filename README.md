Analog Grid Control Plugin
==========================

**Nota Bene**; Still under active development.

This plugin allows for true analog control for grids in Space Engineers

Analog inputs are injected into the regular input pipeline, applying on top of any KB+M inputs.  
Should theoretically work seamlessly with the recorder as well as multiplayer.

To Do;
------

- [X] Working analog input
  - [X] Thrusters
  - [X] Gyros
  - [X] Wheels
    - [ ] Brakes (only digital input for now)
  - [X] Semi-functional fallback without Harmony patch (no support for wheels)
- [X] Per-device mappings
- [X] POV hats
- [X] HOTAS/Wheel support
  - [X] Forward/Backwards strafe toggle
  - [X] Hold-style backwards strafe bind - to support reverse gear on wheel+gearbox
- [ ] More bindable actions
  - [X] Targeting
  - [ ] Stepped toolbar action selection without activating.
        (Will require another patch)
  - [ ] ...
- [ ] Handle devices (re)appearing during gameplay.
      (Currently requires a game restart)
- [ ] Handle DInput giving bogus output on device axises until they've been actuated
- [ ] Ensure multiplayer works
- [ ] Gamepad interoperability(?)
- [ ] Support FPS input binds as well(?)
      (Should these be separate binds, or unified?)
- [X] Configuration UI
  - [ ] Reordering of binds(?)
- [ ] Separate static configuration from runtime data
- [ ] Exception handling _everywhere_
- [ ] Pretty code
