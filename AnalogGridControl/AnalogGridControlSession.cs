﻿using Sandbox.ModAPI;
using SharpDX.DirectInput;
using System.Collections.Generic;
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
        Input.RegisterInput(dev);

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

      if (!Session.IsServer && CurrentTick % Plugin.InputThrottleMultiplayer != 0)
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
          MyPluginLog.Debug($"Attached to grid, analog input active: {Input.IsAnalogInputActive}");
        }
      }
      else if (CurrentControllable == null && oldControllable != null)
      {
        MyPluginLog.Debug("Detached from grid, clearing old analog state");
        if (!Plugin.ControllerPatched && oldControllable is Sandbox.Game.Entities.MyCockpit oldCockpit)
        {
          oldCockpit.EntityThrustComponent.AutopilotEnabled = false;
          oldCockpit.EntityThrustComponent.AutoPilotControlThrust = Vector3.Zero;
          oldCockpit.GridGyroSystem.AutopilotEnabled = false;
          oldCockpit.GridGyroSystem.ControlTorque = Vector3.Zero;
        }
      }
    }

    private void OnActionTriggered(object _sender, InputAction action)
    {
      if (CurrentControllable == null || !Input.IsAnalogInputActive)
        return;

      switch (action)
      {
        case InputAction.SwitchHandbrake: CurrentControllable.SwitchHandbrake(); break;
        case InputAction.SwitchLights: CurrentControllable.SwitchLights(); break;
        case InputAction.SwitchDamping: CurrentControllable.SwitchDamping(); break;
        case InputAction.SwitchReactors: CurrentControllable.SwitchReactors(); break;
        case InputAction.SwitchLandingGears: CurrentControllable.SwitchLandingGears(); break;

        case InputAction.SwitchAnalogInputActive:
          if (!Input.IsAnalogInputActive && !Plugin.ControllerPatched && CurrentControllable is Sandbox.Game.Entities.MyCockpit cockpit)
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
          } break;
      }

      var CurrentCockpit = CurrentControllable as Sandbox.Game.Entities.MyCockpit;
      if (CurrentCockpit == null)
        return;

      switch (action)
      {
        case InputAction.ToolbarAction1: CurrentCockpit.Toolbar.ActivateItemAtIndex(0); break;
        case InputAction.ToolbarAction2: CurrentCockpit.Toolbar.ActivateItemAtIndex(1); break;
        case InputAction.ToolbarAction3: CurrentCockpit.Toolbar.ActivateItemAtIndex(2); break;
        case InputAction.ToolbarAction4: CurrentCockpit.Toolbar.ActivateItemAtIndex(3); break;
        case InputAction.ToolbarAction5: CurrentCockpit.Toolbar.ActivateItemAtIndex(4); break;
        case InputAction.ToolbarAction6: CurrentCockpit.Toolbar.ActivateItemAtIndex(5); break;
        case InputAction.ToolbarAction7: CurrentCockpit.Toolbar.ActivateItemAtIndex(6); break;
        case InputAction.ToolbarAction8: CurrentCockpit.Toolbar.ActivateItemAtIndex(7); break;
        case InputAction.ToolbarAction9: CurrentCockpit.Toolbar.ActivateItemAtIndex(8); break;
        case InputAction.ToolbarAction10: CurrentCockpit.Toolbar.ActivateItemAtIndex(9); break;
      }
    }

    private void OnActionBegin(object _sender, InputAction action)
    {
      if (CurrentControllable == null || !Input.IsAnalogInputActive)
        return;

      var CurrentCockpit = CurrentControllable as Sandbox.Game.Entities.MyCockpit;
      if (CurrentCockpit == null)
        return;

      switch (action)
      {
        case InputAction.FirePrimary: CurrentCockpit.BeginShoot(MyShootActionEnum.PrimaryAction); break;
        case InputAction.FireSecondary: CurrentCockpit.BeginShoot(MyShootActionEnum.SecondaryAction); break;
        case InputAction.FireTertiary: CurrentCockpit.BeginShoot(MyShootActionEnum.TertiaryAction); break;
      }
    }

    private void OnActionEnd(object _sender, InputAction action)
    {
      if (CurrentControllable == null || !Input.IsAnalogInputActive)
        return;

      var CurrentCockpit = CurrentControllable as Sandbox.Game.Entities.MyCockpit;
      if (CurrentCockpit == null)
        return;

      switch (action)
      {
        case InputAction.FirePrimary: CurrentCockpit.EndShoot(MyShootActionEnum.PrimaryAction); break;
        case InputAction.FireSecondary: CurrentCockpit.EndShoot(MyShootActionEnum.SecondaryAction); break;
        case InputAction.FireTertiary: CurrentCockpit.EndShoot(MyShootActionEnum.TertiaryAction); break;
      }
    }

    private void UpdateCurrentGridInputs()
    {
      if (CurrentControllable == null || !Input.IsAnalogInputActive)
        return;


      if (!Plugin.ControllerPatched && CurrentControllable is Sandbox.Game.Entities.MyCockpit CurrentCockpit)
      {
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
          CurrentCockpit.GridWheels.Brake = Input.IsInputActive(InputAction.Brake);
          CurrentCockpit.GridWheels.AngularVelocity += Input.MovementVector;
        }
        */

        CurrentCockpit.EntityThrustComponent.MarkDirty();
        CurrentCockpit.GridGyroSystem.MarkDirty();
      }
    }
  }

}