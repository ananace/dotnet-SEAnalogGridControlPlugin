using AnanaceDev.AnalogGridControl.InputMapping;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using SharpDX.DirectInput;
using AnanaceDev.AnalogGridControl.Util;
using System.Linq;
using System.Text;

namespace AnanaceDev.AnalogGridControl
{
  [XmlType("Joystick")]
  public class InputDevice : IDisposable
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

    [XmlIgnore]
    public bool IsValid => DInput != null && Device != null && Joystick != null;
    [XmlIgnore]
    public bool IsAcquired { get; private set; } = false;
    [XmlIgnore]
    public bool HasBinds => Binds.Count > 0;

    [XmlElement]
    public InputRange DefaultRange { get; set; } = new InputRange(ushort.MinValue, ushort.MaxValue);
    [XmlIgnore]
    public bool DefaultRangeSpecified => DefaultRange.Minimum != ushort.MinValue && DefaultRange.Maximum != ushort.MaxValue;
    [XmlIgnore]
    public IReadOnlyDictionary<DeviceAxis, InputRange> Ranges => _Ranges;

    [XmlIgnore]
    public IEnumerable<DeviceAxis> Axes => _Ranges.Keys;
    public static IEnumerable<DeviceAxis> MaxAxes => Enum.GetValues(typeof(DeviceAxis)).Cast<DeviceAxis>();
    [XmlIgnore]
    public int Buttons { get; private set; } = -1;
    public static int MaxButtons => 128;
    [XmlIgnore]
    public int POVHats { get; private set; } = -1;
    public static int MaxPOVHats => 4;

    [XmlIgnore]
    public JoystickState CurrentState { get; private set; }
    [XmlIgnore]
    public JoystickState LastState { get; private set; }

    bool _IsDisposed = false;

    public void Init(DirectInput dinput, DeviceInstance instance)
    {
      MyPluginLog.Debug($"{instance.InstanceName}/{instance.InstanceGuid} - Initializing");

      DInput = dinput;
      Device = instance;
      Joystick = new Joystick(dinput, instance.InstanceGuid);

      DeviceName = instance.InstanceName;
      DeviceUUID = instance.InstanceGuid;

      if (Joystick != null)
      {
        Joystick.Properties.AxisMode = DeviceAxisMode.Absolute;

        // Acquire device capabilities
        try {
          var cap = Joystick.Capabilities;

          Buttons = cap.ButtonCount;
          POVHats = cap.PovCount;
        } catch {}

        // Grab input ranges for handled axes
        foreach (var axis in Enum.GetValues(typeof(DeviceAxis)))
        {
          try {
            var obj = Joystick.GetObjectInfoByOffset((int)axis);
            var props = Joystick.GetObjectPropertiesById(obj.ObjectId);
            var range = props.Range;

            _Ranges[(DeviceAxis)axis] = range;
          } catch {}
        }

        MyPluginLog.Debug($"{instance.InstanceName} - Has {Buttons} button(s), {POVHats} hat(s), and {Axes.Count()} axis(es); {string.Join(", ", Axes.Select((a) => a.ToString()))}");
      }
      else
      {
        MyPluginLog.Warning($"{instance.InstanceName} - Failed to retrieve joystick object.");
      }
    }

    public void Acquire()
    {
      if (IsAcquired)
        return;

      if (IsValid)
      {
        Joystick.Acquire();
        LastState = CurrentState = Joystick.GetCurrentState();
        IsAcquired = true;
        MyPluginLog.Info($"{Device.InstanceName} - Acquired");
      }
      else
        MyPluginLog.Info($"{Device.InstanceName} - Acquire failed");
    }

    public void Unaquire()
    {
      if (!IsAcquired)
        return;

      if (IsValid)
      {
        try { Joystick.Unacquire(); } catch {}
        IsAcquired = false;
        MyPluginLog.Info($"{Device.InstanceName} - Unacquired");
      }

      Binds.ForEach(bind => bind.Reset());
    }

    public void Dispose()
    {
      if (_IsDisposed)
        return;

      _IsDisposed = true;
      if (Joystick != null)
        Joystick.Dispose();
    }

    public void CleanupBinds()
    {
      Binds.RemoveAll(bind => !bind.IsValid);
    }

    public InputRange GetRange(DeviceAxis axis)
    {
      if (_Ranges.ContainsKey(axis))
        return _Ranges[axis];
      return DefaultRange;
    }

    public void Update(bool runBinds = true)
    {
      if (!IsValid || !IsAcquired)
        return;

      try
      {
        LastState = CurrentState;
        CurrentState = Joystick.GetCurrentState();
        if (!runBinds)
          return;

        foreach (var bind in Binds)
          bind.Apply(CurrentState, this);
      }
      catch (Exception ex)
      {
        MyPluginLog.Warning($"Device {DeviceName} failed to update state, unaquiring... {ex}");

        Unaquire();
      }
    }

    public Bind DetectBind()
    {
      if (!IsValid || !IsAcquired)
        return null;

      Update(false);

      for (int i = 0; i < Buttons; ++i)
        if (CurrentState.Buttons[i] && !LastState.Buttons[i])
          return new Bind() { InputButton = i };

      foreach (var axis in Axes)
      {
        var curVal = CurrentState.GetAxisValueNormalized(axis, GetRange(axis));
        var oldVal = LastState.GetAxisValueNormalized(axis, GetRange(axis));

        if ((curVal < 0.25f && oldVal > 0.25f) || (curVal > 0.75f && oldVal < 0.75f))
          return new Bind() { InputAxis = axis };
      }

      for (int i = 0; i < POVHats; ++i)
        foreach (var axis in Enum.GetValues(typeof(DeviceHatAxis)).Cast<DeviceHatAxis>())
          if (CurrentState.GetPOVAxis(axis, i) && !LastState.GetPOVAxis(axis, i))
            return new Bind() { InputHatAxis = axis, InputHat = i };

      return null;
    }

    public override string ToString()
    {
      var str = new StringBuilder($"{DeviceName} - {Binds.Count} Binds");
      if (!IsValid)
        str.Append(" - Not available");
      return str.ToString();
    }
  }
}
