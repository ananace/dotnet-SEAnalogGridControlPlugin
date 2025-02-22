namespace AnanaceDev.AnalogGridControl.Util
{

  public static class MathExt
  {
    public static double Lerp(double v0, double v1, double t)
    {
      return v0 + t * (v1 - v0);
    }

    public static double InverseLerp(double a, double b, double v)
    {
      return (v - a) / (b - a);
    }
  }

}
