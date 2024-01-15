using SharpDX.DirectInput;

namespace AnanaceDev.AnalogGridControl.InputMapping
{

  public enum DeviceAxis
  {
    X = JoystickOffset.X,
    Y = JoystickOffset.Y,
    Z = JoystickOffset.Z,

    RX = JoystickOffset.RotationX,
    RY = JoystickOffset.RotationY,
    RZ = JoystickOffset.RotationZ,

    Slider0 = JoystickOffset.Sliders0,
    Slider1 = JoystickOffset.Sliders1
  }

}
