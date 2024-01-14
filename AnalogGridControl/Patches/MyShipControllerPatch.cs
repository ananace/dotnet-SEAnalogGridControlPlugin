using HarmonyLib;
using Sandbox.Game.Entities;
using VRageMath;

namespace AnanaceDev.AnalogGridControl.Patches
{

  [HarmonyPatch(typeof(MyShipController), nameof(MyShipController.MoveAndRotate), new System.Type[0])]
  class MyShipControllerPatch
  {
    static void Prefix(MyShipController __instance)
    {
      if (AnalogGridControlSession.Instance == null)
        return;

      var analogInput = AnalogGridControlSession.Instance;
      if (analogInput.CurrentControllable != __instance)
        return;

      if (analogInput.CurrentPlayer?.Character != __instance.Pilot)
        return;

      if (!analogInput.IsAnalogInputActive)
        return;

      /// Inject analog input before the ship controller calculates the final movement data.
      /// Only direct player input seems to have actual analog scaling, autopilot and injected thrust intputs are binary - a.k.a. only zero or full, and overrides are messy.
      /// Also it's currently impossible to inject direct wheel inputs without overrides.

      //MyPluginLog.Debug("Injecting analog input into ship controller...");
      //__instance.MoveAndRotate(analogInput.MovementVector, new VRageMath.Vector2(analogInput.RotationVector.X, analogInput.RotationVector.Y), analogInput.RotationVector.Z);

      var traverse = Traverse.Create(__instance);
      var oldMove = traverse.Property("MoveIndicator").GetValue<Vector3>();
      var oldRot = traverse.Property("RotationIndicator").GetValue<Vector2>();
      var oldRoll = traverse.Property("RollIndicator").GetValue<float>();

      __instance.MoveAndRotate(oldMove + analogInput.MovementVector, oldRot + new VRageMath.Vector2(analogInput.RotationVector.X, analogInput.RotationVector.Y), oldRoll + analogInput.RotationVector.Z);
    }
    
  }

}
