namespace AnanaceDev.AnalogGridControl.Util
{
  public static class AnalogEmulation
  {

    public static bool ShouldTick(float input, int tick, int steps = 10)
    {
      if (input <= 0.0f)
        return false;
      if (input >= 1.0f)
        return true;

      for (int i = 0; i < steps; ++i)
      {
        float step = (1.0f / steps) * i;
        float nextStep = (1.0f / steps) * (i + 1);

        if (input >= step && input <= nextStep && (tick % (steps - i) == 0))
          return true;
      }

      return false;
    }
  }
}
