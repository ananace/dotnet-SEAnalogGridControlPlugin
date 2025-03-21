using AnanaceDev.AnalogGridControl.Util;
using SharpDX.DirectInput;
using System.Text;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Xml;
using System;

namespace AnanaceDev.AnalogGridControl.InputMapping
{

  public class Bind : IXmlSerializable
  {
    public DeviceAxis? InputAxis { get; set; } = null;
    public bool InputAxisInvert { get; set; } = false;
    public int? InputButton { get; set; } = null;
    public DeviceHatAxis? InputHatAxis { get; set; } = null;
    public int? InputHat { get; set; } = null;

    public GameAxis? MappingAxis { get; set; } = null;
    public GameAction? MappingAction { get; set; } = null;

    public float Value { get; private set; } = 0f;
    public bool IsActive { get; private set; } = false;

    public bool IsValid { get { 
      return (InputAxis.HasValue || InputButton.HasValue || InputHatAxis.HasValue)
        && (MappingAxis.HasValue || MappingAction.HasValue);
    } }

    public bool IsAxisMapping { get { return MappingAxis.HasValue; } }
    public bool IsActionMapping { get { return MappingAction.HasValue; } }

    public float Deadzone { get; set; } = 0.05f;
    /// Using the formula f(x) = a * x^3 + (1 - a) * x 
    /// 0 is linear, 1 is x^3
    public float Curve { get; set; } = 0.0f;

    public void Clear(bool onlyDevicePart = false)
    {
      // Reset values to defaults
      InputAxis = null;
      InputAxisInvert = false;
      InputButton = null;
      InputHatAxis = null;
      InputHat = null;
      
      if (!onlyDevicePart)
      {
        MappingAxis = null;
        MappingAction = null;
        Deadzone = 0.05f;
        Curve = 0.0f;
      }

      Reset();
    }

    public void Reset()
    {
      Value = 0.0f;
      IsActive = false;
    }
    public bool Apply(JoystickState state, InputDevice device)
    {
      int? intValue = null;
      float? floatValue = null;

      if (InputAxis.HasValue)
      {
        float value = state.GetAxisValueNormalized(InputAxis.Value, device.GetRange(InputAxis.Value));
        if (InputAxisInvert)
          value = 1.0f - value;

        // Uses an adaptive deadzone, i.e. the deadzone range will be removed from the value range.
        // For a 5% deadzone this means that an input value of 5% will return 0 and an input value of 95% will return 100%
        if (Deadzone > 0 && Deadzone < 1)
        {
          // Remap input into range -1 - 1 for easier deadzone math
          value = (value * 2) - 1;
          switch(MappingAxis?.GetDeadzonePoint() ?? DeadzonePoint.None)
          {
          case DeadzonePoint.End:
            if (Math.Abs(value) > 1 - Deadzone)
              value = Math.Sign(value);
            else
              value = value / (1 - Deadzone);
            break;

          case DeadzonePoint.Mid:
            if (Math.Abs(value) < Deadzone)
              value = 0;
            else
              value = (float)(Math.Sign(value) * MathExt.InverseLerp(Deadzone, 1.0, Math.Abs(value)));
            break;
          }
          // Remap value back to 0 - 1 range after deadzone calculation
          value = (value + 1) / 2;
        }
        else if (Deadzone >= 1)
          value = 0;

        if (Curve != 0.0f)
        {
          var curve = Math.Max(0.0f, Math.Min(1.0f, Curve));

          // Remap both halves of input so that the curve applies properly
          bool? positive = null;
          if ((MappingAxis?.GetDeadzonePoint() ?? DeadzonePoint.None) == DeadzonePoint.Mid)
          {
            positive = value >= 0.5f;
            value = value < 0.5f ? (0.5f - value) * 2 : (value - 0.5f) * 2;
          }

          value = curve * (float)Math.Pow(value, 3) + (1 - curve) * value;

          if (positive.HasValue)
            value = positive.Value ? value * 0.5f + 0.5f : 0.5f - value * 0.5f;
        }

        floatValue = value;
      }
      else if (InputButton.HasValue)
        intValue = state.Buttons[InputButton.Value] ? 1 : 0;
      else if (InputHatAxis.HasValue)
        intValue = state.GetPOVAxis(InputHatAxis.Value, InputHat) ? 1 : 0;

      if (intValue.HasValue || floatValue.HasValue)
      {
        if (floatValue.HasValue)
          Value = floatValue.Value;
        else
          Value = intValue.Value;

        IsActive = Value >= 0.75f;
        return true;
      }

      return false;
    }

    public override string ToString()
    {
      var builder = new StringBuilder();
      if (InputAxis.HasValue)
        builder.Append(InputAxis.Value);
      else if (InputButton.HasValue)
        builder.Append($"Btn[{InputButton.Value + 1}]");
      else if (InputHatAxis.HasValue)
        builder.Append($"Hat[{(InputHat ?? 0) + 1}] {InputHatAxis}");
      else
        builder.Append("<Unk>");

      builder.Append(" => ");

      if (MappingAxis.HasValue)
        builder.Append(MappingAxis.Value.GetHumanReadableName());
      else if (MappingAction.HasValue)
        builder.Append(MappingAction.Value.GetHumanReadableName());
      else
        builder.Append("<Unk>");

      return builder.ToString();
    }

