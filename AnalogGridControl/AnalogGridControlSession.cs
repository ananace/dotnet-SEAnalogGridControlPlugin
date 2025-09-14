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
    public Vector2 CameraRotationVector => Input.CameraRotationVector;
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

      if (!Plugin.ControllerPatched)
        MyPluginLog.Warning("Controller wasn't successfully patched, analog input won't work.");

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

      if (Session?.Player == null)
        return;

      if (CurrentPlayer == null)
      {
        MyPluginLog.Debug("AnalogGridControlSession - Found initial player, initializing devices");

        // Ensure all input devices are primed and clean
        Input.Devices.ForEach(dev => { dev.Update(false); dev.ResetBinds(); });

        if (Plugin.ControllerPatched && !Sync.IsServer && !AnalogWheelAvailabilityRequested)
        {
          SendMessageToServer(new Network.AnalogAvailabilityRequest(), true);
          AnalogWheelAvailabilityRequested = true; // Assume failure to send will remain a failure
        }
      }

      if (CurrentPlayer != Session.Player)
      {
        MyPluginLog.Debug("AnalogGridControlSession - Updating current player link");

        CurrentPlayer = Session.Player;
        CurrentPlayer.Controller.ControlledEntityChanged += UpdateCurrentControlUnit;
        UpdateCurrentControlUnit(null, CurrentPlayer.Controller.ControlledEntity);
      }

      if (!Session.IsServer && Plugin.InputRegistry.InputThrottleMultiplayerSpecified && (CurrentTick % Plugin.InputThrottleMultiplayer) != 0)
        return;

      UpdateCurrentGridInputs();
    }

    public bool CanControl(IMyControllableEntity controllable)
    {
      if (AnalogGridControlSession.Instance != this)
        return false;
      if (controllable == null)
        return false;

      // TODO: Figure out a way to correctly inject analog input while an in-game screen is open
      // If input is injected it will compound open itself while the screen is open
      if (Sandbox.Game.Gui.MyGuiScreenGamePlay.ActiveGameplayScreen != null)
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
          MyPluginLog.Debug("Detached from grid");
      }
    }

    private void OnActionTriggered(object _sender, GameAction action)
    {
      if (!CanControl(CurrentControllable))
        return;

      switch (action)
      {
        case GameAction.SwitchLights: CurrentControllable.SwitchLights(); break;
        case GameAction.SwitchDamping: CurrentControllable.SwitchDamping(); break;
        case GameAction.SwitchHandbrake: CurrentControllable.SwitchHandbrake(); break;
        case GameAction.SwitchReactors: CurrentControllable.SwitchReactorsLocal(); break;
        case GameAction.SwitchLandingGears: CurrentControllable.SwitchLandingGears(); break;
      }

      if (!(CurrentControllable is Sandbox.Game.Entities.MyShipController CurrentCockpit))
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

        case GameAction.ToolbarActionNext: CurrentCockpit.Toolbar.SelectNextSlot(); break;
        case GameAction.ToolbarActionPrev: CurrentCockpit.Toolbar.SelectPreviousSlot(); break;
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
        case GameAction.ToolbarSwitch1: CurrentCockpit.Toolbar.SwitchToPage(0); break;
        case GameAction.ToolbarSwitch2: CurrentCockpit.Toolbar.SwitchToPage(1); break;
        case GameAction.ToolbarSwitch3: CurrentCockpit.Toolbar.SwitchToPage(2); break;
        case GameAction.ToolbarSwitch4: CurrentCockpit.Toolbar.SwitchToPage(3); break;
        case GameAction.ToolbarSwitch5: CurrentCockpit.Toolbar.SwitchToPage(4); break;
        case GameAction.ToolbarSwitch6: CurrentCockpit.Toolbar.SwitchToPage(5); break;
        case GameAction.ToolbarSwitch7: CurrentCockpit.Toolbar.SwitchToPage(6); break;
        case GameAction.ToolbarSwitch8: CurrentCockpit.Toolbar.SwitchToPage(7); break;
        case GameAction.ToolbarSwitch9: CurrentCockpit.Toolbar.SwitchToPage(8); break;
      }
    }

    private void OnActionBegin(object _sender, GameAction action)
    {
      if (!CanControl(CurrentControllable))
        return;

      if (!(CurrentControllable is Sandbox.Game.Entities.MyShipController CurrentCockpit))
        return;

      switch (action)
      {
        case GameAction.FirePrimary: CurrentCockpit.BeginShoot(MyShootActionEnum.PrimaryAction); break;
        case GameAction.FireSecondary: CurrentCockpit.BeginShoot(MyShootActionEnum.SecondaryAction); break;
        // case GameAction.FireTertiary: CurrentCockpit.BeginShoot(MyShootActionEnum.TertiaryAction); break;
        case GameAction.Brake: CurrentCockpit.GridWheels?.SetBrakingForce(1.0f); break;
      }
    }

    private void OnActionEnd(object _sender, GameAction action)
    {
      if (!CanControl(CurrentControllable))
        return;

      if (!(CurrentControllable is Sandbox.Game.Entities.MyShipController CurrentCockpit))
        return;

      switch (action)
      {
        case GameAction.FirePrimary: CurrentCockpit.EndShoot(MyShootActionEnum.PrimaryAction); break;
        case GameAction.FireSecondary: CurrentCockpit.EndShoot(MyShootActionEnum.SecondaryAction); break;
        // case GameAction.FireTertiary: CurrentCockpit.EndShoot(MyShootActionEnum.TertiaryAction); break;
        case GameAction.Brake: CurrentCockpit.GridWheels?.SetBrakingForce(0.0f); break;
      }
    }

    private void UpdateCurrentGridInputs()
    {
      if (!CanControl(CurrentControllable))
        return;

      if (!(CurrentControllable is Sandbox.Game.Entities.MyShipController CurrentCockpit))
        return;

      if (CurrentCockpit.ControlWheels)
      {
        CurrentCockpit.WheelJump(Input.IsInputActive(GameAction.WheelJump));

        // Fake some analog input for wheels even if plugin isn't running on the server
        if (AnalogWheelsAvailable)
        {
          CurrentCockpit.TryEnableBrakes(Input.IsInputActive(GameAction.Brake)); 
        }
        else
        {
          CurrentCockpit.TryEnableBrakes(Input.IsInputActive(GameAction.Brake) || Input.BrakeForce == 1f ||
              AnalogEmulation.ShouldTick(Input.BrakeForce, CurrentTick) ||
              // Emulate proportional acceleration as pulsed brakes for now
              !AnalogEmulation.ShouldTick(AccelForce * 2, CurrentTick));
        }
      }

      IMyCameraController cameraController = MyAPIGateway.Session?.CameraController;
      cameraController?.Rotate(CameraRotationVector, 0f);
    }

