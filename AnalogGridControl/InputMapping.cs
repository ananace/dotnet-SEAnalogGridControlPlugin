using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using SharpDX.DirectInput;

namespace AnanaceDev.AnalogGridControl
{

  public class InputMapping : IXmlSerializable
  {
    public DeviceAxis? InputAxis { get; set; } = null;
    public int? InputButton { get; set; } = null;

    public InputAxis? MappingAxis { get; set; } = null;
    public bool MappingAxisInvert { get; set; } = false;
    public InputAction? MappingAction { get; set; } = null;

    public bool IsActive { get; private set; }
    public bool IsAxisMapping { get { return MappingAxis.HasValue; } }
    public bool IsActionMapping { get { return MappingAction.HasValue; } }
    public float Value { get; private set; }

    public float Deadzone { get; set; } = 0.05f;
    public float Multiplier { get; set; } = 1.0f;
    /// Using the formula f(x) = a * x^3 + (1 - a) * x 
    /// 0 is linear, 1 is x^3
    public float Curve { get; set; } = 0.0f;

    public void Reset()
    {
      Value = 0.0f;
      IsActive = false;
    }
    public bool Apply(JoystickState state, InputDevice device)
    {
      int? newValue = null;
      float? mappedValue = null;

      if (InputAxis.HasValue)
      {
        switch (InputAxis)
        {
          case DeviceAxis.X: newValue = state.X; break;
          case DeviceAxis.Y: newValue = state.Y; break;
          case DeviceAxis.Z: newValue = state.Z; break;
          case DeviceAxis.RX: newValue = state.RotationX; break;
          case DeviceAxis.RY: newValue = state.RotationY; break;
          case DeviceAxis.RZ: newValue = state.RotationZ; break;
        }

        var range = device.GetRange(InputAxis.Value);

        mappedValue = ((float)newValue / (float)range.Maximum);
        if (MappingAxisInvert)
          mappedValue = 1.0f - mappedValue;

        if (mappedValue > 1.0f - Deadzone)
          mappedValue = 1.0f;
        else if (mappedValue > 0.5f - Deadzone / 2 && mappedValue < 0.5f + Deadzone / 2)
          mappedValue = 0.5f;
        else if (mappedValue < Deadzone)
          mappedValue = 0.0f;

        if (Curve != 0.0f)
        {
          var curve = Math.Max(0.0f, Math.Min(1.0f, Curve));
          mappedValue = curve * (float)Math.Pow(mappedValue.Value, 3) + (1 - curve) * mappedValue.Value;
        }
      }
      else if (InputButton.HasValue)
        newValue = state.Buttons[InputButton.Value] ? 1 : 0;

      if (newValue != null)
      {
        if (mappedValue.HasValue)
          Value = mappedValue.Value;
        else
          Value = newValue.Value;

        IsActive = Value >= 0.75f;

        /*
        if (MappingAxis.HasValue)
          MyPluginLog.Debug($"InputMapping {InputAxis}->{MappingAxis} (inv? {MappingAxisInvert}) => {newValue} -> {Value}");
        else
          MyPluginLog.Debug($"InputMapping {InputButton}->{MappingAction} => {newValue} -> {Value} ({IsActive})");
        */
      }

      return newValue != null;
    }

#region XML Serialization
    public void WriteXml(XmlWriter writer)
    {
      if (InputAxis.HasValue)
        writer.WriteAttributeString("Axis", InputAxis.Value.ToString());
      else if (InputButton.HasValue)
        writer.WriteAttributeString("Button", InputButton.Value.ToString());

      if (MappingAxis.HasValue)
      {
        writer.WriteAttributeString("OutputAxis", MappingAxis.Value.ToString());
        if (MappingAxisInvert)
          writer.WriteAttributeString("OutputAxisInvert", MappingAxisInvert.ToString());
      }
      else if (MappingAction.HasValue)
        writer.WriteAttributeString("OutputAction", MappingAction.Value.ToString());

      if (Deadzone != 0.05f)
        writer.WriteAttributeString("Deadzone", Deadzone.ToString());
      if (Multiplier != 1.0f)
        writer.WriteAttributeString("Multiplier", Multiplier.ToString());
      if (Curve != 0.0f)
        writer.WriteAttributeString("Curve", Curve.ToString());
    }

    public void ReadXml(XmlReader reader)
    {
      if (reader.GetAttribute("Axis") is string axis)
        InputAxis = (DeviceAxis)Enum.Parse(typeof(DeviceAxis), axis);
      else if (reader.GetAttribute("Button") is string button)
        InputButton = int.Parse(button);

      if (reader.GetAttribute("OutputAxis") is string outputAxis)
      {
        MappingAxis = (InputAxis)Enum.Parse(typeof(InputAxis), outputAxis);
        if (reader.GetAttribute("OutputAxisInvert") is string invert)
          MappingAxisInvert = bool.Parse(invert);
      }
      else if (reader.GetAttribute("OutputAction") is string action)
        MappingAction = (InputAction)Enum.Parse(typeof(InputAction), action);

      if (reader.GetAttribute("Deadzone") is string deadzone)
        Deadzone = float.Parse(deadzone);
      if (reader.GetAttribute("Multiplier") is string multiplier)
        Multiplier = float.Parse(multiplier);
      if (reader.GetAttribute("Curve") is string curve)
        Curve = float.Parse(curve);

      reader.ReadToNextSibling("Mapping");
    }

    public XmlSchema GetSchema()
    {
      return(null);
    }
#endregion
  }

}
