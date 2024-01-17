using System.Reflection;
using AnanaceDev.AnalogGridControl.Util;

namespace AnanaceDev.AnalogGridControl.InputMapping
{

  public enum InputAxis
  {
    // For HOTAS/pedal usage, single-axis which swaps direction on button press
    [EnumDescription("Strafe Forward", "For HOTAS/Pedal use, toggle between forward/backward with Invert Strafe Forward action")]
    StrafeForward,

    [EnumDescription("Strafe Forward/Backward")]
    StrafeForwardBackward,
    [EnumDescription("Strafe Left/Right")]
    StrafeLeftRight,
    [EnumDescription("Strafe Up/Down")]
    StrafeUpDown,

    [EnumDescription("Pitch")]
    TurnPitch,
    [EnumDescription("Yaw")]
    TurnYaw,
    [EnumDescription("Roll")]
    TurnRoll
  }

  public static class InputAxisExtension
  {
    public static string GetHumanReadableName(this InputAxis axis)
    {
      MemberInfo[] memInfo = typeof(InputAxis).GetMember(axis.ToString());
      if (memInfo != null && memInfo.Length > 0)
      {
        object[] attrs = memInfo[0].GetCustomAttributes(typeof(EnumDescriptionAttribute), false);
        if (attrs != null && attrs.Length > 0)
          return ((EnumDescriptionAttribute)attrs[0]).Name;
      }

      return axis.ToString();
    }

    public static string GetDescription(this InputAxis axis)
    {
      MemberInfo[] memInfo = typeof(InputAxis).GetMember(axis.ToString());
      if (memInfo == null || memInfo.Length == 0)
        return null;

      object[] attrs = memInfo[0].GetCustomAttributes(typeof(EnumDescriptionAttribute), false);
      if (attrs == null || attrs.Length == 0)
        return null;

      return ((EnumDescriptionAttribute)attrs[0]).Name;
    }
  }

}
