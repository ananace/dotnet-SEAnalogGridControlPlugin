using AnanaceDev.AnalogGridControl.InputMapping;
using AnanaceDev.AnalogGridControl.Util;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRageMath;

namespace AnanaceDev.AnalogGridControl
{

  [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation | MyUpdateOrder.Simulation)]
  public class AnalogGridControlSession : MySessionComponentBase
  {
    public static AnalogGridControlSession Instance;

    public InputAggregate Input = new InputAggregate();

    public bool IsAnalogInputActive => Input.IsAnalogInputActive;
    public Vector3 MovementVector => Input.MovementVector;
    public Vector3 RotationVector => Input.RotationVector;

    public IMyPlayer CurrentPlayer { get; private set; }
    public IMyCockpit CurrentControllable { get; private set; }
    public IMyCubeGrid CurrentGrid { get; private set; }

    private ushort CurrentTick = 0;

    public override void Init(MyObjectBuilder_SessionComponent _sessionComponent)
    {
      MyPluginLog.Debug("AnalogGridControlSession - Init");

      Input.DInput = Plugin.DInput;
      foreach (var dev in Plugin.InputRegistry.Devices)
      {
        dev.Acquire();

        if (dev.IsAcquired)
          Input.RegisterInput(dev);
      }

      Input.ActionTriggered += OnActionTriggered;
      Input.ActionBegin += OnActionBegin;
      Input.ActionEnd += OnActionEnd;

      MyPluginLog.Debug("AnalogGridControlSession - Init complete");
    }

    public override void LoadData()
    {
      Instance = this;
    }

    protected override void UnloadData()
    {
      Session.Player.Controller.ControlledEntityChanged -= UpdateCurrentControlUnit;
      Instance = null;
    }

    public override void UpdateBeforeSimulation()
    {
      ++CurrentTick;

      if (Session.Player == null)
        return;

      if (CurrentPlayer == null)
      {
        MyPluginLog.Debug("AnalogGridControlSession - Found player");
        CurrentPlayer = Session.Player;
        CurrentPlayer.Controller.ControlledEntityChanged += UpdateCurrentControlUnit;
        UpdateCurrentControlUnit(null, CurrentPlayer.Controller.ControlledEntity);
      }

      if (!Session.IsServer && Plugin.InputRegistry.InputThrottleMultiplayerSpecified && (CurrentTick % Plugin.InputThrottleMultiplayer) != 0)
        return;

      Input.UpdateInputs();

      UpdateCurrentGridInputs();
    }

    public override void Simulate()
    {
    }

    private void UpdateCurrentControlUnit(IMyControllableEntity oldControlUnit, IMyControllableEntity newControlUnit)
    {
      var oldControllable = CurrentControllable;
      CurrentControllable = newControlUnit as IMyCockpit;

      if (CurrentControllable != null && oldControllable == null)
      {
        CurrentGrid = CurrentControllable?.CubeGrid;

        if (Input != null)
        {
          Input.IsAnalogInputActive = Plugin.InputActiveByDefault;
          MyPluginLog.Debug($"Attached to new grid, analog input active: {Input.IsAnalogInputActive}");
        }
      }
      else if (CurrentControllable == null && oldControllable != null)
      {
        if (!Plugin.ControllerPatched && oldControllable is Sandbox.Game.Entities.MyCockpit oldCockpit)
        {
          MyPluginLog.Debug("Detached from grid, clearing old analog state");

          oldCockpit.EntityThrustComponent.AutopilotEnabled = false;
          oldCockpit.EntityThrustComponent.AutoPilotControlThrust = Vector3.Zero;
          oldCockpit.GridGyroSystem.AutopilotEnabled = false;
          oldCockpit.GridGyroSystem.ControlTorque = Vector3.Zero;
        }
      }
    }

    private void OnActionTriggered(object _sender, GameAction action)
    {
      if (action == GameAction.SwitchAnalogInputActive && !Plugin.ControllerPatched && !Input.IsAnalogInputActive && CurrentControllable is Sandbox.Game.Entities.MyCockpit cockpit)
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

      var CurrentCockpit = CurrentControllable as Sandbox.Game.Entities.MyCockpit;
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

      var CurrentCockpit = CurrentControllable as Sandbox.Game.Entities.MyCockpit;
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

      var CurrentCockpit = CurrentControllable as Sandbox.Game.Entities.MyCockpit;
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
      if (CurrentControllable == null || !Input.IsAnalogInputActive)
        return;


      if (!(CurrentControllable is Sandbox.Game.Entities.MyCockpit CurrentCockpit))
        return;

      if (Plugin.ControllerPatched)
      {
        CurrentCockpit.WheelJump(Input.IsInputActive(GameAction.WheelJump));
        CurrentCockpit.TryEnableBrakes(Input.IsInputActive(GameAction.Brake));

        return;
      }
      
      // Fallback for if analog patch fails

      // Inject normally, to hopefully work with recorder
      CurrentControllable.MoveAndRotate(Input.MovementVector, new Vector2(Input.RotationVector.X, Input.RotationVector.Y), Input.RotationVector.Z);

      Matrix orientMatrix;
      CurrentCockpit.Orientation.GetMatrix(out orientMatrix);
      if (CurrentCockpit.ControlThrusters && CurrentCockpit.EntityThrustComponent != null)
      {
        CurrentCockpit.EntityThrustComponent.AutopilotEnabled = true;
        if (Input.MovementVector.LengthSquared() > 0.0f)
        {
          Vector3 controlThrust;
          Vector3.RotateAndScale(ref Input.MovementVector, ref orientMatrix, out controlThrust);
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

      // Needs override method to work without patches
      /*
      if (CurrentCockpit.ControlWheels && CurrentCockpit.GridWheels != null)
      {
        CurrentCockpit.GridWheels.Brake = Input.IsInputActive(GameAction.Brake);
        CurrentCockpit.GridWheels.AngularVelocity += Input.MovementVector;
      }
      */

      CurrentCockpit.EntityThrustComponent.MarkDirty();
      CurrentCockpit.GridGyroSystem.MarkDirty();
    }
  }

}
