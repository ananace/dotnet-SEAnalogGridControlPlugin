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
    - [X] Proper - multiplayer-compatible - analog input patch
    - [X] Fallback using pulsed input on servers without plugin
  - [X] Semi-functional fallback without Harmony patch (no real support for wheels or multiplayer)
- [X] Per-device mappings
- [X] POV hats
- [X] HOTAS/Wheel support
  - [X] Forward/Backwards strafe toggle
  - [X] Hold-style backwards strafe bind - to support reverse gear on wheel+gearbox
- [ ] More bindable actions
  - [X] Targeting
  - [ ] Stepped toolbar action selection without activating
        (Will require another patch)
  - [ ] ...
- [X] Handle devices disappearing and (re)appearing
  - [X] Handle devices disappearing during gameplay
  - [X] Handle those devices then reappearing during gameplay
  - [X] Handle new devices appearing during gameplay
- [ ] Handle DInput giving bogus output on device axises until they've been actuated
- [ ] Ensure multiplayer works
- [ ] Gamepad interoperability(?)
- [ ] Support FPS input binds as well(?)
      (Should these be separate binds, or unified? E.g. on foot/jetpack/etc)
- [X] Configuration UI
  - [ ] Reordering of binds(?)
- [ ] Separate static configuration from runtime data
- [ ] Exception handling _everywhere_
- [ ] Pretty code
