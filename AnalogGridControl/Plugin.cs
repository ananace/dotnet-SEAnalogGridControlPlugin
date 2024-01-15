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

  class Plugin : IPlugin
  {
    public const string Name = "AnalogGridControl";
    private static readonly string ConfigFileName = $"{Name}.cfg";

    public static bool InputActiveByDefault => InputRegistry.InputActiveByDefault;
    public static ushort InputThrottleMultiplayer => InputRegistry.InputThrottleMultiplayer;
    public static bool ControllerPatched = false;

    public static InputRegistry InputRegistry = new InputRegistry();
    public static DirectInput DInput;

    public void Init(object _gameObject)
    {
      AttemptPatches();

      LoadMappings();

      ReadDevices();
    }
    public void Update() {}
    public void Dispose() {}

    public static void SaveMappings()
    {
      MyPluginLog.Info("Saving mappings...");
      var configPath = Path.Combine(MyFileSystem.UserDataPath, "Storage", ConfigFileName);

      Directory.CreateDirectory(System.IO.Path.GetDirectoryName(configPath));
      using (var text = File.CreateText(configPath))
        new XmlSerializer(typeof(InputRegistry)).Serialize(text, InputRegistry);
    }

    void ReadDevices()
    {
      MyPluginLog.Info("Acquiring DirectInput devices...");

      DInput = new DirectInput();

      var devices = DInput.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly) as IReadOnlyList<DeviceInstance>;

      bool dirty = false;
      foreach (var device in devices)
      {
        InputDevice dev;
        if (Plugin.InputRegistry.HasDevice(device))
        {
          MyPluginLog.Info($"- Existing device '{device.InstanceName}', reading mappings from registry.");
          dev = Plugin.InputRegistry.GetDevice(device);
        }
        else
        {
          MyPluginLog.Info($"- New device '{device.InstanceName}', adding to registry.");
          dev = new InputDevice();
        }

        dev.Acquire(DInput, device);

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
          MyPluginLog.Info("Loading mappings from file...");
          var xmlSerializer = new XmlSerializer(typeof(InputRegistry));
          using (var streamReader = File.OpenText(configPath))
            InputRegistry = (InputRegistry)xmlSerializer.Deserialize(streamReader);

          MyPluginLog.Info($"Read mappings for {InputRegistry.Devices.Count} devices.");
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