#region Networking
    public static bool SendMessageToServer<T>(T msg, bool reliable) where T : Network.AnalogGridControlPacket
    {
      try
      {
        if (Sync.MultiplayerActive)
          using (var stream = new System.IO.MemoryStream())
          {
              ProtoBuf.Serializer.Serialize(stream, msg);
              MyModAPIHelper.MyMultiplayer.Static.SendMessageToServer(Plugin.Id, stream.ToArray(), reliable);
            return true;
          }
      }
      catch (Exception ex)
      {
        MyPluginLog.Warning($"Failed to send message {msg} to server; {ex}");
      }

      return false;
    }

    public static bool SendMessageToPlayer<T>(T msg, ulong playerId, bool reliable) where T : Network.AnalogGridControlPacket
    {
      try
      {
        if (Sync.MultiplayerActive)
          using (var stream = new System.IO.MemoryStream())
          {
            ProtoBuf.Serializer.Serialize(stream, msg);
            MyModAPIHelper.MyMultiplayer.Static.SendMessageTo(Plugin.Id, stream.ToArray(), playerId, reliable);
            return true;
          }
      }
      catch (Exception ex)
      {
        MyPluginLog.Warning($"Failed to send message {msg} to player {playerId}; {ex}");
      }

      return false;
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

        SendMessageToPlayer(new Network.AnalogAvailabilityResponse { Version = Plugin.NetworkVersion }, playerId, true);
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
