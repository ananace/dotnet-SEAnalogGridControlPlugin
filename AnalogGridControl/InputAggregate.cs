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
      return (_Actions.ContainsKey(action) && _Actions[action]);
    }
    public bool WasInputActive(GameAction action)
    {
      return (_LastActions.ContainsKey(action) && _LastActions[action]);
    }

    float ForwardMult = -1;
    DateTime ForwardMultInvertAt = DateTime.MinValue;
    
    public Vector3 MovementVector;
    public Vector3 RotationVector;

    public DirectInput DInput { get; set; }
    List<InputDevice> _Inputs = new List<InputDevice>();

    Dictionary<GameAction, bool> _LastActions = new Dictionary<GameAction, bool>();
    Dictionary<GameAction, bool> _Actions = new Dictionary<GameAction, bool>();

    public void RegisterInput(InputDevice device)
    {
      MyPluginLog.Debug($"InputAggregate - Registering {device.DeviceName}");
      _Inputs.Add(device);

      device.OnUnacquired += (_) => {
        // Clear input vectors in case they were fed by the lost device,
        // they will be rebuilt the next tick if another device feeds them
        MovementVector = Vector3.Zero;
        RotationVector = Vector3.Zero;

        // Stop all currently active action binds fed by the device
        device.Binds
          .Where(b => b.IsActionMapping && b.IsActive)
          .ForEach(b => {
            _Actions[b.MappingAction.Value] = false;
          });
      };
    }

    public void UpdateInputs()
    {
      _LastActions = new Dictionary<GameAction, bool>(_Actions);
      _Actions.Clear();
      foreach (var device in _Inputs)
      {
        if (!device.IsValid || !device.IsAcquired || !device.HasBinds)
        {
          // MyPluginLog.Debug($"InputAggregate - Skipping {device.DeviceName}");
          continue;
        }

        // MyPluginLog.Debug($"InputAggregate - Updating {device.DeviceName}");
        if (!device.Update())
          continue;

        foreach (var mapping in device.Binds)
        {
          if (mapping.IsAxisMapping)
          {
            float value = mapping.Value;
            switch (mapping.MappingAxis)
            {
              case GameAxis.StrafeForward: MovementVector.Z = value * ForwardMult; break;
              case GameAxis.StrafeForwardBackward: MovementVector.Z = (value - 0.5f) * 2; break;

              case GameAxis.StrafeLeftRight: MovementVector.X = (value - 0.5f) * 2; break;
              case GameAxis.StrafeUpDown: MovementVector.Y = (value - 0.5f) * 2; break;

              case GameAxis.TurnPitch: RotationVector.X = (value - 0.5f) * -40; break;
              case GameAxis.TurnYaw: RotationVector.Y = (value - 0.5f) * 40; break;
              case GameAxis.TurnRoll: RotationVector.Z = (value - 0.5f) * 40; break;
            }
          }

          if (mapping.IsActionMapping)
          {
            bool value = mapping.IsActive;
            _Actions[mapping.MappingAction.Value] = value;
          }
        }
      }

      foreach (var action in System.Enum.GetValues(typeof(GameAction)).Cast<GameAction>())
      {
        if (IsInputActive(action) && !WasInputActive(action))
        {
          MyPluginLog.Debug($"StartActive {action}");
          ActionTriggered?.Invoke(this, action);
          ActionBegin?.Invoke(this, action);
        }
        if (!IsInputActive(action) && WasInputActive(action))
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
