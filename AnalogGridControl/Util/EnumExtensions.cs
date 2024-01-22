using AnanaceDev.AnalogGridControl.InputMapping;
using System.Reflection;
using System.Text.RegularExpressions;

namespace AnanaceDev.AnalogGridControl.Util
{

  public static class EnumExtensions
  {
    public static string WordifyEnum(object obj)
    {
      var rex = new Regex("(?<=[a-z])(?<x>[A-Z0-9])|(?<=.)(?<x>[A-Z0-9])(?=[a-z])");
      return rex.Replace(obj.ToString() , " ${x}");
    }

    public static string GetHumanReadableEnumName(object obj)
    {
      MemberInfo[] memInfo = obj.GetType().GetMember(obj.ToString());
      if (memInfo != null && memInfo.Length > 0)
      {
        object[] attrs = memInfo[0].GetCustomAttributes(typeof(EnumDescriptionAttribute), false);
        if (attrs != null && attrs.Length > 0)
          return ((EnumDescriptionAttribute)attrs[0]).Name;
      }

      return WordifyEnum(obj);
    }

    public static string GetEnumDescription(object obj)
    {
      MemberInfo[] memInfo = obj.GetType().GetMember(obj.ToString());
      if (memInfo == null || memInfo.Length == 0)
        return null;

      object[] attrs = memInfo[0].GetCustomAttributes(typeof(EnumDescriptionAttribute), false);
      if (attrs == null || attrs.Length == 0)
        return null;

      return ((EnumDescriptionAttribute)attrs[0]).Description;
    }

#region GameAction
    public static string Wordify(this GameAction action) => WordifyEnum(action);
    public static string GetHumanReadableName(this GameAction action) => GetHumanReadableEnumName(action);
    public static string GetDescription(this GameAction action) => GetEnumDescription(action);
#endregion

#region GameAxis
    public static string Wordify(this GameAxis axis) => WordifyEnum(axis);
    public static string GetHumanReadableName(this GameAxis axis) => GetHumanReadableEnumName(axis);
    public static string GetDescription(this GameAxis axis) => GetEnumDescription(axis);
#endregion
  }

}
