using AnanaceDev.AnalogGridControl.InputMapping;
using SharpDX.DirectInput;

namespace AnanaceDev.AnalogGridControl.Util
{

  public static class JoystickStateExtensions
  {

    public static int GetAxisValue(this JoystickState state, DeviceAxis axis)
    {
      switch (axis)
      {
        case DeviceAxis.X: return state.X;
        case DeviceAxis.Y: return state.Y;
        case DeviceAxis.Z: return state.Z;
        case DeviceAxis.RX: return state.RotationX;
        case DeviceAxis.RY: return state.RotationY;
        case DeviceAxis.RZ: return state.RotationZ;
        case DeviceAxis.Slider0: return state.Sliders[0];
        case DeviceAxis.Slider1: return state.Sliders[1];
      }

      return 0;
    }

    public static float GetAxisValueNormalized(this JoystickState state, DeviceAxis axis, InputRange range)
    {
      var intValue = state.GetAxisValue(axis);
      return (float)((double)(intValue - range.Minimum) / (double)(range.Maximum - range.Minimum));
    }

    public static bool GetPOVAxis(this JoystickState state, DeviceHatAxis axis, int? hat = null)
    {
      var intValue = state.PointOfViewControllers[hat ?? 0];
      if (intValue < 0)
        return false;

      switch (axis)
      {
        case DeviceHatAxis.Up: return intValue >= 31500 || intValue <= 4500;
        case DeviceHatAxis.Right: return intValue >= 4500 && intValue <= 13500;
        case DeviceHatAxis.Down: return intValue >= 13500 && intValue <= 22500;
        case DeviceHatAxis.Left: return intValue >= 22500 && intValue <= 31500;
      }
      return false;
    }
  }

}
