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

    public event EventHandler<InputAction> ActionTriggered;
    public event EventHandler<InputAction> ActionBegin;
    public event EventHandler<InputAction> ActionEnd;

    /// Is the input activated for this tick?
    /// Only true for the first tick the input activates
    public bool IsInputJustActivated(InputAction action)
    {
      return IsInputActive(action) && !WasInputActive(action);
    }
    public bool IsInputJustDeactivated(InputAction action)
    {
      return WasInputActive(action) && !IsInputActive(action);
    }
    /// Is the input currently active
    public bool IsInputActive(InputAction action)
    {
      return (_Actions.ContainsKey(action) && _Actions[action]);
    }
    public bool WasInputActive(InputAction action)
    {
      return (_LastActions.ContainsKey(action) && _LastActions[action]);
    }

    float ForwardMult = -1;
    DateTime ForwardMultInvertAt = DateTime.MinValue;
    
    public Vector3 MovementVector;
    public Vector3 RotationVector;

    public DirectInput DInput { get; set; }
    List<InputDevice> _Inputs = new List<InputDevice>();

    Dictionary<InputAction, bool> _LastActions = new Dictionary<InputAction, bool>();
    Dictionary<InputAction, bool> _Actions = new Dictionary<InputAction, bool>();

    public void RegisterInput(InputDevice device)
    {
      MyPluginLog.Debug($"InputAggregate - Registering {device.DeviceName}");
      _Inputs.Add(device);
    }

    public void UpdateInputs()
    {
      _LastActions = new Dictionary<InputAction, bool>(_Actions);
      _Actions.Clear();
      foreach (var device in _Inputs)
      {
        if (!device.IsValid || !device.IsAcquired || !device.HasBinds)
        {
          // MyPluginLog.Debug($"InputAggregate - Skipping {device.DeviceName}");
          continue;
        }

        // MyPluginLog.Debug($"InputAggregate - Updating {device.DeviceName}");
        device.Update();

        foreach (var mapping in device.Binds)
        {
          if (mapping.IsAxisMapping)
          {
            float value = mapping.Value;
            switch (mapping.MappingAxis)
            {
              case InputAxis.StrafeForward: MovementVector.Z = value * ForwardMult; break;
              case InputAxis.StrafeForwardBackward: MovementVector.Z = (value - 0.5f) * 2; break;

              case InputAxis.StrafeLeftRight: MovementVector.X = (value - 0.5f) * 2; break;
              case InputAxis.StrafeUpDown: MovementVector.Y = (value - 0.5f) * 2; break;

              case InputAxis.TurnPitch: RotationVector.X = (value - 0.5f) * -40; break;
              case InputAxis.TurnYaw: RotationVector.Y = (value - 0.5f) * 40; break;
              case InputAxis.TurnRoll: RotationVector.Z = (value - 0.5f) * 40; break;
            }
          }

          if (mapping.IsActionMapping)
          {
            bool value = mapping.IsActive;
            _Actions[mapping.MappingAction.Value] = value;
          }
        }
      }

      foreach (var action in System.Enum.GetValues(typeof(InputAction)).Cast<InputAction>())
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

      if (IsInputJustActivated(InputAction.InvertStrafeForward))
      {
        MyPluginLog.Info("Inverting forward strafe");
        ForwardMult = -ForwardMult;
        ForwardMultInvertAt = DateTime.Now;
      }
      else if (IsInputJustDeactivated(InputAction.InvertStrafeForward) && (DateTime.Now - ForwardMultInvertAt) > TimeSpan.FromMilliseconds(500))
      {
        MyPluginLog.Info("Forward strafe invert released after hold, re-inverting");
        ForwardMult = -ForwardMult;
      }

      if (IsInputJustActivated(InputAction.SwitchAnalogInputActive))
      {
        MyPluginLog.Info("Toggling analog input active");
        IsAnalogInputActive = !IsAnalogInputActive;
      }
    }
  }

}
