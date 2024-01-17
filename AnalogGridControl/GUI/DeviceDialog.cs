using System.Text;
using Sandbox;
using Sandbox.Graphics.GUI;
using VRageMath;

namespace AnanaceDev.AnalogGridControl.GUI
{

  public class DeviceDialog : MyGuiScreenBase
  {
    public override string GetFriendlyName() => "DeviceDialog";

    const string Caption = "Configure Device Binds";

    public InputDevice Device { get; private set; }

    public DeviceDialog(InputDevice dev)
      : base(
          new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, new Vector2(0.8f, 0.6f),
          backgroundTransition: MySandboxGame.Config.UIBkOpacity, guiTransition: MySandboxGame.Config.UIOpacity
        )
    {
      Device = dev;

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

      var bindLabel = new MyGuiControlLabel(text: "Binds");
      var bindList = new MyGuiControlTable();
      bindList.ColumnsCount = 3;
      bindList.SetCustomColumnWidths(new[]
      {
        0.9f,
        0.05f,
        0.05f,
      });
      bindList.SetColumnName(0, new StringBuilder("Bind"));
      bindList.SetColumnName(1, new StringBuilder("Edit"));
      bindList.SetColumnName(2, new StringBuilder("Delete"));

      var addBindButton = new MyGuiControlButton(text: new StringBuilder("Add Bind"));
#endregion

#region Layout
      var layout = new MyLayoutVertical(this, 0.0f);
      
      layout.Add(caption, MyAlignH.Center);
      layout.Advance(10.0f);
      layout.Add(bindList, MyAlignH.Left);
      layout.Add(addBindButton, MyAlignH.Right);
#endregion

      PopulateBindsList(bindList);
    }

    void PopulateBindsList(MyGuiControlTable bindsList)
    {
      bindsList.Clear();
      bindsList.Controls.Clear();

      foreach (var bind in Device.Binds)
      {
        string tip = null;

        var row = new MyGuiControlTable.Row(bind);
        row.AddCell(new MyGuiControlTable.Cell(bind.ToString(), toolTip: tip));

        var editCell = new MyGuiControlTable.Cell();
        var editButton = new MyGuiControlButton();
        var editIcon = new MyGuiControlImage(size: editButton.Size, textures: new[] { @"Textures\GUI\Controls\button_filter_system_highlight.dds" });
        editIcon.HasHighlight = editButton.HasHighlight;
        editButton.Elements.Add(editIcon);
        editCell.Control = editButton;
        bindsList.Controls.Add(editButton);
        row.AddCell(editCell);

        var deleteCell = new MyGuiControlTable.Cell();
        var deleteButton = new MyGuiControlButton();
        var deleteIcon = new MyGuiControlImage(size: deleteButton.Size, textures: new[] { @"Textures\GUI\Controls\button_close_symbol_bcg_highlight.dds" });
        deleteIcon.HasHighlight = deleteButton.HasHighlight;
        deleteButton.Elements.Add(deleteIcon);
        deleteCell.Control = deleteButton;
        bindsList.Controls.Add(deleteButton);
        row.AddCell(deleteCell);

        bindsList.Add(row);
      }
    }
  }

}
