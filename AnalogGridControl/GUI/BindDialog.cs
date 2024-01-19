using System;
using System.Linq;
using System.Text;
using AnanaceDev.AnalogGridControl.GUI.Controls;
using AnanaceDev.AnalogGridControl.GUI.Dialogs;
using AnanaceDev.AnalogGridControl.InputMapping;
using AnanaceDev.AnalogGridControl.Util;
using Sandbox;
using Sandbox.Graphics.GUI;
using VRage.Utils;
using VRageMath;

namespace AnanaceDev.AnalogGridControl.GUI
{

  public class BindDialog : MyGuiScreenBase
  {
    public enum BindType
    {
      Axis = 0,
      Button = 1,
      Hat = 2
    }

    public enum OutputType
    {
      Axis = 0,
      Action = 1,
    }

    public override string GetFriendlyName() => "BindDialog";

    const string _AddCaption = "Create Bind";
    const string _EditCaption = "Edit Bind";
    readonly string Caption = _EditCaption;

    public InputDevice Device { get; private set; }
    public Bind Bind { get; private set; }

    public BindType BindTab { get; set; } = BindType.Axis;
    public OutputType OutputTab { get; set; } = OutputType.Axis;

    MyGuiScreenMessageBox.ResultEnum _Result = MyGuiScreenMessageBox.ResultEnum.CANCEL;
    public event EventHandler<MyGuiScreenMessageBox.ResultEnum> ResultCallback;

    public BindDialog(InputDevice dev, Bind bind = null)
      : base(
          new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, new Vector2(0.65f, 0.85f),
          backgroundTransition: MySandboxGame.Config.UIBkOpacity, guiTransition: MySandboxGame.Config.UIOpacity
        )
    {
      if (bind == null)
        Caption = _AddCaption;

      Device = dev;
      Bind = bind ?? new Bind();
      SelectCurrentBindPages();

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

      var inputLabel = new MyGuiControlLabel(text: "Input");
      var inputTabs = new MyGuiControlTabControl
      {
        OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM,
        Size = new Vector2(m_size.Value.X - this.GetOptimalSpacer() * 2, 0),
        TabButtonScale = 0.75f,
      };

      var axisTab = inputTabs.GetTabSubControl((int)BindType.Axis);
      var buttonTab = inputTabs.GetTabSubControl((int)BindType.Button);
      var hatTab = inputTabs.GetTabSubControl((int)BindType.Hat);

      inputTabs.SelectedPage = (int)BindTab;
      inputTabs.OnPageChanged += () => BindTab = (BindType)inputTabs.SelectedPage;

      var outputLabel = new MyGuiControlLabel(text: "Output");
      var outputTabs = new MyGuiControlTabControl
      {
        OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM,
        Size = new Vector2(m_size.Value.X - this.GetOptimalSpacer() * 2, 0),
        TabButtonScale = 0.75f,
      };

      var outputAxisTab = outputTabs.GetTabSubControl((int)OutputType.Axis);
      var outputActionTab = outputTabs.GetTabSubControl((int)OutputType.Action);

      outputTabs.SelectedPage = (int)OutputTab;
      outputTabs.OnPageChanged += () => OutputTab = (OutputType)outputTabs.SelectedPage;

      var cancelButton = new MyGuiControlButton(text: VRage.MyTexts.Get(MyCommonTexts.Cancel), originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);
      var confirmButton = new MyGuiControlButton(text: VRage.MyTexts.Get(MyCommonTexts.Confirm));

      cancelButton.ButtonClicked += OnCancelClicked;
      confirmButton.ButtonClicked += OnConfirmClicked;

      var detectButton = new MyGuiControlButton(text: new StringBuilder("Detect Input"), originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM);
      detectButton.Enabled = Device.IsValid;
      detectButton.ButtonClicked += OnDetectClicked;

      Elements.Add(inputLabel);
      Controls.Add(inputTabs);
      Elements.Add(outputLabel);
      Controls.Add(outputTabs);
      Controls.Add(confirmButton);
      Controls.Add(cancelButton);
      Controls.Add(detectButton);
#endregion

#region Layout
      var bottomLeft = new Vector2(m_size.Value.X * -0.5f, m_size.Value.Y * 0.5f);
      var bottomRight = new Vector2(m_size.Value.X * 0.5f, m_size.Value.Y * 0.5f);
      var spacer = this.GetOptimalSpacerVector();

      cancelButton.Position = bottomRight - spacer;
      confirmButton.PositionToLeftOf(cancelButton);
      detectButton.Position = new Vector2(bottomLeft.X + spacer.X, bottomLeft.Y - spacer.Y);

      var usableArea = this.GetAreaBetween(caption, confirmButton);
      usableArea.Size = new Vector2(usableArea.Size.X, usableArea.Size.Y - inputLabel.Size.Y - outputLabel.Size.Y - spacer.Y * 2);

      outputTabs.Position = new Vector2(bottomRight.X - spacer.X, confirmButton.GetCoordTopLeftFromAligned().Y - spacer.Y);
      outputTabs.Size = new Vector2(outputTabs.Size.X, usableArea.Size.Y * 0.4f);

      outputLabel.PositionAbove(outputTabs);

      inputTabs.Size = new Vector2(inputTabs.Size.X, usableArea.Size.Y * 0.5f);
      inputTabs.PositionAbove(outputLabel);

      inputLabel.PositionAbove(inputTabs);
#endregion

      BuildAxisTab(axisTab);
      BuildButtonTab(buttonTab);
      BuildHatTab(hatTab);
      BuildOutputAxisTab(outputAxisTab);
      BuildOutputActionTab(outputActionTab);
    }

