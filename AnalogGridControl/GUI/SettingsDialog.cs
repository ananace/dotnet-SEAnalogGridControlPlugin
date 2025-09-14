using AnanaceDev.AnalogGridControl.Util;
using Sandbox.Graphics.GUI;
using Sandbox;
using System.Linq;
using System.Text;
using VRage.Utils;
using VRageMath;

namespace AnanaceDev.AnalogGridControl.GUI
{

  public class SettingsDialog : MyGuiScreenBase
  {
    public override string GetFriendlyName() => "SettingsDialog";

    const string Caption = "Configure Analog Grid Control";

    MyGuiControlTable deviceList;
  
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
      var caption = AddCaption(Caption);

      deviceList = new MyGuiControlTable()
      {
        ColumnsCount = 4,
        OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
        Size = new Vector2(m_size.Value.X - this.GetOptimalSpacer() * 2, 0),
      };
      deviceList.SetCustomColumnWidths(new[]
      {
          0.1f,
          0.76f,
          0.1f,
          0.04f,
      });
      deviceList.SetColumnName(0, new StringBuilder("Status"));
      deviceList.SetColumnName(1, new StringBuilder("Name"));
      deviceList.SetColumnName(2, new StringBuilder("Binds"));
      //deviceList.SetColumnName(3, new StringBuilder("Edit"));
      
      var rescanButton = new MyGuiControlButton(visualStyle: VRage.Game.MyGuiControlButtonStyleEnum.SquareSmall, toolTip: "Rescan devices");
      rescanButton.ButtonClicked += (_) => {
        if (Plugin.InputRegistry.DiscoverDevices(Plugin.DInput, true))
          PopulateDeviceList(deviceList);
      };

      var analogEnabledTip = "Should analog input be enabled when entering a cockpit/seat,\nor should it require a press of the Toggle Analog Input Active bind first.";
      var analogEnabledLabel = new MyGuiControlLabel(text: "Enabled By Default");
      var analogEnabledCheckbox = new MyGuiControlCheckbox(toolTip: analogEnabledTip, isChecked: Plugin.InputActiveByDefault, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM);
      analogEnabledCheckbox.IsCheckedChanged += (_) => Plugin.InputRegistry.InputActiveByDefault = analogEnabledCheckbox.IsChecked;

      Controls.Add(deviceList);
      Controls.Add(rescanButton);
      Elements.Add(analogEnabledLabel);
      Controls.Add(analogEnabledCheckbox);
#endregion

#region Layout
      var bottomLeft = new Vector2(m_size.Value.X * -0.5f, m_size.Value.Y * 0.5f);
      var spacer = this.GetOptimalSpacerVector();

      analogEnabledCheckbox.Position = new Vector2(bottomLeft.X + spacer.X, bottomLeft.Y - spacer.Y);
      analogEnabledLabel.PositionToRightOf(analogEnabledCheckbox);

      deviceList.Position = new Vector2(bottomLeft.X + spacer.X, caption.GetCoordTopLeftFromAligned().Y + caption.Size.Y + spacer.Y);
      deviceList.SetTableHeight(analogEnabledCheckbox.GetCoordTopLeftFromAligned().Y - caption.GetCoordTopLeftFromAligned().Y - spacer.Y * 2);

      rescanButton.PositionBelow(deviceList, MyAlignH.Right);
#endregion
      
      PopulateDeviceList(deviceList);
    }

    void PopulateDeviceList(MyGuiControlTable table)
    {
      table.Clear();
      table.Controls.Clear();

      var editColumnSize = new Vector2(table.Size.X * 0.15f, table.RowHeight) * 0.5f;

      foreach (var dev in Plugin.InputRegistry.Devices)
      {
        string tip = null;
        if (dev.IsValid)
          tip = $"{dev.Buttons} Buttons, {dev.POVHats} Hats, {dev.Axes.Count()} Axes";

        var row = new MyGuiControlTable.Row(dev);
        row.AddCell(new MyGuiControlTable.Cell(dev.IsValid ? "OK" : "N/A", textColor: dev.IsValid ? Color.Green : Color.OrangeRed));
        row.AddCell(new MyGuiControlTable.Cell(dev.DisplayName, toolTip: tip));
        row.AddCell(new MyGuiControlTable.Cell(dev.Binds.Count.ToString()));

        var editCell = new MyGuiControlTable.Cell(toolTip: "Edit");
        var editButton = new MyGuiControlButton()
        {
          VisualStyle = VRage.Game.MyGuiControlButtonStyleEnum.SquareSmall,
          Size = editColumnSize,
          UserData = dev,
        };
        editButton.AddImageToButton(@"Textures\GUI\Controls\button_filter_system_highlight.dds");
        editButton.ButtonClicked += EditButtonPressed;
        editCell.Control = editButton;

        table.Controls.Add(editButton);
        row.AddCell(editCell);

        table.Add(row);
      }
    }

    void EditButtonPressed(MyGuiControlButton button)
    {
      if (!(button.UserData is InputDevice dev))
        return;

      var dialog = new DeviceDialog(dev);
      dialog.Closed += (_1, _2) => PopulateDeviceList(deviceList);
      MyGuiSandbox.AddScreen(dialog);
    }
  }

}
