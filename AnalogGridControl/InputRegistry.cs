using AnanaceDev.AnalogGridControl.Util;
using SharpDX.DirectInput;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using System;

namespace AnanaceDev.AnalogGridControl
{

  [XmlRoot]
  public class InputRegistry : IDisposable
  {
    [XmlArray]
    public List<InputDevice> Devices { get; private set; } = new List<InputDevice>();

    public bool InputActiveByDefault { get; set; } = true;

    public ushort InputThrottleMultiplayer { get; set; } = 1;
    [XmlIgnore]
    public bool InputThrottleMultiplayerSpecified => InputThrottleMultiplayer <= 1;


    public bool HasDevice(DeviceInstance device)
    {
      return Devices.Any((reg) => reg.DeviceName == device.InstanceName);
    }

    // XXX: Some different Logitech devices seem to grab the same instance UUIDs
    public InputDevice GetDevice(DeviceInstance device)
    {
      var potential = Devices.Where(reg => reg.DeviceName == device.InstanceName);
      var attempted = potential.FirstOrDefault(reg => reg.DeviceUUID == device.InstanceGuid);
      if (attempted == null)
      {
        MyPluginLog.Warning($"Found instances for '{device.InstanceName}', but none which match the given UUID, using the first discovered");
        return potential.First();
      }
      return attempted;
    }

    public bool DiscoverDevices(DirectInput dinput, bool rediscover = false, bool verbose = true)
    {
      if (verbose)
        MyPluginLog.Info("Checking for attached DirectInput devices...");
      var devices = dinput.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly) as IReadOnlyList<DeviceInstance>;

      bool dirty = false;
      foreach (var device in devices)
      {
        InputDevice dev;
        if (Plugin.InputRegistry.HasDevice(device))
        {
          if (verbose)
            MyPluginLog.Info($"- Existing device '{device.InstanceName}' found.");
          dev = Plugin.InputRegistry.GetDevice(device);
        }
        else
        {
          if (verbose)
            MyPluginLog.Info($"- New device '{device.InstanceName}' found.");
          dev = new InputDevice();
        }

        if (!dev.IsInitialized)
        {
          dev.Init(dinput, device);
          if (rediscover)
            dirty = true;
        }

        if (!Plugin.InputRegistry.HasDevice(device))
        {
          Plugin.InputRegistry.Devices.Add(dev);
          dirty = true;
        }
      }
      return dirty;
    }

    public void Cleanup()
    {
      Devices.ForEach(dev => dev.CleanupBinds());
    }

    public void Dispose()
    {
      Devices.ForEach(dev => dev.Dispose());
    }
  }

}