    void BuildAxisTab(MyGuiControlTabPage page)
    {
      var axes = Device.IsValid ? Device.Axes : InputDevice.MaxAxes;

      page.Text = new StringBuilder("Axis");
      page.Enabled = axes.Any();

#region Controls
      float combinedSize = 0;
      var axisLabel = new MyGuiControlLabel(text: "Axis", originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP);
      var axisChoice = new MyGuiControlCombobox(originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
      foreach (DeviceAxis axis in axes)
        axisChoice.AddItem((long)axis, MyStringId.GetOrCompute(axis.ToString()));

      axisChoice.ItemSelected += () => {
        Bind.InputAxis = (DeviceAxis)axisChoice.GetSelectedKey();
      };
      
      if (!Bind.InputAxis.HasValue)
        Bind.InputAxis = DeviceAxis.X;

      axisChoice.SelectItemByKey((long)Bind.InputAxis.Value, false);
      combinedSize += axisChoice.Size.Y + this.GetOptimalSpacer();

      var deadzoneLabel = new MyGuiControlLabel(text: "Deadzone", originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP);
      var deadzoneChoice = new MyGuiControlSlider(labelText: "{0}", labelDecimalPlaces: 2, labelSpaceWidth: 0.05f, showLabel: true, defaultValue: Bind.Deadzone, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);

      deadzoneChoice.ValueChanged += (_) => {
        Bind.Deadzone = deadzoneChoice.Value;
      };
      combinedSize += deadzoneChoice.Size.Y + this.GetOptimalSpacer();

      var curveTooltip = "The input curve ranges from linear at 0 to cubic at 1";
      var curveLabel = new MyGuiControlLabel(text: "Input Curve", originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP);
      var curveChoice = new MyGuiControlSlider(toolTip: curveTooltip, labelText: "{0}", labelDecimalPlaces: 2, labelSpaceWidth: 0.05f, showLabel: true, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP)
      {
        MinValue = 0,
        MaxValue = 1.0f,
      };

      curveChoice.Value = Bind.Curve;
      curveChoice.ValueChanged += (_) => {
        Bind.Curve = curveChoice.Value;
      };
      combinedSize += curveChoice.Size.Y + this.GetOptimalSpacer();
#endregion

#region Layout
      var spacer = this.GetOptimalSpacerVector();

      var layout = new MyLayoutTable(page, page.Size * -0.5f + spacer, new Vector2(page.Size.X - spacer.X * 2, combinedSize));
      layout.SetColumnWidthsNormalized(0.25f, 0.75f);
      layout.SetRowHeightsNormalized(0.3f, 0.3f, 0.3f);

      int row = 0;
      layout.Add(axisLabel, MyAlignH.Right, MyAlignV.Center, row, 0);
      layout.AddWithSize(axisChoice, MyAlignH.Left, MyAlignV.Center, row++, 1);

      layout.Add(deadzoneLabel, MyAlignH.Right, MyAlignV.Center, row, 0);
      layout.AddWithSize(deadzoneChoice, MyAlignH.Left, MyAlignV.Center, row++, 1);

      layout.Add(curveLabel, MyAlignH.Right, MyAlignV.Center, row, 0);
      layout.AddWithSize(curveChoice, MyAlignH.Left, MyAlignV.Center, row++, 1);
#endregion
    }

    void BuildButtonTab(MyGuiControlTabPage page)
    {
      int buttons = Device.IsValid ? Device.Buttons : InputDevice.MaxButtons;

      page.Text = new StringBuilder("Button");
      page.Enabled = buttons > 0;

#region Controls
      float combinedSize = 0;
      var buttonLabel = new MyGuiControlLabel(text: "Button", originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP);
      var buttonChoice = new MyGuiControlNumberWang(minValue: 1, maxValue: buttons, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);

      if (!Bind.InputButton.HasValue)
        Bind.InputButton = 0;

      buttonChoice.Value = Bind.InputButton.Value + 1;

      buttonChoice.ValueChanged += (_) => {
        Bind.InputButton = (int)buttonChoice.Value - 1;
      };
      combinedSize += buttonChoice.Size.Y + this.GetOptimalSpacer();
#endregion

#region Layout
      var spacer = this.GetOptimalSpacerVector();

      var layout = new MyLayoutTable(page, page.Size * -0.5f + spacer, new Vector2(page.Size.X - spacer.X * 2, combinedSize));
      layout.SetColumnWidthsNormalized(0.25f, 0.75f);
      layout.SetRowHeightsNormalized(1.0f);

      layout.Add(buttonLabel, MyAlignH.Right, MyAlignV.Center, 0, 0);
      layout.AddWithSize(buttonChoice, MyAlignH.Left, MyAlignV.Center, 0, 1);
#endregion
    }

    void BuildHatTab(MyGuiControlTabPage page)
    {
      int hats = Device.IsValid ? Device.POVHats : InputDevice.MaxPOVHats;

      page.Text = new StringBuilder("POV Hat");
      page.Enabled = hats > 0;

#region Controls
      float combinedSize = 0;
      MyGuiControlLabel hatLabel = null;
      MyGuiControlNumberWang hatChoice = null;

      if (hats > 1)
      {
        hatLabel = new MyGuiControlLabel(text: "POV Hat", originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP);
        hatChoice = new MyGuiControlNumberWang(minValue: 1, maxValue: hats, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);

        hatChoice.ValueChanged += (value) => {
          Bind.InputHat = (int)value - 1;
        };
        
        if (!Bind.InputHat.HasValue)
          Bind.InputHat = 0;

        hatChoice.Value = Bind.InputHat.Value + 1;

        combinedSize += hatChoice.Size.Y + this.GetOptimalSpacer();
      }

      var hatAxisLabel = new MyGuiControlLabel(text: "Hat Direction", originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP);
      var hatAxisChoice = new MyGuiControlCombobox(originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
      foreach (DeviceHatAxis axis in Enum.GetValues(typeof(DeviceHatAxis)))
        hatAxisChoice.AddItem((long)axis, MyStringId.GetOrCompute(axis.ToString()));

      hatAxisChoice.ItemSelected += () => {
        Bind.InputHatAxis = (DeviceHatAxis)hatAxisChoice.GetSelectedKey();
      };

      if (!Bind.InputHatAxis.HasValue)
        Bind.InputHatAxis = DeviceHatAxis.Up;

      hatAxisChoice.SelectItemByKey((long)Bind.InputHatAxis.Value, false);

      combinedSize += hatAxisChoice.Size.Y + this.GetOptimalSpacer();
#endregion

#region Layout
      var spacer = this.GetOptimalSpacerVector();
      var layout = new MyLayoutTable(page, page.Size * -0.5f + spacer, new Vector2(page.Size.X - spacer.X * 2, combinedSize));
      layout.SetColumnWidthsNormalized(0.25f, 0.75f);
      if (hatChoice != null)
        layout.SetRowHeightsNormalized(0.5f, 0.5f);
      else
        layout.SetRowHeightsNormalized(1.0f);

      int row = 0;
      if (hatChoice != null)
      {
        layout.Add(hatLabel, MyAlignH.Right, MyAlignV.Center, row, 0);
        layout.AddWithSize(hatChoice, MyAlignH.Left, MyAlignV.Center, row++, 1);
      }

      layout.Add(hatAxisLabel, MyAlignH.Right, MyAlignV.Center, row, 0);
      layout.AddWithSize(hatAxisChoice, MyAlignH.Left, MyAlignV.Center, row++, 1);
#endregion
    }

    void BuildOutputAxisTab(MyGuiControlTabPage page)
    {
      page.Text = new StringBuilder("Axis");

#region Controls
      float combinedSize = 0;
      var axisChoiceLabel = new MyGuiControlLabel(text: "Axis", originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP);
      var axisChoice = new MyGuiControlCombobox(originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
      foreach (GameAxis axis in Enum.GetValues(typeof(GameAxis)))
      {
        var name = axis.GetHumanReadableName();
        var tooltip = axis.GetDescription();
        axisChoice.AddItem((long)axis, MyStringId.GetOrCompute(name), toolTip: MyStringId.GetOrCompute(tooltip), sort: false);
      }

      axisChoice.ItemSelected += () => {
        Bind.MappingAxis = (GameAxis)axisChoice.GetSelectedKey();
      };

      if (Bind.MappingAxis.HasValue)
        axisChoice.SelectItemByKey((long)Bind.MappingAxis.Value, false);
      else
        axisChoice.SelectItemByIndex(0);
      combinedSize += axisChoice.Size.Y + this.GetOptimalSpacer();

      var invertLabel = new MyGuiControlLabel(text: "Invert", originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP);
      var invertCheckbox = new MyGuiControlCheckbox(isChecked: Bind.MappingAxisInvert, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
      invertCheckbox.IsCheckedChanged += (_) => Bind.MappingAxisInvert = invertCheckbox.IsChecked;
      combinedSize += invertCheckbox.Size.Y + this.GetOptimalSpacer();
#endregion

#region Layout
      var spacer = this.GetOptimalSpacerVector();
      var layout = new MyLayoutTable(page, page.Size * -0.5f + spacer, new Vector2(page.Size.X - spacer.X * 2, combinedSize));
      layout.SetColumnWidthsNormalized(0.25f, 0.75f);
      layout.SetRowHeightsNormalized(0.5f, 0.5f);

      int row = 0;
      layout.Add(axisChoiceLabel, MyAlignH.Right, MyAlignV.Center, row, 0);
      layout.AddWithSize(axisChoice, MyAlignH.Left, MyAlignV.Center, row++, 1);

      layout.Add(invertLabel, MyAlignH.Right, MyAlignV.Center, row, 0);
      layout.AddWithSize(invertCheckbox, MyAlignH.Left, MyAlignV.Center, row++, 1);
#endregion
    }

    void BuildOutputActionTab(MyGuiControlTabPage page)
    {
      page.Text = new StringBuilder("Action");

#region Controls
      float combinedSize = 0;
      var actionChoiceLabel = new MyGuiControlLabel(text: "Action", originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP);
      var actionChoice = new MyGuiControlCombobox(originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
      foreach (GameAction action in Enum.GetValues(typeof(GameAction)))
      {
        var name = action.GetHumanReadableName();
        var tooltip = action.GetDescription();
        actionChoice.AddItem((long)action, MyStringId.GetOrCompute(name), toolTip: MyStringId.GetOrCompute(tooltip), sort: false);
      }

      actionChoice.ItemSelected += () => {
        var bind = (GameAction)actionChoice.GetSelectedKey();
        Bind.MappingAction = bind;
      };

      if (Bind.MappingAction.HasValue)
        actionChoice.SelectItemByKey((long)Bind.MappingAction.Value, false);
      else
        actionChoice.SelectItemByIndex(0);
      combinedSize += actionChoice.Size.Y + this.GetOptimalSpacer();
#endregion

#region Layout
      var spacer = this.GetOptimalSpacerVector();
      var layout = new MyLayoutTable(page, page.Size * -0.5f + spacer, new Vector2(page.Size.X - spacer.X * 2, combinedSize));
      layout.SetColumnWidthsNormalized(0.25f, 0.75f);
      layout.SetRowHeightsNormalized(1.0f);

      int row = 0;
      layout.Add(actionChoiceLabel, MyAlignH.Right, MyAlignV.Center, row, 0);
      layout.AddWithSize(actionChoice, MyAlignH.Left, MyAlignV.Center, row++, 1);
#endregion
    }

    void OnCancelClicked(MyGuiControlButton _)
    {
      _Result = MyGuiScreenMessageBox.ResultEnum.CANCEL;
      FinalizeBind();
    }

    void OnConfirmClicked(MyGuiControlButton _)
    {
      _Result = MyGuiScreenMessageBox.ResultEnum.YES;
      FinalizeBind();
    }

    void OnDetectClicked(MyGuiControlButton _)
    {
      if (!Device.IsAcquired)
        Device.Acquire();

      var detect = new MyGuiScreenInputDetection(Device, "Press any button or move any axis on the device...", timeoutInMilliseconds: 10000);
      detect.BindDetected += OnBindDetected;
      MyGuiSandbox.AddScreen(detect);
    }

    void OnBindDetected(Bind bind)
    {
      Bind.ApplyValuesFrom(bind);
      SelectCurrentBindPages();
      RecreateControls(false);
    }

    void SelectCurrentBindPages()
    {
      if (Bind.InputAxis.HasValue)
        BindTab = BindType.Axis;
      else if (Bind.InputButton.HasValue)
        BindTab = BindType.Button;
      else if (Bind.InputHatAxis.HasValue)
        BindTab = BindType.Hat;
      else if (Device.Axes.Any())
        BindTab = BindType.Axis;
      else if (Device.POVHats > 0)
        BindTab = BindType.Hat;
      else
        BindTab = BindType.Button;

      if (Bind.MappingAxis.HasValue)
        OutputTab = OutputType.Axis;
      else if (Bind.MappingAction.HasValue)
        OutputTab = OutputType.Action;
      else if (BindTab == BindType.Axis)
        OutputTab = OutputType.Axis;
      else
        OutputTab = OutputType.Action;
    }

    void FinalizeBind()
    {
      if (_Result == MyGuiScreenMessageBox.ResultEnum.YES)
      {
        // Clear unwanted values to avoid generating a different bind than expected
        if (BindTab != BindType.Axis)
          Bind.InputAxis = null;
        if (BindTab != BindType.Hat)
          Bind.InputHatAxis = null;
        if (BindTab != BindType.Button)
          Bind.InputButton = null;

        if (OutputTab != OutputType.Axis)
          Bind.MappingAxis = null;
        if (OutputTab != OutputType.Action)
          Bind.MappingAction = null;
      }

      ResultCallback?.Invoke(this, _Result);
      CloseScreen();
    }
  }

}
