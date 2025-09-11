using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using AnanaceDev.AnalogGridControl.Util;
using HarmonyLib;
using SharpDX.DirectInput;
using VRage.FileSystem;
using VRage.Plugins;

namespace AnanaceDev.AnalogGridControl
{

  class Plugin : IPlugin, IDisposable
  {
    public const string Name = "AnalogGridControl";
    public static ushort Id = 4556; // CRC16 of 'AnalogGridControl'
    public static uint NetworkVersion = 1;
    private static readonly string ConfigFileName = $"{Name}.cfg";

    public static bool InputActiveByDefault => InputRegistry.InputActiveByDefault;
    public static ushort InputThrottleMultiplayer => InputRegistry.InputThrottleMultiplayer;
    public static bool ControllerPatched = false;

    public static DirectInput DInput = new DirectInput();
    public static InputRegistry InputRegistry = new InputRegistry();
    public static InputAggregate InputAggregate = new InputAggregate();

    bool _IsDisposed = false;
    uint _CurrentTick = 0;

    public void Init(object _gameObject)
    {
      MyPluginLog.Info($"Analog Grid Control {Assembly.GetExecutingAssembly().GetName().Version} Running");

      AttemptPatches();

      LoadMappings();

      ReadDevices();
    }

    public void Update()
    {
      _CurrentTick++;
      if (_CurrentTick % 1000 == 0)
      {
        bool verbose = false;
        if (InputAggregate.Devices.Any(dev => !dev.IsInitialized))
        {
          verbose = true;
          MyPluginLog.Info("Invalid devices in input aggregate, attempting rescan...");
        }

        if (InputRegistry.DiscoverDevices(DInput, true, verbose))
        {
          Plugin.InputRegistry.Devices.ForEach(dev => InputAggregate.RegisterInput(dev));
          InputAggregate.Devices.Where(dev => !dev.IsAcquired).ForEach((dev => dev.Acquire()));
        }

        // Devices that are still lost after an attempted re-acquire are dropped until next full rescan
        InputAggregate.Devices.Where(dev => !dev.IsInitialized).ToList().ForEach(dev => InputAggregate.UnregisterInput(dev));
      }

      InputAggregate.UpdateInputs();
    }
    public void Dispose()
    {
      if (_IsDisposed)
        return;

      _IsDisposed = true;
      InputRegistry.Dispose();
      if (DInput != null)
        DInput.Dispose();
    }

    public void OpenConfigDialog()
    {
      var settings = new GUI.SettingsDialog();
      settings.Closed += (_1, _2) => SaveMappings();

      Sandbox.Graphics.GUI.MyGuiSandbox.AddScreen(settings);
    }

    public static void SaveMappings()
    {
      // Remove empty binds before saving
      InputRegistry.Cleanup();

      MyPluginLog.Info("Saving configuration...");
      var configPath = Path.Combine(MyFileSystem.UserDataPath, "Storage", ConfigFileName);

      try
      {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(configPath));

        string newConfigText;
        using (var writer = new StringWriter())
        {
          new XmlSerializer(typeof(InputRegistry)).Serialize(writer, InputRegistry);
          newConfigText = writer.ToString();
        }

        File.WriteAllText(configPath, newConfigText);
      }
      catch (Exception ex)
      {
        MyPluginLog.Error($"Failed to save the configuration file: {configPath} - {ex}");
      }
    }

    void ReadDevices()
    {
      if (InputRegistry.DiscoverDevices(DInput))
        SaveMappings();
      InputAggregate.DInput = DInput;

      foreach (var dev in InputRegistry.Devices)
      {
        dev.Acquire();

        if (dev.IsAcquired)
          InputAggregate.RegisterInput(dev);
      }
    }

    void LoadMappings()
    {
      var configPath = Path.Combine(MyFileSystem.UserDataPath, "Storage", ConfigFileName);
      
      try
      {
        if (File.Exists(configPath))
        {
          MyPluginLog.Info("Loading configuration from file...");
          var xmlSerializer = new XmlSerializer(typeof(InputRegistry));
          using (var streamReader = File.OpenText(configPath))
            InputRegistry = (InputRegistry)xmlSerializer.Deserialize(streamReader);

          MyPluginLog.Info($"Read mappings for {InputRegistry.Devices.Count} devices.");
          foreach (var dev in InputRegistry.Devices)
          {
            MyPluginLog.Info($"  {dev.DeviceName} - {dev.Binds.Count} binds.");
          }
          return;
        }
      }
      catch (Exception e)
      {
        MyPluginLog.Warning($"Failed to load configuration file: {configPath} - {e}");
        try
        {
          var timestamp = DateTime.Now.ToString("yyyyMMdd-hhmmss");
          var corruptedPath = $"{configPath}.corrupted.{timestamp}.txt";
          MyPluginLog.Info($"Moving corrupted configuration file: {configPath} => {corruptedPath}");
          File.Move(configPath, corruptedPath);
        } catch { }
      }

      MyPluginLog.Info("Writing default config.");
      SaveMappings();
    }

    void AttemptPatches()
    {
      try
      {
        MyPluginLog.Info("Applying patches...");

        Harmony harmony = new Harmony("AnanaceDev.AnalogGridControl");
        harmony.PatchAll(Assembly.GetExecutingAssembly());

        MyPluginLog.Info("Patches applied.");

        ControllerPatched = true;
      }
      catch (Exception ex)
      {
        MyPluginLog.Warning($"Failed to apply patches, {ex.GetType()}: {ex}");
      }
    }
  }

}
