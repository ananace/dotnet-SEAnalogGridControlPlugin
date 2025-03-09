using AnanaceDev.AnalogGridControl.Util;

namespace AnanaceDev.AnalogGridControl.InputMapping
{

  public enum GameAxis
  {
    // For HOTAS/pedal usage, single-axis which swaps direction on button press
    [EnumDescription("Strafe Forward", "For HOTAS/Pedal use, toggle between forward/backward with Invert Strafe Forward action")]
    StrafeForward,

    [EnumDescription("Strafe Forward/Backward")]
    StrafeForwardBackward,
    [EnumDescription("Strafe Left/Right", "This also handles turning on wheeled vehicles.")]
    StrafeLeftRight,
    [EnumDescription("Strafe Up/Down")]
    StrafeUpDown,

    // [EnumDescription("Accelerate (wheels)", "Same as Strafe Forward, but limited to only wheels. Toggle between forward/backward with Invert Strafe Forward action")]
    // Accelerate,
    [EnumDescription("Brake (wheels)")]
    Brake,

    [EnumDescription("Pitch")]
    TurnPitch,
    [EnumDescription("Yaw")]
    TurnYaw,
    [EnumDescription("Roll")]
    TurnRoll,

    [EnumDescription("Camera Pitch")]
    CameraPitch,
    [EnumDescription("Camera Yaw")]
    CameraYaw
  }

  public static class GameAxisExtensions
  {
    public static DeadzonePoint GetDeadzonePoint(this GameAxis axis)
    {
      switch (axis)
      {
      case GameAxis.StrafeForward:
      case GameAxis.Brake:
        return DeadzonePoint.End;

      case GameAxis.StrafeForwardBackward:
      case GameAxis.StrafeLeftRight:
      case GameAxis.StrafeUpDown:
      case GameAxis.TurnPitch:
      case GameAxis.TurnYaw:
      case GameAxis.TurnRoll:
      case GameAxis.CameraPitch:
      case GameAxis.CameraYaw:
        return DeadzonePoint.Mid;
      }

      return DeadzonePoint.None;
    }
  }

}
