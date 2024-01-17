using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using SharpDX.DirectInput;

namespace AnanaceDev.AnalogGridControl
{

  [XmlRoot]
  public class InputRegistry
  {
    [XmlArray]
    public List<InputDevice> Devices { get; private set; } = new List<InputDevice>();

    public bool InputActiveByDefault { get; set; } = true;

    public ushort InputThrottleMultiplayer { get; set; } = 1;
    [XmlIgnore]
    public bool InputThrottleMultiplayerSpecified => InputThrottleMultiplayer <= 1;


    public bool HasDevice(DeviceInstance device)
    {
      return Devices.Any((reg) => reg.DeviceName == device.InstanceName || reg.DeviceUUID == device.InstanceGuid);
    }

    public InputDevice GetDevice(DeviceInstance device)
    {
      return Devices.First((reg) => reg.DeviceName == device.InstanceName || reg.DeviceUUID == device.InstanceGuid);
    }
  }

}
