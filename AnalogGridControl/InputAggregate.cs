using AnanaceDev.AnalogGridControl.InputMapping;
using AnanaceDev.AnalogGridControl.Util;
using SharpDX.DirectInput;
using System.Collections.Generic;
using System.Linq;
using System;
using VRageMath;

namespace AnanaceDev.AnalogGridControl
{

  public class InputAggregate
  {
    public bool IsAnalogInputActive { get; set; }

    public event EventHandler<GameAction> ActionTriggered;
    public event EventHandler<GameAction> ActionBegin;
    public event EventHandler<GameAction> ActionEnd;

    /// Is the input activated for this tick?
    /// Only true for the first tick the input activates
    public bool IsInputJustActivated(GameAction action)
    {
      return IsInputActive(action) && !WasInputActive(action);
    }
    public bool IsInputJustDeactivated(GameAction action)
    {
      return WasInputActive(action) && !IsInputActive(action);
    }
    /// Is the input currently active
    public bool IsInputActive(GameAction action)
    {
      return _Actions.Contains(action);
    }
    public bool WasInputActive(GameAction action)
    {
      return _LastActions.Contains(action);
    }

    float ForwardMult = -1;
    DateTime ForwardMultInvertAt = DateTime.MinValue;
    
    public Vector3 _MovementVector = Vector3.Zero;
    public Vector3 _RotationVector = Vector3.Zero;
    public Vector2 _CameraRotationVector = Vector2.Zero;
    public float _AccelForce = 0;
    public float _BrakeForce = 0;

    public Vector3 MovementVector => _MovementVector;
    public Vector3 RotationVector => _RotationVector;
    public Vector2 CameraRotationVector => _CameraRotationVector;
    public float AccelForce => _BrakeForce;
    public float BrakeForce => _BrakeForce;

    public DirectInput DInput { get; set; }
    List<InputDevice> _Inputs = new List<InputDevice>();

    GameAction[] _LastActions = new GameAction[0];
    HashSet<GameAction> _Actions = new HashSet<GameAction>();

    public IReadOnlyList<InputDevice> Devices => _Inputs;

    public void RegisterInput(InputDevice device)
    {
      if (_Inputs.Contains(device))
        return;

      MyPluginLog.Debug($"InputAggregate - Registering {device.DeviceName}");
      _Inputs.Add(device);

      device.OnUnacquired += OnDeviceUnaquired;
    }

    public void UnregisterInput(InputDevice device)
    {
      if (!_Inputs.Contains(device))
        return;

      MyPluginLog.Debug($"InputAggregate - Unregistering {device.DeviceName}");
      _Inputs.Remove(device);

      device.OnUnacquired -= OnDeviceUnaquired;
    }

    void OnDeviceUnaquired(InputDevice device)
    {
      // Stop all currently active action binds fed by the device
      device.Binds
        .Where(b => b.IsActionMapping && b.IsActive)
        .ForEach(b => {
          _Actions.Remove(b.MappingAction.Value);
        });
    }

    public void Reset()
    {
      _LastActions = _Actions.ToArray();
      _Actions.Clear();

      _MovementVector = Vector3.Zero;
      _RotationVector = Vector3.Zero;
      _CameraRotationVector = Vector2.Zero;
      _BrakeForce = 0;
      _AccelForce = 0;
    }

