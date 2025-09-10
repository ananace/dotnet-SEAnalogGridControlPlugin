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
    public bool InputThrottleMultiplayerSpecified => InputThrottleMultiplayer > 1;

    public bool DiscoverDevices(DirectInput dinput, bool rediscover = false, bool verbose = true)
    {
      if (verbose)
        MyPluginLog.Info("Checking for attached DirectInput devices...");
      var dinputDevices = dinput.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly).ToList();
      var unclaimedDevices = Devices.ToList();

      if (verbose)
        MyPluginLog.Info($"Found {dinputDevices.Count} DirectInput devices");

      bool dirty = false;
      var initDevice = new Action<InputDevice, DeviceInstance>((dev, din) => {
        if (verbose)
          MyPluginLog.Info(String.Format("- {0} matched existing device {1}", (din.InstanceGuid == dev.DeviceUUID && din.InstanceName == dev.DeviceName) ? "Perfectly" : "Partially", din.InstanceName));

        if (dev.IsInitialized) 
          return;

        dev.Init(dinput, din);
        if (rediscover)
          dirty = true;
      });

      // Claim disovered devices first by UUID, then by name and discovery order
      ClaimDevices(dinputDevices, unclaimedDevices, (din, dev) => din.DeviceUUID == dev.InstanceGuid && din.DeviceName == dev.InstanceName, initDevice);
      ClaimDevices(dinputDevices, unclaimedDevices, (din, dev) => din.DeviceName == dev.InstanceName, (dev, din) => {
        initDevice(dev, din);
        if (verbose)
          MyPluginLog.Info("  Used an unmatched device based on name + order, as no UUIDs perfectly matched.");
      });

      // Handle dinput devices that didn't find a claim
      foreach (var dev in dinputDevices)
      {
        if (verbose)
          MyPluginLog.Info($"- Found new device {dev.InstanceName}");

        var device = new InputDevice();
        device.Init(dinput, dev);

        Devices.Add(device);
        dirty = true;
      }

      return dirty;
    }

    void ClaimDevices(IList<DeviceInstance> toHandle, IList<InputDevice> unclaimed, Func<InputDevice, DeviceInstance, bool> lambda, Action<InputDevice, DeviceInstance> onClaim)
    {
      List<DeviceInstance> handled = new List<DeviceInstance>();
      foreach (var device in toHandle)
      {
        if (!(unclaimed.FirstOrDefault(d => lambda(d, device)) is InputDevice claimed))
          continue;

        handled.Add(device);
        unclaimed.Remove(claimed);

        onClaim.Invoke(claimed, device);
      }

      handled.ForEach(dev => toHandle.Remove(dev));
    }

    public void Cleanup()
    {
      Devices.ForEach(dev => dev.CleanupBinds());
      Devices.RemoveAll(dev => !dev.Binds.Any());
    }

    public void Dispose()
    {
      Devices.ForEach(dev => dev.Dispose());
    }
  }

}
