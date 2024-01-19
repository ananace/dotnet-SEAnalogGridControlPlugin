using AnanaceDev.AnalogGridControl.InputMapping;
using AnanaceDev.AnalogGridControl.Util;
using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Linq;
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
      return _Actions.HasFlag(action);
    }
    public bool WasInputActive(GameAction action)
    {
      return _LastActions.HasFlag(action);
    }

    float ForwardMult = -1;
    DateTime ForwardMultInvertAt = DateTime.MinValue;
    
    public Vector3 _MovementVector = Vector3.Zero;
    public Vector3 _RotationVector = Vector3.Zero;

    public Vector3 MovementVector => _MovementVector;
    public Vector3 RotationVector => _RotationVector;

    public DirectInput DInput { get; set; }
    List<InputDevice> _Inputs = new List<InputDevice>();

    GameAction _LastActions = GameAction.None;
    GameAction _Actions = GameAction.None;

    public IReadOnlyList<InputDevice> Devices => _Inputs;

    public void RegisterInput(InputDevice device)
    {
      MyPluginLog.Debug($"InputAggregate - Registering {device.DeviceName}");
      _Inputs.Add(device);

      device.OnUnacquired += (_) => {
        // Clear input vectors in case they were fed by the lost device,
        // they will be rebuilt the next tick if another device feeds them
        _MovementVector = Vector3.Zero;
        _RotationVector = Vector3.Zero;

        // Stop all currently active action binds fed by the device
        device.Binds
          .Where(b => b.IsActionMapping && b.IsActive)
          .ForEach(b => {
            _Actions &= ~b.MappingAction.Value;
          });
      };
    }

    public void UpdateInputs()
    {
      _LastActions = _Actions;
      _Actions = GameAction.None;

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
            float value = mapping.Value;
            switch (mapping.MappingAxis)
            {
              case GameAxis.StrafeForward: _MovementVector.Z = value * ForwardMult; break;
              case GameAxis.StrafeForwardBackward: _MovementVector.Z = (value - 0.5f) * 2; break;

              case GameAxis.StrafeLeftRight: _MovementVector.X = (value - 0.5f) * 2; break;
              case GameAxis.StrafeUpDown: _MovementVector.Y = (value - 0.5f) * 2; break;

              case GameAxis.TurnPitch: _RotationVector.X = (value - 0.5f) * -40; break;
              case GameAxis.TurnYaw: _RotationVector.Y = (value - 0.5f) * 40; break;
              case GameAxis.TurnRoll: _RotationVector.Z = (value - 0.5f) * 40; break;
            }
          }

          if (mapping.IsActionMapping)
          {
            bool value = mapping.IsActive;

            if (value)
              _Actions |= mapping.MappingAction.Value;
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
        MyPluginLog.Info("Inverting forward strafe");
        ForwardMult = -ForwardMult;
        ForwardMultInvertAt = DateTime.Now;
      }
      else if (IsInputJustDeactivated(GameAction.InvertStrafeForward) && (DateTime.Now - ForwardMultInvertAt) > TimeSpan.FromMilliseconds(500))
      {
        MyPluginLog.Info("Forward strafe invert released after hold, re-inverting");
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
