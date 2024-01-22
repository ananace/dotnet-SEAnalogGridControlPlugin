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

      if (Sandbox.Game.Gui.MyGuiScreenGamePlay.DisableInput)
        return false;

      var analogInput = AnalogGridControlSession.Instance;
      if (!analogInput.IsAnalogInputActive || analogInput.CurrentControllable != controller || analogInput.CurrentPlayer?.Character != controller.Pilot)
        return false;

      return true;
    }
  }

}
