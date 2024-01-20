using System;
using System.Linq;
using System.Reflection;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Multiplayer;
using VRageMath;

namespace AnanaceDev.AnalogGridControl.Util
{

  public static class MyGridWheelSystemExtensions
  {
    public static float GetPropulsionStrength(this MyGridWheelSystem system)
    {
      return MyMath.Clamp(Math.Abs(system.AngularVelocity.Z), 0f, 1f);
    }
    public static float GetBrakingForce(this MyGridWheelSystem system)
    {
      return MyMath.Clamp(Math.Abs(system.AngularVelocity.Y), 0f, 1f);
    }

    public static void SetBrakingForce(this MyGridWheelSystem system, float force)
    {
      if (!Sync.IsServer)
      {
        var grid = typeof(MyUpdateableGridSystem).GetProperty("Grid", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(system) as Sandbox.Game.Entities.MyCubeGrid;
        AnalogGridControlSession.SendMessageToServer(new Network.AnalogInputUpdate{ GridId = grid.EntityId, BrakeForce = force }, false);
      }

      var curVel = system.AngularVelocity;
      curVel.Y = force;
      system.AngularVelocity = curVel;

      if (Math.Abs(force) > 0f && Sync.IsServer)
        system.Wheels.ForEach(wheel => wheel.UpdateBrake());
    }
  }

}
