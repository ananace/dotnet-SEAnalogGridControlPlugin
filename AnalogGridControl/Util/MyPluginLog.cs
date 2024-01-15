using System;
using VRage.Utils;

namespace AnanaceDev.AnalogGridControl.Util
{

  public enum MyPluginLogLevel
  {
    INFO,
    WARNING,
    DEBUG
  }

  public static class MyPluginLog
  {
    public static void Info(string Message)
    {
      WriteMessage(MyPluginLogLevel.INFO, Message);
    }

    public static void Warning(string Message)
    {
      WriteMessage(MyPluginLogLevel.WARNING, Message);
    }

    public static void Debug(string Message)
    {
#if DEBUG
      Console.WriteLine($"[AnalogGridControl|DEBUG] {Message}");
      WriteMessage(MyPluginLogLevel.DEBUG, Message);
#endif
    }

    private static void WriteMessage(MyPluginLogLevel Level, string Message)
    {
      MyLog.Default.WriteLine($"[AnalogGridControl|{Level}] {Message}");
    }
  }

}
