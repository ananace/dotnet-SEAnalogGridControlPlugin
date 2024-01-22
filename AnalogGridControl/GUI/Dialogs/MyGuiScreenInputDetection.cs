using AnanaceDev.AnalogGridControl.InputMapping;
using AnanaceDev.AnalogGridControl.Util;
using Sandbox.Graphics.GUI;
using Sandbox.Graphics;
using SharpDX.DirectInput;
using System;
using VRage.Utils;
using VRage;
using VRageMath;

namespace AnanaceDev.AnalogGridControl.GUI.Dialogs
{

  public class MyGuiScreenInputDetection : MyGuiScreenBase
  {
    public override string GetFriendlyName() => "MyGuiScreenInputDetection";

    public Bind Bind { get; private set; } = null;

    public event Action<Bind> BindDetected;

    public string DialogCaption { get; set; }
    public string DialogMessage { get; set; }
    public InputDevice Device { get; private set; }
    JoystickState OriginState { get; set; }

    public int TimeoutInMS { get; set; }
    public int TimeoutStarted { get; private set; }
    public MyStringId CancelButtonText { get; set; }

    MyGuiControlLabel TimerLabel;

    public MyGuiScreenInputDetection(InputDevice device, string dialogText, string dialogCaption = null, MyStringId? cancelButtonText = null, Action<Bind> callback = null, int timeoutInMilliseconds = 0, Vector2? size = null, bool useOpacity = true, Vector2? position = null, float backgroundTransition = 0f, float guiTransition = 0f, bool focusable = true, bool canBeHidden = true)
      : base(position ?? new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, size ?? new Vector2(87f / 175f, 147f / 524f), isTopMostScreen: true, null, backgroundTransition, guiTransition)
    {
      Device = device;
      if (Device.IsAcquired)
        Device.Update(false);

      CanHaveFocus = focusable;
      CanHideOthers = false;
      EnabledBackgroundFade = false;
      BindDetected = callback;
      CanBeHidden = canBeHidden;

      DialogCaption = dialogCaption ?? "Detecting Inputs";
      DialogMessage = dialogText;
      CancelButtonText = cancelButtonText ?? MyCommonTexts.Cancel;

      TimeoutStarted = MyGuiManager.TotalTimeInMilliseconds;
      TimeoutInMS = timeoutInMilliseconds;

      RecreateControls(true);
    }

    public override void RecreateControls(bool constructor)
    {
      base.RecreateControls(constructor);

#region Controls
      var caption = new MyGuiControlLabel(text: DialogCaption, originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP);

      var text = new MyGuiControlLabel(text: DialogMessage, originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);

      if (TimeoutInMS > 0)
        TimerLabel = new MyGuiControlLabel(text: "<Timer>", originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP);

      var cancelButton = new MyGuiControlButton(text: MyTexts.Get(CancelButtonText), originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM, onButtonClick: OnCancelClick);

      Elements.Add(caption);
      Elements.Add(text);
      if (TimerLabel != null)
        Elements.Add(TimerLabel);
      Controls.Add(cancelButton);
#endregion

#region Layout
      var middleTop = new Vector2(0, Size.Value.Y * -0.5f);
      var bottomRight = new Vector2(Size.Value.X * 0.5f, Size.Value.Y * 0.5f);
      var spacer = this.GetOptimalSpacerVector();

      caption.Position = new Vector2(middleTop.X, middleTop.Y + spacer.Y);
      cancelButton.Position = bottomRight - spacer;

      var between = this.GetAreaBetween(caption, cancelButton);
      text.Position = new Vector2(0, between.Position.Y + spacer.Y * 2);
      text.Size = new Vector2(between.Size.X - spacer.X * 2, text.Size.Y);

      if (TimerLabel != null)
        TimerLabel.PositionBelow(text, spacing: spacer.Y * 2);
#endregion
    }

    public override bool Update(bool hasFocus)
    {
      if (!Device.IsValid || !Device.IsAcquired)
      {
        OnCancelClick(null);
        return false;
      }

      var bind = Device.DetectBind();
      if (bind != null)
        OnBindFound(bind);

      if (!base.Update(hasFocus))
        return false;

      if (TimeoutInMS > 0)
      {
        int num = MyGuiManager.TotalTimeInMilliseconds - TimeoutStarted;
        if (TimerLabel != null)
          TimerLabel.Text = $"{(int)Math.Ceiling(((float)TimeoutInMS - (float)num) / 1000.0f)}s";

        if (num >= TimeoutInMS)
          OnCancelClick(null);
      }

      return true;
    }

    void OnBindFound(Bind bind)
    {
      Bind = bind;
      BindDetected?.Invoke(bind);

      CloseScreen();
    }

    void OnCancelClick(MyGuiControlButton _)
    {
      CloseScreen();
    }
  }

}
