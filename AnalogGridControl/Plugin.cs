using System;
using System.Collections.Generic;
using System.IO;
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
    private static readonly string ConfigFileName = $"{Name}.cfg";

    public static bool InputActiveByDefault => InputRegistry.InputActiveByDefault;
    public static ushort InputThrottleMultiplayer => InputRegistry.InputThrottleMultiplayer;
    public static bool ControllerPatched = false;

    public static InputRegistry InputRegistry = new InputRegistry();
    public static DirectInput DInput;

    bool _IsDisposed = false;

    public void Init(object _gameObject)
    {
      AttemptPatches();

      LoadMappings();

      ReadDevices();
    }
    public void Update() {}
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

      Directory.CreateDirectory(System.IO.Path.GetDirectoryName(configPath));
      using (var text = File.CreateText(configPath))
        new XmlSerializer(typeof(InputRegistry)).Serialize(text, InputRegistry);
    }

    void ReadDevices()
    {
      MyPluginLog.Info("Checking for attached DirectInput devices...");

      DInput = new DirectInput();
      var devices = DInput.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly) as IReadOnlyList<DeviceInstance>;

      bool dirty = false;
      foreach (var device in devices)
      {
        InputDevice dev;
        if (Plugin.InputRegistry.HasDevice(device))
        {
          MyPluginLog.Info($"- Existing device '{device.InstanceName}' found, reading mappings from registry.");
          dev = Plugin.InputRegistry.GetDevice(device);
        }
        else
        {
          MyPluginLog.Info($"- New device '{device.InstanceName}' found, adding to registry.");
          dev = new InputDevice();
        }

        dev.Init(DInput, device);

        if (!Plugin.InputRegistry.HasDevice(device))
        {
          Plugin.InputRegistry.Devices.Add(dev);
          dirty = true;
        }
      }

      if (dirty)
        SaveMappings();
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
        harmony.PatchAll();

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
