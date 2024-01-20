using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Multiplayer;

namespace AnanaceDev.AnalogGridControl.Util
{

  public static class MyMotorSuspensionExtensions
  {
    public static MyGridWheelSystem GetWheelSystem(this MyMotorSuspension motor)
    {
      return motor.CubeGrid?.GridSystems?.WheelSystem;
    }

    public static bool CanAnalogInput(this MyMotorSuspension motor)
    {
      if (Sync.IsServer)
        return true;

      return true;
    }

    public static bool ShouldAnalogInput(this MyMotorSuspension motor)
    {
      if (AnalogGridControlSession.Instance == null)
        return false;

      var analogInput = AnalogGridControlSession.Instance;
      if (!analogInput.IsAnalogInputActive || analogInput.CurrentControllable == null)
        return false;

      if (Sandbox.Game.Gui.MyGuiScreenGamePlay.DisableInput)
        return false;

      if (!analogInput.CurrentControllable.ControlWheels)
        return false;
      if (analogInput.CurrentPlayer != motor.CubeGrid.GridSystems.ControlSystem.GetController().Player)
        return false;

      return true;
    }
  }

}
