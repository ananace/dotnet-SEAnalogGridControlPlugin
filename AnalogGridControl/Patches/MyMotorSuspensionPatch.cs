using System;
using AnanaceDev.AnalogGridControl.Util;
using HarmonyLib;
using Havok;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using VRage.ModAPI;
using VRageMath;

namespace AnanaceDev.AnalogGridControl.Patches
{

  [HarmonyPatch(typeof(MyMotorSuspension), "Accelerate")]
  public static class MyMotorSuspensionAcceleratePatch
  {
    public static void Prefix(MyMotorSuspension __instance, ref float force, bool forward)
    {
      if (__instance.PropulsionOverride != 0f)
        return;

      var input = __instance.GetWheelSystem()?.GetPropulsionStrength() ?? 0f;
      if (input == 0f)
        return;

      force *= input;
    }
  }

  [HarmonyPatch(typeof(MyMotorSuspension), nameof(MyMotorSuspension.UpdateBrake))]
  public static class MyMotorSuspensionBrakePatch
  {
    public static void Postfix(MyMotorSuspension __instance, ref bool ___m_updateBrakeNeeded)
    {
      var brakeForce = __instance.GetWheelSystem()?.GetBrakingForce() ?? 0f;
      if (brakeForce == 0f)
        return;

      ___m_updateBrakeNeeded = true;
      __instance.NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;

      var traverse = Traverse.Create(__instance);
      var safeBody = traverse.Property("SafeBody").GetValue<HkRigidBody>();

      if (safeBody == null)
        return;

      var propulsionForce = __instance.BlockDefinition.PropulsionForce;
      var baseAngularDamping = __instance.CubeGrid.Physics.AngularDamping;
      safeBody.AngularDamping = baseAngularDamping + brakeForce * (propulsionForce - baseAngularDamping);
    }
  }

  [HarmonyPatch(typeof(MyMotorSuspension), "PropagateFriction")]
  public static class MyMotorSuspensionPropagateFrictionPatch
  {
    public static void Postfix(MyMotorSuspension __instance)
    {
      var brakeForce = __instance.GetWheelSystem()?.GetBrakingForce() ?? 0f;
      if (brakeForce == 0f)
        return;

      if (!(__instance.TopBlock is MyWheel myWheel))
        return;
      
      var before = myWheel.Friction;
      double baseFriction = 35.0 * ((double)(MyMath.FastTanH(6f * (float)__instance.Friction - 3f) / 2f) + 0.5);

      myWheel.Friction *= (1 + brakeForce);
      myWheel.CubeGrid.Physics.RigidBody.Friction = myWheel.Friction;
    }
  }


}
