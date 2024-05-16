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
    TurnRoll
  }

}
