using AnanaceDev.AnalogGridControl.InputMapping;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using SharpDX.DirectInput;
using AnanaceDev.AnalogGridControl.Util;

namespace AnanaceDev.AnalogGridControl
{
  [XmlType("Joystick")]
  public class InputDevice
  {
    [XmlAttribute("Name")]
    public string DeviceName { get; set; }
    [XmlAttribute("GUID")]
    public Guid DeviceUUID { get; set; }
    [XmlArray("Binds"), XmlArrayItem("Bind")]
    public List<Bind> Binds { get; private set; } = new List<Bind>();

    [XmlIgnore]
    public DirectInput DInput { get; private set; }
    [XmlIgnore]
    public DeviceInstance Device { get; private set; }
    [XmlIgnore]
    public Joystick Joystick { get; private set; }

    Dictionary<DeviceAxis, InputRange> _Ranges = new Dictionary<DeviceAxis, InputRange>();

    bool _Acquired = false;
    [XmlIgnore]
    public bool IsValid => DInput != null && Device != null && Joystick != null;
    [XmlIgnore]
    public bool IsAcquired => _Acquired;
    [XmlIgnore]
    public bool HasBinds => Binds.Count > 0;

    [XmlElement]
    public InputRange DefaultRange { get; set; } = new InputRange(ushort.MinValue, ushort.MaxValue);
    [XmlIgnore]
    public bool DefaultRangeSpecified => DefaultRange.Minimum != ushort.MinValue && DefaultRange.Maximum != ushort.MaxValue;
    [XmlIgnore]
    public IReadOnlyDictionary<DeviceAxis, InputRange> Ranges => _Ranges;

    [XmlIgnore]
    public JoystickState CurrentState { get; private set; }
    [XmlIgnore]
    public JoystickState LastState { get; private set; }

    public void Acquire(DirectInput dinput, DeviceInstance instance)
    {
      MyPluginLog.Debug($"{instance.InstanceName}/{instance.InstanceGuid} - Acquiring");

      DInput = dinput;
      Device = instance;
      Joystick = new Joystick(dinput, instance.InstanceGuid);

      DeviceName = instance.InstanceName;
      DeviceUUID = instance.InstanceGuid;

      if (Joystick != null)
      {
        Joystick.Properties.AxisMode = DeviceAxisMode.Absolute;

        foreach (var axis in Joystick.GetObjects())
        {
          try {
            var props = Joystick.GetObjectPropertiesById(axis.ObjectId);
            var range = props.Range;

            if (Enum.IsDefined(typeof(DeviceAxis), axis.Offset))
              _Ranges[(DeviceAxis)axis.Offset] = range;
          } catch {}
        }

        MyPluginLog.Debug($"{instance.InstanceName} - Retrieved {_Ranges.Count} ranges as;");
        foreach (var range in _Ranges)
          MyPluginLog.Debug($"  - {range.Key} => {range.Value.Minimum}-{range.Value.Maximum}");
      }
      else
      {
        MyPluginLog.Warning($"{instance.InstanceName} - Failed to retrieve joystick object.");
      }

      if (IsValid)
      {
        Joystick.Acquire();
        LastState = CurrentState = Joystick.GetCurrentState();
        _Acquired = true;
        MyPluginLog.Debug($"{Device.InstanceName} - Acquired");
      }
      else
        MyPluginLog.Debug($"{Device.InstanceName} - Acquire failed");
    }

    public InputRange GetRange(DeviceAxis axis)
    {
      if (_Ranges.ContainsKey(axis))
        return _Ranges[axis];
      return DefaultRange;
    }

    public void Update()
    {
      if (!IsValid || !IsAcquired)
        return;

      LastState = CurrentState;
      CurrentState = Joystick.GetCurrentState();

      foreach (var bind in Binds)
        bind.Apply(CurrentState, this);
    }
  }
}
