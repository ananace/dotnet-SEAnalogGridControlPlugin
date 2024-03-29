using AnanaceDev.AnalogGridControl.InputMapping;
using AnanaceDev.AnalogGridControl.Util;
using SharpDX.DirectInput;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System;

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
    public DirectInput DInput { get; private set; } = null;
    [XmlIgnore]
    public DeviceInstance Device { get; private set; } = null;
    [XmlIgnore]
    public Joystick Joystick { get; private set; } = null;
    [XmlIgnore]
    public bool IsInitialized { get; private set; } = false;

    Dictionary<DeviceAxis, InputRange> _DefaultRanges = new Dictionary<DeviceAxis, InputRange>();
    Dictionary<DeviceAxis, InputRange> _Ranges = new Dictionary<DeviceAxis, InputRange>();

    [XmlIgnore]
    public bool IsValid => DInput != null && Device != null && Joystick != null && IsInitialized;
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
    [XmlArray("Axes"), XmlArrayItem("Range")]
    public SerializableDeviceRange[] SerializableRanges {
      get { return _Ranges.Select(axis => new SerializableDeviceRange(axis.Key, axis.Value)).ToArray(); }
      set {
        foreach (var loaded in value)
        {
          if (!_Ranges.ContainsKey(loaded.Axis))
            continue;

          var existing = _DefaultRanges[loaded.Axis];
          _Ranges[loaded.Axis] = new InputRange { Minimum = loaded.RangeMin ?? existing.Minimum, Maximum = loaded.RangeMax ?? existing.Maximum };
        }
      }
    }

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
    public JoystickState CurrentState { get; private set; } = new JoystickState();
    JoystickState _LastState, _InitialState = null;
    List<DeviceAxis> _PotentiallyBogusAxes = new List<DeviceAxis>();

    public event Action<InputDevice> OnAcquired;
    public event Action<InputDevice> OnUnacquired;

    bool _IsDisposed = false;

    public void Init(DirectInput dinput, DeviceInstance instance)
    {
      MyPluginLog.Debug($"{instance.InstanceName}/{instance.InstanceGuid} - Initializing");

      DInput = dinput;
      Device = instance;
      Joystick = new Joystick(dinput, instance.InstanceGuid);
      IsInitialized = true;

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

            _DefaultRanges[(DeviceAxis)axis] = range;
          } catch {}
        }

        _Ranges = _DefaultRanges;
        MyPluginLog.Debug($"{DeviceName} - Has {Buttons} button(s), {POVHats} hat(s), and {Axes.Count()} axis(es); {string.Join(", ", Axes.Select((a) => a.ToString()))}");
      }
      else
      {
        MyPluginLog.Warning($"{DeviceName} - Failed to retrieve joystick object.");
      }
    }

    public void Uninit()
    {
      if (IsAcquired)
        Unacquire();

      Joystick?.Dispose();
      Joystick = null;

      IsInitialized = false;
    }

    public void Acquire()
    {
      if (IsAcquired)
        return;

      if (IsValid)
      {
        Joystick.Acquire();

        // Set up initial joystick state data after acquire
        _LastState = CurrentState = Joystick.GetCurrentState();
        _PotentiallyBogusAxes = Axes.ToList();
        IsAcquired = true;

        // Ensure binds are clean after acquire
        ResetBinds();

        MyPluginLog.Info($"{DeviceName} - Acquired");

        OnAcquired?.Invoke(this);
      }
      else
        MyPluginLog.Info($"{DeviceName} - Acquire failed");
    }

    public void Unacquire()
    {
      if (!IsAcquired)
        return;

      if (IsValid)
      {
        try { Joystick.Unacquire(); } catch {}
        IsAcquired = false;
        MyPluginLog.Info($"{DeviceName} - Unacquired");

        OnUnacquired?.Invoke(this);
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

    public void ResetBinds()
    {
      Binds.ForEach(bind => bind.Reset());
    }

    public InputRange GetRange(DeviceAxis axis)
    {
      if (_Ranges.ContainsKey(axis))
        return _Ranges[axis];
      return DefaultRange;
    }

    public bool IsPotentiallyBogus(DeviceAxis axis) => _PotentiallyBogusAxes.Contains(axis);

    public bool Update(bool runBinds = true)
    {
      if (!IsValid || !IsAcquired)
        return false;

      try
      {
        _LastState = CurrentState;
        CurrentState = Joystick.GetCurrentState();
        if (_InitialState == null)
          _InitialState = CurrentState;

        _PotentiallyBogusAxes.RemoveAll(axis => CurrentState.GetAxisValue(axis) != _InitialState.GetAxisValue(axis));

        if (!runBinds)
          return false;

        bool hasData = false;
        foreach (var bind in Binds)
        {
          bind.Reset();

          if (bind.InputAxis.HasValue && IsPotentiallyBogus(bind.InputAxis.Value))
            continue;

          if (bind.Apply(CurrentState, this))
            hasData = true;
        }
        return hasData;
      }
      catch (Exception ex)
      {
        MyPluginLog.Warning($"Device {DeviceName} failed to update state, disabling... {ex}");

        Uninit();
      }

      return false;
    }

    public Bind DetectBind()
    {
      if (!IsValid || !IsAcquired)
        return null;

      Update(false);

      for (int i = 0; i < Buttons; ++i)
        if (CurrentState.Buttons[i] && !_LastState.Buttons[i])
          return new Bind() { InputButton = i };

      foreach (var axis in Axes)
      {
        var curVal = CurrentState.GetAxisValueNormalized(axis, GetRange(axis));
        var oldVal = _LastState.GetAxisValueNormalized(axis, GetRange(axis));

        if ((curVal < 0.25f && oldVal > 0.25f) || (curVal > 0.75f && oldVal < 0.75f))
          return new Bind() { InputAxis = axis };
      }

      for (int i = 0; i < POVHats; ++i)
        foreach (var axis in Enum.GetValues(typeof(DeviceHatAxis)).Cast<DeviceHatAxis>())
          if (CurrentState.GetPOVAxis(axis, i) && !_LastState.GetPOVAxis(axis, i))
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
