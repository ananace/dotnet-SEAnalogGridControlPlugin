using System.Reflection;
using AnanaceDev.AnalogGridControl.Util;

namespace AnanaceDev.AnalogGridControl.InputMapping
{

  public enum InputAction
  {
    // Meta actions
    [EnumDescription("Invert Strafe Forward", "Toggle the Strafe Forward bind between being forward/backward")]
    InvertStrafeForward,
    [EnumDescription("Toggle Analog Input Active")]
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
    [EnumDescription("Next Toolbar Action")]
    ToolbarActionNext,
    [EnumDescription("Previous Toolbar Action")]
    ToolbarActionPrev,
  }

  public static class InputActionExtension
  {
    public static string Wordify(this InputAction action)
    {
      var rex = new System.Text.RegularExpressions.Regex("(?<=[a-z])(?<x>[A-Z])|(?<=.)(?<x>[A-Z])(?=[a-z])");
      return rex.Replace(action.ToString() , " ${x}");
    }

    public static string GetHumanReadableName(this InputAction action)
    {
      MemberInfo[] memInfo = typeof(InputAction).GetMember(action.ToString());
      if (memInfo != null && memInfo.Length > 0)
      {
        object[] attrs = memInfo[0].GetCustomAttributes(typeof(EnumDescriptionAttribute), false);
        if (attrs != null && attrs.Length > 0)
          return ((EnumDescriptionAttribute)attrs[0]).Name;
      }

      return action.Wordify();
    }

    public static string GetDescription(this InputAction action)
    {
      MemberInfo[] memInfo = typeof(InputAction).GetMember(action.ToString());
      if (memInfo == null || memInfo.Length == 0)
        return null;

      object[] attrs = memInfo[0].GetCustomAttributes(typeof(EnumDescriptionAttribute), false);
      if (attrs == null || attrs.Length == 0)
        return null;

      return ((EnumDescriptionAttribute)attrs[0]).Name;
    }
  }

}
