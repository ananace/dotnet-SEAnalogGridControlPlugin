using Sandbox.Game.Entities;
using Sandbox.Game.Multiplayer;

namespace AnanaceDev.AnalogGridControl.Util
{

  public static class MyShipControllerExtensions
  {
    public static bool CanAnalogInput(this MyShipController controller)
    {
      if (Plugin.ControllerPatched && Sync.IsServer)
        return true;

      return true;
    }

    public static bool ShouldAnalogInput(this MyShipController controller)
    {
      if (AnalogGridControlSession.Instance == null)
        return false;

      return AnalogGridControlSession.Instance.CanControl(controller);
    }
  }

}