    public void DebugPrint()
    {
      MyPluginLog.Debug("Bind:");
      MyPluginLog.Debug($"  InputAxis: {InputAxis}");
      MyPluginLog.Debug($"  InputAxisInvert: {InputAxisInvert}");
      MyPluginLog.Debug($"  InputButton: {InputButton}");
      MyPluginLog.Debug($"  InputHatAxis: {InputHatAxis}");
      MyPluginLog.Debug($"  InputHat: {InputHat}");
      MyPluginLog.Debug($"  MappingAxis: {MappingAxis}");
      MyPluginLog.Debug($"  MappingAction: {MappingAction}");
      MyPluginLog.Debug($"  Deadzone: {Deadzone}");
      MyPluginLog.Debug($"  Curve: {Curve}");
      MyPluginLog.Debug("");
      MyPluginLog.Debug($"  Value: {Value}");
      MyPluginLog.Debug($"  IsActive: {IsActive}");
    }

#region XML Serialization
    public void WriteXml(XmlWriter writer)
    {
      if (InputAxis.HasValue)
      {
        writer.WriteAttributeString("Axis", InputAxis.Value.ToString());
        if (InputAxisInvert)
          writer.WriteAttributeString("AxisInvert", InputAxisInvert.ToString());
        if (Deadzone != 0.05f)
          writer.WriteAttributeString("Deadzone", Deadzone.ToString());
        if (Curve != 0.0f)
          writer.WriteAttributeString("Curve", Curve.ToString());
      }
      else if (InputButton.HasValue)
        writer.WriteAttributeString("Button", InputButton.Value.ToString());
      else if (InputHatAxis.HasValue)
      {
        if (InputHat.HasValue)
          writer.WriteAttributeString("Hat", InputHat.Value.ToString());
        writer.WriteAttributeString("HatAxis", InputHatAxis.Value.ToString());
      }

      if (MappingAxis.HasValue)
        writer.WriteAttributeString("OutputAxis", MappingAxis.Value.ToString());
      else if (MappingAction.HasValue)
        writer.WriteAttributeString("OutputAction", MappingAction.Value.ToString());
    }

    public void ReadXml(XmlReader reader)
    {
      try
      {
        if (reader.GetAttribute("Axis") is string axis)
        {
          InputAxis = (DeviceAxis)Enum.Parse(typeof(DeviceAxis), axis);

          if (reader.GetAttribute("AxisInvert") is string invert)
            InputAxisInvert = bool.Parse(invert);
          if (reader.GetAttribute("Deadzone") is string deadzone)
            Deadzone = float.Parse(deadzone);
          if (reader.GetAttribute("Curve") is string curve)
            Curve = float.Parse(curve);
        }
        else if (reader.GetAttribute("Button") is string button)
          InputButton = int.Parse(button);
        else if (reader.GetAttribute("HatAxis") is string hatAxis)
        {
          InputHatAxis = (DeviceHatAxis)Enum.Parse(typeof(DeviceHatAxis), hatAxis);
          if (reader.GetAttribute("Hat") is string hat)
            InputHat = int.Parse(hat);
        }

        if (reader.GetAttribute("OutputAxis") is string outputAxis)
        {
          MappingAxis = (GameAxis)Enum.Parse(typeof(GameAxis), outputAxis);
          // For old invert setting
          if (reader.GetAttribute("OutputAxisInvert") is string invert)
            InputAxisInvert = bool.Parse(invert);
        }
        else if (reader.GetAttribute("OutputAction") is string action)
          MappingAction = (GameAction)Enum.Parse(typeof(GameAction), action);
      }
      catch (Exception ex)
      {
        MyPluginLog.Warning($"Failed to parse a bind, skipping. {ex}");
      }

      reader.Skip();
    }

    public XmlSchema GetSchema()
    {
      return(null);
    }
#endregion

#region Clone
    public Bind Clone()
    {
      return MemberwiseClone() as Bind;
    }

    public void ApplyValuesFrom(Bind other, bool onlyDevicePart = false)
    {
      Clear(onlyDevicePart);

      // Import relevant values from given bind
      if (other.InputAxis.HasValue)
      {
        InputAxis = other.InputAxis;
        if (onlyDevicePart)
          return;

        InputAxisInvert = other.InputAxisInvert;
        Deadzone = other.Deadzone;
        Curve = other.Curve;
      }
      else if (other.InputHatAxis.HasValue)
      {
        InputHatAxis = other.InputHatAxis;
        InputHat = other.InputHat;
      }
      else if (other.InputButton.HasValue)
        InputButton = other.InputButton;

      if (onlyDevicePart)
        return;

      if (other.MappingAxis.HasValue)
        MappingAxis = other.MappingAxis;
      else if (other.MappingAction.HasValue)
        MappingAction = other.MappingAction;
    }
#endregion
  }

}
