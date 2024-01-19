using System;
using Sandbox.Graphics.GUI;
using VRage.Utils;
using VRageMath;

namespace AnanaceDev.AnalogGridControl.Util
{

  public static class MyGuiControlExtensions
  {
    public static float GetOptimalSpacer(this MyGuiScreenBase _)
    {
      return 0.0175f;
    }
    public static float GetOptimalSpacer(this MyGuiControlBase _)
    {
      return 0.0175f;
    }

    public static Vector2 GetOptimalSpacerVector(this MyGuiScreenBase _)
    {
      return new Vector2(0.0175f, 0.0175f);
    }
    public static Vector2 GetOptimalSpacerVector(this MyGuiControlBase _)
    {
      return new Vector2(0.0175f, 0.0175f);
    }

    public static Vector2 GetCoordTopLeftFromAligned(this MyGuiControlBase control)
    {
      return MyUtils.GetCoordTopLeftFromAligned(control.Position, control.Size, control.OriginAlign);
    }

    public static RectangleF GetAreaBetween(this MyGuiScreenBase screen, MyGuiControlBase topControl, MyGuiControlBase bottomControl, float? horizSpace = null, float? vertSpace = null)
    {
      Vector2 halfSize = screen.Size.Value / 2;
      var spacing = new Vector2(horizSpace ?? screen.GetOptimalSpacer(), vertSpace ?? screen.GetOptimalSpacer());

      float topPosY = topControl.GetCoordTopLeftFromAligned().Y;
      Vector2 topPos = new Vector2(spacing.X - halfSize.X, topPosY + topControl.Size.Y + spacing.Y);

      float bottomPosY = bottomControl.GetCoordTopLeftFromAligned().Y;
      Vector2 bottomPos = new Vector2(halfSize.X - spacing.X, bottomPosY - spacing.Y);

      Vector2 size = bottomPos - topPos;
      size.X = Math.Abs(size.X);
      size.Y = Math.Abs(size.Y);

      return new RectangleF(topPos, size);
    }

    public static void PositionToRightOf(this MyGuiControlBase newControl, MyGuiControlBase currentControl, MyAlignV align = MyAlignV.Center, float? spacing = null)
    {
      Vector2 currentTopLeft = currentControl.GetCoordTopLeftFromAligned();
      currentTopLeft.X += currentControl.Size.X + (spacing ?? currentControl.GetOptimalSpacer());
      switch (align)
      {
        case MyAlignV.Top: newControl.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP; break;
        case MyAlignV.Center:
          currentTopLeft.Y += currentControl.Size.Y / 2;
          newControl.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER;
          break;
        case MyAlignV.Bottom:
          currentTopLeft.Y += currentControl.Size.Y;
          newControl.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM;
          break;
        default:
          return;
      }
      newControl.Position = currentTopLeft;
    }

    public static void PositionToLeftOf(this MyGuiControlBase newControl, MyGuiControlBase currentControl, MyAlignV align = MyAlignV.Center, float? spacing = null)
    {
      Vector2 currentTopLeft = currentControl.GetCoordTopLeftFromAligned();
      currentTopLeft.X -= spacing ?? currentControl.GetOptimalSpacer();
      switch (align)
      {
        case MyAlignV.Top: newControl.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP; break;
        case MyAlignV.Center:
          currentTopLeft.Y += currentControl.Size.Y / 2;
          newControl.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER;
          break;
        case MyAlignV.Bottom:
          currentTopLeft.Y += currentControl.Size.Y;
          newControl.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM;
          break;
        default:
          return;
      }
      newControl.Position = currentTopLeft;
    }

    public static void PositionAbove(this MyGuiControlBase newControl, MyGuiControlBase currentControl, MyAlignH align = MyAlignH.Center, float? spacing = null)
    {
      Vector2 currentTopLeft = currentControl.GetCoordTopLeftFromAligned();
      currentTopLeft.Y -= spacing ?? currentControl.GetOptimalSpacer();
      switch (align)
      {
        case MyAlignH.Left: newControl.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM; break;
        case MyAlignH.Center:
          currentTopLeft.X += currentControl.Size.X / 2;
          newControl.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_BOTTOM;
          break;
        case MyAlignH.Right:
          currentTopLeft.X += currentControl.Size.X;
          newControl.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM;
          break;
        default:
          return;
      }
      newControl.Position = currentTopLeft;
    }

    public static void PositionBelow(this MyGuiControlBase newControl, MyGuiControlBase currentControl, MyAlignH align = MyAlignH.Center, float? spacing = null)
    {
      Vector2 currentTopLeft = currentControl.GetCoordTopLeftFromAligned();
      currentTopLeft.Y += currentControl.Size.Y + spacing ?? currentControl.GetOptimalSpacer();
      switch (align)
      {
        case MyAlignH.Left: newControl.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP; break;
        case MyAlignH.Center:
          currentTopLeft.X += currentControl.Size.X / 2;
          newControl.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP;
          break;
        case MyAlignH.Right:
          currentTopLeft.X += currentControl.Size.X;
          newControl.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP;
          break;
        default:
          return;
      }
      newControl.Position = currentTopLeft;
    }

    public static void SetTableHeight(this MyGuiControlTable table, float? height = null)
    {
      float numRows = (height ?? table.Size.Y) / table.RowHeight;
      table.VisibleRowsCount = Math.Max((int)numRows - 1, 1);
    }

    public static void AddImageToButton(this MyGuiControlButton button, string iconTexture, float iconSize = 1)
    {
      MyGuiControlImage icon = new MyGuiControlImage(size: button.Size * iconSize, textures: new[] { iconTexture });
      icon.Enabled = button.Enabled;
      icon.HasHighlight = button.HasHighlight;
      button.Elements.Add(icon);
    }
  };

}
