using System;

namespace AnanaceDev.AnalogGridControl.Util
{

  [AttributeUsage(AttributeTargets.Field)]
  public class EnumDescriptionAttribute : Attribute
  {
    public string Name { get; set;}
    public string Description { get; set; }

    public EnumDescriptionAttribute(string Name, string Description = null)
    {
      this.Name = Name;
      this.Description = Description;
    }
  };

}
