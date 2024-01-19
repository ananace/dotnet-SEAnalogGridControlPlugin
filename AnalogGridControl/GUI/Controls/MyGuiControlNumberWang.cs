using System;
using AnanaceDev.AnalogGridControl.Util;
using Sandbox.Graphics.GUI;
using VRage.Utils;
using VRageMath;

namespace AnanaceDev.AnalogGridControl.GUI.Controls
{

  public class MyGuiControlNumberWang : MyGuiControlTextbox,IMyGuiControlsParent
  {
    MyGuiControls m_controls;
    public MyGuiControls Controls => m_controls;

    public decimal MinValue { get; private set; }
    public decimal MaxValue { get; private set; }
    public decimal Value {
      get
      {
        decimal value;
        if (decimal.TryParse(Text, out value))
          return value;
        return MinValue;
      }
      set
      {
        var newValue = Math.Min(MaxValue, Math.Max(MinValue, value));
        decimal oldValue;
        if (decimal.TryParse(Text, out oldValue) && oldValue == newValue)
          return;

        Text = newValue.ToString();
        ValueChanged?.Invoke(newValue);
      }
    }

    MyGuiControlButton decreaseButton = null;
    MyGuiControlButton increaseButton = null;

    public event Action<decimal> ValueChanged;

    public MyGuiControlNumberWang(Vector2? position = null, decimal minValue = decimal.MinValue, decimal maxValue = decimal.MaxValue, decimal? defaultValue = null, Vector4? textColor = null, float textScale = 0.8f, string toolTip = null, MyGuiControlTextboxStyleEnum visualStyle = MyGuiControlTextboxStyleEnum.Default, MyGuiDrawAlignEnum originAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER)
            : base(position, null, 16, textColor, textScale, MyGuiControlTextboxType.DigitsOnly, visualStyle, minNumericValue: minValue, maxNumericValue: maxValue)
    {
      m_controls = new MyGuiControls(this);

      base.Name = "NumberWang";
      base.CanFocusChildren = true;

      if (defaultValue.HasValue)
        Text = defaultValue.ToString();
      else
        Text = minValue.ToString();
      MinValue = minValue;
      MaxValue = maxValue;

      increaseButton = new MyGuiControlButton(visualStyle: VRage.Game.MyGuiControlButtonStyleEnum.Increase, originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER);
      decreaseButton = new MyGuiControlButton(visualStyle: VRage.Game.MyGuiControlButtonStyleEnum.Decrease, originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER);

      increaseButton.ButtonClicked += (_) => Value += 1;
      decreaseButton.ButtonClicked += (_) => Value -= 1;

      Controls.Add(increaseButton);
      Controls.Add(decreaseButton);

      RefreshInternal();
    }

    public override void Draw(float transitionAlpha, float backgroundTransitionAlpha)
    {
      base.Draw(transitionAlpha, backgroundTransitionAlpha);
      foreach (MyGuiControlBase visibleControl in Controls.GetVisibleControls())
      {
        if (visibleControl.IsWithinDrawScissor && visibleControl.GetExclusiveInputHandler() != visibleControl && !(visibleControl is MyGuiControlGridDragAndDrop))
          visibleControl.Draw(transitionAlpha * visibleControl.Alpha, backgroundTransitionAlpha * visibleControl.Alpha);
      }
    }

    public override MyGuiControlBase HandleInput()
    {
      MyGuiControlBase myGuiControlBase = base.HandleInput();
      foreach (MyGuiControlBase visibleControl in Controls.GetVisibleControls())
      {
        if (visibleControl.IsWithinDrawScissor)
        {
          var otherControl = visibleControl.HandleInput();
          if (otherControl != null)
          {
            myGuiControlBase = otherControl;
            break;
          }
        }
      }
      return myGuiControlBase;
    }

    public override MyGuiControlBase GetExclusiveInputHandler()
    {
      MyGuiControlBase exclusiveInputHandler = MyGuiControlBase.GetExclusiveInputHandler(Controls);
      if (exclusiveInputHandler == null)
        exclusiveInputHandler = base.GetExclusiveInputHandler();
      return exclusiveInputHandler;
    }

    public override bool IsMouseOverAnyControl()
    {
      foreach (var control in Controls.GetVisibleControls())
      {
        if (!control.IsHitTestVisible && control.IsWithinDrawScissor && control.IsMouseOver)
          return true;
      }
      return base.IsMouseOverAnyControl();
    }

    public override MyGuiControlBase GetMouseOverControl()
    {
      foreach (var control in Controls.GetVisibleControls())
      {
        if (control.IsHitTestVisible && control.IsMouseOver)
          return control;
      }
      return base.GetMouseOverControl();
    }

    public override void Update()
    {
      foreach (var visibleControl in Controls.GetVisibleControls())
      {
        if (visibleControl.IsWithinDrawScissor || visibleControl.HasFocus || visibleControl.HasHighlight || visibleControl.IsActiveControl)
          visibleControl.Update();
      }
      base.Update();
    }

    public override void OnRemoving()
    {
      Controls.Clear();
      base.OnRemoving();
    }

    protected override void OnOriginAlignChanged()
    {
      base.OnOriginAlignChanged();
      RefreshInternal();
    }

    protected override void OnSizeChanged()
    {
      base.OnSizeChanged();
      RefreshInternal();
    }

    public override void CheckIsWithinScissor(RectangleF scissor, bool complete = false)
    {
      Vector2 topLeft = Vector2.Zero,
              botRight = Vector2.Zero;
      GetScissorBounds(ref topLeft, ref botRight);

      Vector2 vector = new Vector2(Math.Max(topLeft.X, scissor.X), Math.Max(topLeft.Y, scissor.Y));
      Vector2 size = new Vector2(Math.Min(botRight.X, scissor.Right), Math.Min(botRight.Y, scissor.Bottom)) - vector;
      if (size.X <= 0f || size.Y <= 0f)
      {
        base.IsWithinScissor = false;
        foreach (MyGuiControlBase control in Controls)
          control.IsWithinScissor = false;
        return;
      }

      RectangleF scissor2 = default(RectangleF);
      scissor2.Position = vector;
      scissor2.Size = size;
      base.IsWithinScissor = true;
      foreach (MyGuiControlBase control in Controls)
        control.CheckIsWithinScissor(scissor2, complete);
    }

    void RefreshInternal()
    {
      if (decreaseButton == null || increaseButton == null)
        return;

      var buttonSize = new Vector2(base.Size.Y, base.Size.Y) * 0.95f;
      increaseButton.Position = new Vector2(base.Size.X * 0.5f + 0.003f, 0.004f);
      increaseButton.Size = buttonSize;
      decreaseButton.PositionToLeftOf(increaseButton, spacing: -0.006f);
      decreaseButton.Size = buttonSize;
    }
  }

}
