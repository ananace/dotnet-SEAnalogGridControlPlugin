using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using AnanaceDev.AnalogGridControl.InputMapping;
using SharpDX.DirectInput;

namespace AnanaceDev.AnalogGridControl.Util
{

  public class SerializableDeviceRange : IXmlSerializable
  {
    public DeviceAxis Axis { get; set; }
    public int? RangeMin { get; set; }
    public int? RangeMax { get; set; }

    public SerializableDeviceRange(DeviceAxis axis, InputRange range)
    {
      Axis = axis;
      RangeMin = range.Minimum;
      RangeMax = range.Maximum;
    }

#region XML Serialization
    public void WriteXml(XmlWriter writer)
    {
      writer.WriteAttributeString("Axis", Axis.ToString());
      if (RangeMin.HasValue)
        writer.WriteAttributeString("Min", RangeMin.Value.ToString());
      if (RangeMax.HasValue)
        writer.WriteAttributeString("Max", RangeMax.Value.ToString());
    }

    public void ReadXml(XmlReader reader)
    {
      try
      {
        if (reader.GetAttribute("Axis") is string axis)
          Axis = (DeviceAxis)Enum.Parse(typeof(DeviceAxis), axis);

        if (reader.GetAttribute("Min") is string min && int.TryParse(min, out int minInt))
          RangeMin = minInt;
        if (reader.GetAttribute("Max") is string max && int.TryParse(max, out int maxInt))
          RangeMax = maxInt;
      }
      catch (Exception ex)
      {
        MyPluginLog.Warning($"Failed to parse an axis range, skipping. {ex}");
      }

      reader.Skip();
    }

    public XmlSchema GetSchema()
    {
      return(null);
    }
#endregion
  }

}
