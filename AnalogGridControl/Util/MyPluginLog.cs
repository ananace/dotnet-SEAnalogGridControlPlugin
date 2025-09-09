using System;
using System.IO;
using VRage.FileSystem;
using VRage.Utils;

namespace AnanaceDev.AnalogGridControl.Util
{

  public enum MyPluginLogLevel
  {
    ERROR,
    INFO,
    WARNING,
    DEBUG
  }

  public static class MyPluginLog
  {
    private const string FileName = "AnalogGridControlPlugin.log";
    private static string FilePath => Path.Combine(MyFileSystem.UserDataPath, FileName);

    private static Lazy<string> _LogFile = new Lazy<string>(() => {
      System.IO.File.WriteAllText(FilePath, $"{DateTime.Now.ToString("s")} Logging started.\n");
      return FilePath;
    });
    private static string LogFile => _LogFile.Value;

    public static void Info(string Message)
    {
      WriteMessage(MyPluginLogLevel.INFO, Message);
    }

    public static void Warning(string Message)
    {
      WriteMessage(MyPluginLogLevel.WARNING, Message);
    }

    public static void Error(string Message)
    {
      WriteMessage(MyPluginLogLevel.ERROR, Message);
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
      File.AppendAllText(LogFile, $"{DateTime.Now.ToString("s")} {Level.ToString().PadLeft(7)} -> {Message}\n");
      MyLog.Default.WriteLine($"[AnalogGridControl|{Level}] {Message}");
    }
  }

}
