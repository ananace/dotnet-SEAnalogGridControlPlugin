using AnanaceDev.AnalogGridControl.Util;
using Sandbox.Graphics.GUI;
using Sandbox;
using System.Text;
using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace AnanaceDev.AnalogGridControl.GUI
{

  public class DeviceDialog : MyGuiScreenBase
  {
    public override string GetFriendlyName() => "DeviceDialog";

    const string Caption = "Configure Device Binds";

    public InputDevice Device { get; private set; }

    MyGuiControlTable bindsList;

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
      var caption = AddCaption(Device.DisplayName);

      bindsList = new MyGuiControlTable()
      {
        ColumnsCount = 3,
        OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP,
        Size = new Vector2(m_size.Value.X - this.GetOptimalSpacer() * 2, 0),
      };
      bindsList.SetCustomColumnWidths(new[]
      {
        0.92f,
        0.04f,
        0.04f,
      });
      bindsList.SetColumnName(0, new StringBuilder("Bind"));
      //bindsList.SetColumnName(1, new StringBuilder("Edit"));
      //bindsList.SetColumnName(2, new StringBuilder("Delete"));

      var addBindButton = new MyGuiControlButton(
        originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM,
        visualStyle: MyGuiControlButtonStyleEnum.Increase, toolTip: "Add Bind"
      );
      addBindButton.ButtonClicked += AddBindPressed;

      var nameDialog = new MyGuiControlTextbox(
        defaultText: Device.DisplayName
      );
      nameDialog.SetToolTip("Device Display Name");

      var renameButton = new MyGuiControlButton(
        originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP,
        visualStyle: MyGuiControlButtonStyleEnum.SquareSmall, toolTip: "Rename Device"
      );
      renameButton.AddImageToButton(@"Textures\GUI\Icons\buttons\Script.dds");
      renameButton.ButtonClicked += b => RenameDevicePressed(nameDialog, b);

      Controls.Add(addBindButton);
      Controls.Add(nameDialog);
      Controls.Add(renameButton);
      Controls.Add(bindsList);
#endregion

#region Layout
      var bottomRight = new Vector2(m_size.Value.X * 0.5f, m_size.Value.Y * 0.5f);
      var spacer = this.GetOptimalSpacerVector();

      addBindButton.Position = bottomRight - spacer;
      renameButton.PositionToLeftOf(addBindButton, spacing: spacer.X);
      nameDialog.SetMaxSize(new Vector2(nameDialog.Size.X, renameButton.Size.Y));
      nameDialog.PositionToLeftOf(renameButton, MyAlignV.Top, spacing: 0);

      bindsList.Position = new Vector2(bottomRight.X - spacer.X, caption.GetCoordTopLeftFromAligned().Y + caption.Size.Y + spacer.Y);
      bindsList.SetTableHeight(addBindButton.GetCoordTopLeftFromAligned().Y - caption.GetCoordTopLeftFromAligned().Y - spacer.Y * 2);
#endregion

      PopulateBindsList();
    }

    void PopulateBindsList()
    {
      bindsList.Clear();
      bindsList.Controls.Clear();

      var alterColumnSize = new Vector2(bindsList.Size.X * 0.15f, bindsList.RowHeight) * 0.5f;

      foreach (var bind in Device.Binds)
      {
        string tip = null;

        var row = new MyGuiControlTable.Row(bind);
        row.AddCell(new MyGuiControlTable.Cell(bind.ToString(), toolTip: tip));

        var editCell = new MyGuiControlTable.Cell(toolTip: "Edit");
        var editButton = new MyGuiControlButton(visualStyle: MyGuiControlButtonStyleEnum.SquareSmall, size: alterColumnSize)
        {
          UserData = bind,
        };
        editButton.AddImageToButton(@"Textures\GUI\Controls\button_filter_system_highlight.dds");
        editButton.ButtonClicked += EditBindPressed;
        editCell.Control = editButton;

        bindsList.Controls.Add(editButton);
        row.AddCell(editCell);

        var deleteCell = new MyGuiControlTable.Cell(toolTip: "Delete");
        var deleteButton = new MyGuiControlButton(visualStyle: MyGuiControlButtonStyleEnum.SquareSmall, size: alterColumnSize)
        {
          UserData = bind,
        };
        deleteButton.AddImageToButton(@"Textures\GUI\Controls\button_close_symbol_highlight.dds");
        deleteButton.ButtonClicked += DeleteBindPressed;
        deleteCell.Control = deleteButton;

        bindsList.Controls.Add(deleteButton);
        row.AddCell(deleteCell);

        bindsList.Add(row);
      }
    }

    void RenameDevicePressed(MyGuiControlTextbox name, MyGuiControlButton button)
    {
      Device.DisplayName = name.Text;

      RecreateControls(false);
    }

    void AddBindPressed(MyGuiControlButton button)
    {
      MyGuiSandbox.AddScreen(CreateBindDialog(Device));
    }

    void EditBindPressed(MyGuiControlButton button)
    {
      if (!(button.UserData is InputMapping.Bind bind))
        return;

      MyGuiSandbox.AddScreen(CreateBindDialog(Device, bind));
    }

    BindDialog CreateBindDialog(InputDevice dev, InputMapping.Bind bind = null)
    {
      var dialog = new BindDialog(dev, bind == null ? null : bind.Clone());
      dialog.ResultCallback += (_, result) => {
        if (result == MyGuiScreenMessageBox.ResultEnum.YES)
        {
          if (bind == null)
            Device.Binds.Add(dialog.Bind);
          else
            bind.ApplyValuesFrom(dialog.Bind);

          PopulateBindsList();
        }
      };
      return dialog;
    }

    void DeleteBindPressed(MyGuiControlButton button)
    {
      if (!(button.UserData is InputMapping.Bind bind))
        return;

      var dialog = MyGuiSandbox.CreateMessageBox(MyMessageBoxStyleEnum.Info, MyMessageBoxButtonsType.YES_NO, new StringBuilder("Delete bind?"), VRage.MyTexts.Get(MyCommonTexts.MessageBoxCaptionPleaseConfirm));

      dialog.ResultCallback += (result) => {
        if (result == MyGuiScreenMessageBox.ResultEnum.YES)
        {
          Device.Binds.Remove(bind);
          var row = bindsList.Find((match) => match.UserData == bind);
          bindsList.Controls.Remove(row.GetCell(1).Control);
          bindsList.Controls.Remove(row.GetCell(2).Control);
          bindsList.Remove(row);
        }
      };

      MyGuiSandbox.AddScreen(dialog);
    }
  }

}
