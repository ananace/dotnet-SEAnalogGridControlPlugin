using AnanaceDev.AnalogGridControl.InputMapping;
using AnanaceDev.AnalogGridControl.Util;
using Sandbox.Game.Multiplayer;
using Sandbox.ModAPI;
using System.IO;
using System.Linq;
using System;
using VRage.Game.Components;
using VRage.Game.ModAPI.Interfaces;
using VRage.Game.ModAPI;
using VRage.Game;
using VRageMath;

namespace AnanaceDev.AnalogGridControl
{

  [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation | MyUpdateOrder.Simulation)]
  public class AnalogGridControlSession : MySessionComponentBase
  {
    public static AnalogGridControlSession Instance;
    public InputAggregate Input => Plugin.InputAggregate;

    public bool IsAnalogInputActive => Input.IsAnalogInputActive;
    public Vector3 MovementVector => Input.MovementVector;
    public Vector3 RotationVector => Input.RotationVector;
    public float BrakeForce => Input.BrakeForce;
    public float AccelForce => Math.Max(Input.MovementVector.Z + Input.AccelForce, 1f);

    public uint? AnalogServerVersion = null;
    public bool AnalogWheelsAvailable => Plugin.ControllerPatched && (Sync.IsServer || AnalogServerVersion.HasValue);
    public bool AnalogWheelAvailabilityRequested = false;

    public IMyPlayer CurrentPlayer { get; private set; }
    public IMyShipController CurrentControllable { get; private set; }
    public IMyCubeGrid CurrentGrid { get; private set; }

    public ushort CurrentTick { get; private set; } = 0;

    public override void Init(MyObjectBuilder_SessionComponent _sessionComponent)
    {
      if (Sync.IsServer)
        AnalogServerVersion = Plugin.NetworkVersion;
    }

    public override void LoadData()
    {
      Instance = this;


      Input.ActionTriggered += OnActionTriggered;
      Input.ActionBegin += OnActionBegin;
      Input.ActionEnd += OnActionEnd;

      if (Sync.MultiplayerActive)
        Sandbox.ModAPI.MyModAPIHelper.MyMultiplayer.Static.RegisterSecureMessageHandler(Plugin.Id, OnReceiveAnalogUpdate);
    }

    protected override void UnloadData()
    {
      if (Sync.MultiplayerActive)
        Sandbox.ModAPI.MyModAPIHelper.MyMultiplayer.Static.UnregisterSecureMessageHandler(Plugin.Id, OnReceiveAnalogUpdate);

      if (CurrentPlayer != null)
        CurrentPlayer.Controller.ControlledEntityChanged -= UpdateCurrentControlUnit;

      Input.ActionTriggered -= OnActionTriggered;
      Input.ActionBegin -= OnActionBegin;
      Input.ActionEnd -= OnActionEnd;

      if (Instance == this)
      {
        // Clean the aggregate if ending the session, to not leave active input on resumption
        Input.Reset();
        Instance = null;
      }
    }

    public override void UpdateBeforeSimulation()
    {
      ++CurrentTick;

      if (Session.Player == null)
        return;

      if (CurrentPlayer == null)
      {
        MyPluginLog.Debug("AnalogGridControlSession - Found player");

        // Ensure all input devices are primed and clean
        Input.Devices.ForEach(dev => { dev.Update(false); dev.ResetBinds(); });

        CurrentPlayer = Session.Player;
        CurrentPlayer.Controller.ControlledEntityChanged += UpdateCurrentControlUnit;
        UpdateCurrentControlUnit(null, CurrentPlayer.Controller.ControlledEntity);

        if (Plugin.ControllerPatched && !Sync.IsServer && !AnalogWheelAvailabilityRequested)
        {
          AnalogWheelAvailabilityRequested = true;
          SendMessageToServer(new Network.AnalogAvailabilityRequest(), true);
        }
      }

      if (!Session.IsServer && Plugin.InputRegistry.InputThrottleMultiplayerSpecified && (CurrentTick % Plugin.InputThrottleMultiplayer) != 0)
        return;

      UpdateCurrentGridInputs();
    }

    public bool CanControl(IMyControllableEntity controllable)
    {
      if (AnalogGridControlSession.Instance != this)
        return false;

      if (Sandbox.Game.Gui.MyGuiScreenGamePlay.DisableInput || !IsAnalogInputActive)
        return false;

      if (!controllable.ControllerInfo.IsLocallyControlled())
        return false;

      return true;
    }

    private void UpdateCurrentControlUnit(IMyControllableEntity oldControlUnit, IMyControllableEntity newControlUnit)
    {
      var oldControllable = CurrentControllable;
      CurrentControllable = newControlUnit as IMyShipController;

      if (CurrentControllable != null && oldControllable == null)
      {
        CurrentGrid = CurrentControllable?.CubeGrid;

        Input.IsAnalogInputActive = Plugin.InputActiveByDefault;
        MyPluginLog.Debug($"Attached to new grid, analog input active: {Input.IsAnalogInputActive}");
      }
      else if (CurrentControllable == null && oldControllable != null)
      {
        if (oldControllable is Sandbox.Game.Entities.MyShipController oldCockpit)
        {
          MyPluginLog.Debug("Detached from grid, clearing old analog state");

          if (!Plugin.ControllerPatched && Sync.IsServer)
          {
            if (oldCockpit.EntityThrustComponent != null)
            {
              oldCockpit.EntityThrustComponent.AutopilotEnabled = false;
              oldCockpit.EntityThrustComponent.AutoPilotControlThrust = Vector3.Zero;
            }

            if (oldCockpit.GridGyroSystem != null)
            {
              oldCockpit.GridGyroSystem.AutopilotEnabled = false;
              oldCockpit.GridGyroSystem.ControlTorque = Vector3.Zero;
            }
          }
        }
      }
    }

    private void OnActionTriggered(object _sender, GameAction action)
    {
      if (action == GameAction.SwitchAnalogInputActive && !Plugin.ControllerPatched && !Input.IsAnalogInputActive && CurrentControllable is Sandbox.Game.Entities.MyShipController cockpit && Sync.IsServer)
      {
        if (cockpit.EntityThrustComponent != null)
        {
          cockpit.EntityThrustComponent.AutopilotEnabled = false;
          cockpit.EntityThrustComponent.AutoPilotControlThrust = Vector3.Zero;
        }
        if (cockpit.GridGyroSystem != null)
        {
          cockpit.GridGyroSystem.AutopilotEnabled = false;
          cockpit.GridGyroSystem.ControlTorque = Vector3.Zero;
        }
      }

      if (CurrentControllable == null || !Input.IsAnalogInputActive)
        return;

      switch (action)
      {
        case GameAction.SwitchLights: CurrentControllable.SwitchLights(); break;
        case GameAction.SwitchDamping: CurrentControllable.SwitchDamping(); break;
        case GameAction.SwitchHandbrake: CurrentControllable.SwitchHandbrake(); break;
        case GameAction.SwitchReactors: CurrentControllable.SwitchReactorsLocal(); break;
        case GameAction.SwitchLandingGears: CurrentControllable.SwitchLandingGears(); break;
      }

      var CurrentCockpit = CurrentControllable as Sandbox.Game.Entities.MyShipController;
      if (CurrentCockpit == null)
        return;

      switch (action)
      {
        case GameAction.Target:
          if (CurrentCockpit.IsTargetLockingEnabled())
            CurrentCockpit.Pilot.TargetFocusComp.OnLockRequest();
          break;
        case GameAction.ReleaseTarget:
          if (CurrentCockpit.IsTargetLockingEnabled())
            CurrentCockpit.Pilot.TargetLockingComp.ReleaseTargetLockRequest();
          break;

        case GameAction.ToolbarAction1: CurrentCockpit.Toolbar.ActivateItemAtSlot(0); break;
        case GameAction.ToolbarAction2: CurrentCockpit.Toolbar.ActivateItemAtSlot(1); break;
        case GameAction.ToolbarAction3: CurrentCockpit.Toolbar.ActivateItemAtSlot(2); break;
        case GameAction.ToolbarAction4: CurrentCockpit.Toolbar.ActivateItemAtSlot(3); break;
        case GameAction.ToolbarAction5: CurrentCockpit.Toolbar.ActivateItemAtSlot(4); break;
        case GameAction.ToolbarAction6: CurrentCockpit.Toolbar.ActivateItemAtSlot(5); break;
        case GameAction.ToolbarAction7: CurrentCockpit.Toolbar.ActivateItemAtSlot(6); break;
        case GameAction.ToolbarAction8: CurrentCockpit.Toolbar.ActivateItemAtSlot(7); break;
        case GameAction.ToolbarAction9: CurrentCockpit.Toolbar.ActivateItemAtSlot(8); break;

        case GameAction.ToolbarActionHolster: CurrentCockpit.Toolbar.ActivateItemAtSlot(CurrentCockpit.Toolbar.SlotCount); break;

        case GameAction.ToolbarSwitchNext: CurrentCockpit.Toolbar.PageUp(); break;
        case GameAction.ToolbarSwitchPrev: CurrentCockpit.Toolbar.PageDown(); break;
        case GameAction.ToolbarActionNext: CurrentCockpit.Toolbar.SelectNextSlot(); break;
        case GameAction.ToolbarActionPrev: CurrentCockpit.Toolbar.SelectPreviousSlot(); break;
      }
    }

    private void OnActionBegin(object _sender, GameAction action)
    {
      if (CurrentControllable == null || !Input.IsAnalogInputActive)
        return;

      var CurrentCockpit = CurrentControllable as Sandbox.Game.Entities.MyShipController;
      if (CurrentCockpit == null)
        return;

      switch (action)
      {
        case GameAction.FirePrimary: CurrentCockpit.BeginShoot(MyShootActionEnum.PrimaryAction); break;
        case GameAction.FireSecondary: CurrentCockpit.BeginShoot(MyShootActionEnum.SecondaryAction); break;
        // case GameAction.FireTertiary: CurrentCockpit.BeginShoot(MyShootActionEnum.TertiaryAction); break;
      }
    }

    private void OnActionEnd(object _sender, GameAction action)
    {
      if (CurrentControllable == null || !Input.IsAnalogInputActive)
        return;

      var CurrentCockpit = CurrentControllable as Sandbox.Game.Entities.MyShipController;
      if (CurrentCockpit == null)
        return;

      switch (action)
      {
        case GameAction.FirePrimary: CurrentCockpit.EndShoot(MyShootActionEnum.PrimaryAction); break;
        case GameAction.FireSecondary: CurrentCockpit.EndShoot(MyShootActionEnum.SecondaryAction); break;
        // case GameAction.FireTertiary: CurrentCockpit.EndShoot(MyShootActionEnum.TertiaryAction); break;
      }
    }

    private void UpdateCurrentGridInputs()
    {
      if (!Input.IsAnalogInputActive ||!(CurrentControllable is Sandbox.Game.Entities.MyShipController CurrentCockpit))
        return;

      if (CurrentCockpit.ControlWheels)
      {
        CurrentCockpit.WheelJump(Input.IsInputActive(GameAction.WheelJump));

        if (AnalogWheelsAvailable)
          CurrentCockpit.TryEnableBrakes(Input.IsInputActive(GameAction.Brake) || Input.BrakeForce == 1f);
        else
        {
          CurrentCockpit.TryEnableBrakes(Input.IsInputActive(GameAction.Brake) || Input.BrakeForce == 1f ||
              AnalogEmulation.ShouldTick(Input.BrakeForce, CurrentTick) ||
              // Emulate proportional acceleration as pulsed brakes for now
              !AnalogEmulation.ShouldTick(AccelForce * 2, CurrentTick));
        }
      }
      
      if (Plugin.ControllerPatched || !Sync.IsServer)
        return;

#region Fallbacks
      // Fallback for if analog patch fails

      // Inject normally, to hopefully work with recorder
      CurrentCockpit.MoveAndRotate(Input.MovementVector, new Vector2(Input.RotationVector.X, Input.RotationVector.Y), Input.RotationVector.Z);

      Matrix orientMatrix;
      CurrentCockpit.Orientation.GetMatrix(out orientMatrix);
      if (CurrentCockpit.ControlThrusters && CurrentCockpit.EntityThrustComponent != null)
      {
        CurrentCockpit.EntityThrustComponent.AutopilotEnabled = true;
        if (Input.MovementVector.LengthSquared() > 0.0f)
        {
          Vector3 controlThrust;
          Vector3 input = Input.MovementVector;
          Vector3.RotateAndScale(ref input, ref orientMatrix, out controlThrust);
          CurrentCockpit.EntityThrustComponent.AutoPilotControlThrust = -controlThrust;
        }
        else
          CurrentCockpit.EntityThrustComponent.AutoPilotControlThrust = Vector3.Zero;

        CurrentCockpit.EntityThrustComponent.AutoPilotControlThrustDampenersEnabled = CurrentControllable.DampenersOverride;
      }

      if (CurrentCockpit.ControlGyros && CurrentCockpit.GridGyroSystem != null)
      {
        CurrentCockpit.GridGyroSystem.AutopilotEnabled = true;
        CurrentCockpit.GridGyroSystem.ControlTorque = Vector3.ClampToSphere(-Vector3.Transform(Input.RotationVector, orientMatrix), 1.0f);
      }

      CurrentCockpit.EntityThrustComponent?.MarkDirty();
      CurrentCockpit.GridGyroSystem?.MarkDirty();
#endregion
    }

#region Networking
    public static void SendMessageToServer<T>(T msg, bool reliable) where T : Network.AnalogGridControlPacket
    {
      using (var stream = new System.IO.MemoryStream())
      {
        ProtoBuf.Serializer.Serialize(stream, msg);
        MyModAPIHelper.MyMultiplayer.Static.SendMessageToServer(Plugin.Id, stream.ToArray(), reliable);
      }
    }

    public static void SendMessageToPlayer<T>(T msg, ulong playerId, bool reliable) where T : Network.AnalogGridControlPacket
    {
      using (var stream = new System.IO.MemoryStream())
      {
        ProtoBuf.Serializer.Serialize(stream, msg);
        MyModAPIHelper.MyMultiplayer.Static.SendMessageTo(Plugin.Id, stream.ToArray(), playerId, reliable);
      }
    }

    void OnReceiveAnalogUpdate(ushort id, byte[] data, ulong playerId, bool arrivedFromServer)
    {
      Network.AnalogGridControlPacket packet = null;

      try
      {
        using (var stream = new MemoryStream(data))
          packet = ProtoBuf.Serializer.Deserialize<Network.AnalogGridControlPacket>(stream);
      }
      catch {}

      if (packet is Network.AnalogInputUpdate inputUpdate)
      {
        if (!Sync.IsServer || id != Plugin.Id || arrivedFromServer)
          return;
        if (!(Sandbox.Game.Entities.MyEntities.GetEntityById(inputUpdate.GridId) is Sandbox.Game.Entities.MyCubeGrid grid))
          return;

        Sandbox.Game.World.MyPlayer player;
        if (!Sandbox.Game.World.MySession.Static.Players.TryGetPlayerBySteamId(playerId, out player))
          return;

        if (!grid.GridSystems.ControlSystem?.HasPilot(player.Id) ?? false)
          return;

        if (inputUpdate.BrakeForce.HasValue)
          grid.GridSystems.WheelSystem?.SetBrakingForce(inputUpdate.BrakeForce.Value);
        // if (inputUpdate.AccelForce.HasValue)
        //   grid.GridSystems.WheelSystem?.SetAccelForce(inputUpdate.AccelForce.Value);
      }
      else if (packet is Network.AnalogAvailabilityRequest availabilityRequest)
      {
        if (!Sync.IsServer || id != Plugin.Id || arrivedFromServer)
          return;

        SendMessageToPlayer(new Network.AnalogAvailabilityResponse { Version = AnalogServerVersion ?? Plugin.NetworkVersion }, playerId, true);
      }
      else if (packet is Network.AnalogAvailabilityResponse availabilityResponse)
      {
        if (Sync.IsServer || id != Plugin.Id || !arrivedFromServer || AnalogWheelsAvailable)
          return;

        AnalogServerVersion = availabilityResponse.Version;
      }
    }
#endregion
  }

}
