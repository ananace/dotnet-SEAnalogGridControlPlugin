using System.Linq;
using System.Text;
using Sandbox;
using Sandbox.Graphics.GUI;
using VRageMath;

namespace AnanaceDev.AnalogGridControl.GUI
{

  public class SettingsDialog : MyGuiScreenBase
  {
    public override string GetFriendlyName() => "SettingsDialog";

    const string Caption = "Configure Analog Grid Control";
  
    public SettingsDialog()
      : base(
          new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, new Vector2(0.8f, 0.6f),
          backgroundTransition: MySandboxGame.Config.UIBkOpacity, guiTransition: MySandboxGame.Config.UIOpacity
        )
    {
      EnabledBackgroundFade = true;
      m_closeOnEsc = true;
      m_drawEvenWithoutFocus = true;
      CanHideOthers = true;
      CanBeHidden = true;
      CloseButtonEnabled = true;
    }

    public override void LoadContent()
    {
      base.LoadContent();

      RecreateControls(true);
    }

    public override void RecreateControls(bool constructor)
    {
      base.RecreateControls(constructor);

#region Controls
      var caption = AddCaption(Caption, captionScale: 1);

      var deviceLabel = new MyGuiControlLabel(text: "Plugins");
      var deviceList = new MyGuiControlTable();
      deviceList.ColumnsCount = 3;
      deviceList.SetCustomColumnWidths(new[]
      {
          0.1f,
          0.8f,
          0.05f,
          0.05f,
      });
      deviceList.SetColumnName(0, new StringBuilder("Status"));
      deviceList.SetColumnName(1, new StringBuilder("Name"));
      deviceList.SetColumnName(2, new StringBuilder("Binds"));
      deviceList.SetColumnName(3, new StringBuilder("Edit"));
      // deviceList.SetColumnComparison(1, (a, b) => );

      var analogEnabledTip = "Should analog input be enabled when entering a cockpit/seat,\nor should it require a press of the Toggle Analog Input Active bind first.";
      var analogEnabledLabel = new MyGuiControlLabel(text: "Enabled By Default");
      var analogEnabledCheckbox = new MyGuiControlCheckbox(toolTip: analogEnabledTip)
      {
        IsChecked = Plugin.InputRegistry.InputActiveByDefault,
      };
      analogEnabledCheckbox.IsCheckedChanged += (_) => Plugin.InputRegistry.InputActiveByDefault = analogEnabledCheckbox.IsChecked;
#endregion

#region Layout
      var layout = new MyLayoutVertical(this, 0.0f);
      layout.Add(caption, MyAlignH.Center);
      layout.Advance(10.0f);
      layout.Add(deviceLabel, MyAlignH.Left);
      layout.Add(deviceList, MyAlignH.Left);
      layout.Add(analogEnabledLabel, MyAlignH.Left);
      layout.Add(analogEnabledCheckbox, MyAlignH.Left);
#endregion
      
      PopulateDeviceList(deviceList);
    }

    void PopulateDeviceList(MyGuiControlTable table)
    {
      table.Clear();
      table.Controls.Clear();

      foreach (var dev in Plugin.InputRegistry.Devices)
      {
        var tip = $"{dev.DeviceName} - {dev.Buttons} Buttons, {dev.POVHats} Hats, {dev.Axes.Count()} Axes";

        var row = new MyGuiControlTable.Row(dev, tip);
        row.AddCell(new MyGuiControlTable.Cell(dev.IsValid ? "OK" : "N/A", textColor: dev.IsValid ? Color.Green : Color.OrangeRed));
        row.AddCell(new MyGuiControlTable.Cell(dev.DeviceName, toolTip: tip));
        row.AddCell(new MyGuiControlTable.Cell(dev.Binds.Count.ToString()));

        var editCell = new MyGuiControlTable.Cell();
        var editButton = new MyGuiControlButton();
        var editIcon = new MyGuiControlImage(size: editButton.Size, textures: new[] { @"Textures\GUI\Controls\button_filter_system_highlight.dds" });
        editIcon.HasHighlight = editButton.HasHighlight;
        editButton.Elements.Add(editIcon);

        editButton.ButtonClicked += (_) => OpenDeviceDialog(dev);

        editCell.Control = editButton;
        table.Controls.Add(editButton);
        row.AddCell(editCell);

        table.Add(row);
      }
    }

    void OpenDeviceDialog(InputDevice dev)
    {
      
    }
  }

}