    public void UpdateInputs()
    {
      Reset();

      foreach (var device in _Inputs)
      {
        if (!device.IsValid || !device.IsAcquired || !device.HasBinds)
        {
          // MyPluginLog.Debug($"InputAggregate - Skipping {device.DeviceName} !({device.IsValid} && {device.IsAcquired} && {device.HasBinds})");
          continue;
        }

        // MyPluginLog.Debug($"InputAggregate - Updating {device.DeviceName}");
        if (!device.Update())
        {
          // MyPluginLog.Debug($"InputAggregate - Skipping {device.DeviceName} updates due to no new data");
          continue;
        }

        foreach (var mapping in device.Binds)
        {
          if (mapping.IsAxisMapping)
          {
            float value = MyMath.Clamp(mapping.Value, -1f, 1f);
            switch (mapping.MappingAxis)
            {
              case GameAxis.StrafeForward:
              // case GameAxis.Accelerate:
                value *= ForwardMult; break;
              case GameAxis.StrafeLeft:
              case GameAxis.StrafeDown:
                value *= -1;
                break;
              case GameAxis.StrafeForwardBackward:
              case GameAxis.StrafeLeftRight:
              case GameAxis.StrafeUpDown:
                value = (value - 0.5f) * 2;
                break;

              // Pitch in SE is inverted compared to how joysticks usually handle it
              case GameAxis.TurnPitch: 
              case GameAxis.CameraPitch:
                value = (value - 0.5f) * -40; 
                break;
              case GameAxis.TurnYaw:
              case GameAxis.TurnRoll:
              case GameAxis.CameraYaw:
                value = (value - 0.5f) * 40;
                break;
            }

            switch (mapping.MappingAxis)
            {
              case GameAxis.StrafeForward:
              case GameAxis.StrafeBackward:
              case GameAxis.StrafeForwardBackward: _MovementVector.Z = Math.Abs(value) > Math.Abs(_MovementVector.Z) ? value : _MovementVector.Z; break;
              case GameAxis.StrafeLeft:
              case GameAxis.StrafeRight:
              case GameAxis.StrafeLeftRight: _MovementVector.X = Math.Abs(value) > Math.Abs(_MovementVector.X) ? value : _MovementVector.X; break;
              case GameAxis.StrafeUp:
              case GameAxis.StrafeDown:
              case GameAxis.StrafeUpDown: _MovementVector.Y = Math.Abs(value) > Math.Abs(_MovementVector.Y) ? value : _MovementVector.Y; break;

              // case GameAxis.Accelerate: _AccelForce = Math.Abs(value) > _AccelForce ? MyMath.Clamp(value, 0f, 1f) : _AccelForce; break;
              case GameAxis.Brake: _BrakeForce = Math.Abs(value) > _BrakeForce ? MyMath.Clamp(value, 0f, 1f) : _BrakeForce; break;

              case GameAxis.TurnPitch: _RotationVector.X = Math.Abs(value) > Math.Abs(_RotationVector.X) ? value : _RotationVector.X; break;
              case GameAxis.TurnYaw: _RotationVector.Y = Math.Abs(value) > Math.Abs(_RotationVector.Y) ? value : _RotationVector.Y; break;
              case GameAxis.TurnRoll: _RotationVector.Z = Math.Abs(value) > Math.Abs(_RotationVector.Z) ? value : _RotationVector.Z; break;
              
              case GameAxis.CameraPitch: _CameraRotationVector.X = Math.Abs(value) > Math.Abs(_CameraRotationVector.X) ? value : _CameraRotationVector.X; break;
              case GameAxis.CameraYaw: _CameraRotationVector.Y = Math.Abs(value) > Math.Abs(_CameraRotationVector.Y) ? value : _CameraRotationVector.Y; break;
            }
          }

          if (mapping.IsActionMapping)
          {
            bool value = mapping.IsActive;

            if (value)
              _Actions.Add(mapping.MappingAction.Value);
          }
        }
      }


      foreach (var action in System.Enum.GetValues(typeof(GameAction)).Cast<GameAction>())
      {
        if (IsInputJustActivated(action))
        {
          MyPluginLog.Debug($"StartActive {action}");
          ActionTriggered?.Invoke(this, action);
          ActionBegin?.Invoke(this, action);
        }
        if (IsInputJustDeactivated(action))
        {
          MyPluginLog.Debug($"StopActive {action}");
          ActionEnd?.Invoke(this, action);
        }
      }

      if (IsInputJustActivated(GameAction.InvertStrafeForward))
      {
        MyPluginLog.Info("Inverting forward strafe/accelerate");
        ForwardMult = -ForwardMult;
        ForwardMultInvertAt = DateTime.Now;
      }
      else if (IsInputJustDeactivated(GameAction.InvertStrafeForward) && (DateTime.Now - ForwardMultInvertAt) > TimeSpan.FromMilliseconds(500))
      {
        MyPluginLog.Info("Forward strafe/accelerate invert released after hold, re-inverting");
        ForwardMult = -ForwardMult;
      }

      if (IsInputJustActivated(GameAction.SwitchAnalogInputActive))
      {
        MyPluginLog.Info("Toggling analog input active");
        IsAnalogInputActive = !IsAnalogInputActive;
      }
    }
  }

}
