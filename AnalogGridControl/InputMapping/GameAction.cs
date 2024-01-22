using AnanaceDev.AnalogGridControl.Util;

namespace AnanaceDev.AnalogGridControl.InputMapping
{

  public enum GameAction
  {
    // Meta actions
    None = 0,

    [EnumDescription("Invert Strafe Forward", "Toggle the Strafe Forward bind between being forward/backward,\nworks as both a toggle and a hold")]
    InvertStrafeForward = 1 << 0,
    [EnumDescription("Toggle Analog Input Active", "Toggle if analog input should be applied to the currently piloted grid,\ndefault can be chosen in the main settings")]
    SwitchAnalogInputActive = 1 << 1,

    // Grid actions
    [EnumDescription("Primary Fire")]
    FirePrimary = 1 << 2,
    [EnumDescription("Secondary Fire")]
    FireSecondary = 1 << 3,

    [EnumDescription("Lock Target")]
    Target = 1 << 4,
    [EnumDescription("Release Target")]
    ReleaseTarget = 1 << 5,
    [EnumDescription("Jump (Wheels)")]
    WheelJump = 1 << 6,
    [EnumDescription("Brake (Wheels)")]
    Brake = 1 << 7,

    [EnumDescription("Toggle Lights")]
    SwitchLights = 1 << 8,
    [EnumDescription("Toggle Dampeners")]
    SwitchDamping = 1 << 9,
    [EnumDescription("Toggle Handbrake")]
    SwitchHandbrake = 1 << 10,
    [EnumDescription("Toggle Power")]
    SwitchReactors = 1 << 11,
    [EnumDescription("Toggle Parking")]
    SwitchLandingGears = 1 << 12,

    ToolbarAction1 = 1 << 13,
    ToolbarAction2 = 1 << 14,
    ToolbarAction3 = 1 << 15,
    ToolbarAction4 = 1 << 16,
    ToolbarAction5 = 1 << 17,
    ToolbarAction6 = 1 << 18,
    ToolbarAction7 = 1 << 19,
    ToolbarAction8 = 1 << 20,
    ToolbarAction9 = 1 << 21,
    [EnumDescription("Empty Hands")]
    ToolbarActionHolster = 1 << 22,

    [EnumDescription("Next Toolbar")]
    ToolbarSwitchNext = 1 << 23,
    [EnumDescription("Previous Toolbar")]
    ToolbarSwitchPrev = 1 << 24,
    [EnumDescription("Next Toolbar Action", "Nota Bene; This will currently both switch the highlighted action as well as trigger it")]
    ToolbarActionNext = 1 << 25,
    [EnumDescription("Previous Toolbar Action", "Nota Bene; This will currently both switch the highlighted action as well as trigger it")]
    ToolbarActionPrev = 1 << 26,
  }

}
