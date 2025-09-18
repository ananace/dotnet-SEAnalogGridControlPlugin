using AnanaceDev.AnalogGridControl.Util;

namespace AnanaceDev.AnalogGridControl.InputMapping
{

  public enum GameAction
  {
    // Meta actions
    None,

    [EnumDescription("Invert Strafe Forward", "Toggle the Strafe Forward bind between being forward/backward,\nworks as both a toggle and a hold")]
    InvertStrafeForward,
    [EnumDescription("Invert Strafe Forward (Toggle)", "Toggle the Strafe Forward bind between being forward/backward")]
    InvertStrafeForwardToggle,
    [EnumDescription("Invert Strafe Forward (Hold)", "Change the Strafe Forward bind between being forward/backward while held")]
    InvertStrafeForwardHold,
    [EnumDescription("Toggle Analog Input Active", "Toggle if analog input should be applied to the currently piloted grid,\ndefault can be chosen in the main settings")]
    SwitchAnalogInputActive,

    // Grid actions
    [EnumDescription("Primary Fire")]
    FirePrimary,
    [EnumDescription("Secondary Fire")]
    FireSecondary,

    [EnumDescription("Lock Target")]
    Target,
    [EnumDescription("Release Target")]
    ReleaseTarget,
    [EnumDescription("Jump (Wheels)")]
    WheelJump,
    [EnumDescription("Brake (Wheels)")]
    Brake,

    [EnumDescription("Toggle Lights")]
    SwitchLights,
    [EnumDescription("Toggle Dampeners")]
    SwitchDamping,
    [EnumDescription("Toggle Handbrake")]
    SwitchHandbrake,
    [EnumDescription("Toggle Power")]
    SwitchReactors,
    [EnumDescription("Toggle Parking")]
    SwitchLandingGears,

    [EnumDescription("Next Toolbar Action", "Nota Bene; This will currently both switch the highlighted action as well as trigger it")]
    ToolbarActionNext,
    [EnumDescription("Previous Toolbar Action", "Nota Bene; This will currently both switch the highlighted action as well as trigger it")]
    ToolbarActionPrev,
    ToolbarAction1,
    ToolbarAction2,
    ToolbarAction3,
    ToolbarAction4,
    ToolbarAction5,
    ToolbarAction6,
    ToolbarAction7,
    ToolbarAction8,
    ToolbarAction9,
    [EnumDescription("Empty Hands")]
    ToolbarActionHolster,

    [EnumDescription("Next Toolbar")]
    ToolbarSwitchNext,
    [EnumDescription("Previous Toolbar")]
    ToolbarSwitchPrev,
    [EnumDescription("Switch to Toolbar 1")]
    ToolbarSwitch1,
    [EnumDescription("Switch to Toolbar 2")]
    ToolbarSwitch2,
    [EnumDescription("Switch to Toolbar 3")]
    ToolbarSwitch3,
    [EnumDescription("Switch to Toolbar 4")]
    ToolbarSwitch4,
    [EnumDescription("Switch to Toolbar 5")]
    ToolbarSwitch5,
    [EnumDescription("Switch to Toolbar 6")]
    ToolbarSwitch6,
    [EnumDescription("Switch to Toolbar 7")]
    ToolbarSwitch7,
    [EnumDescription("Switch to Toolbar 8")]
    ToolbarSwitch8,
    [EnumDescription("Switch to Toolbar 9")]
    ToolbarSwitch9,
  }

}
