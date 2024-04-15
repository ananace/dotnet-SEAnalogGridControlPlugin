namespace AnanaceDev.AnalogGridControl.InputMapping
{

  /// How the values outside the deazone should scale.
  public enum DeadzoneScale
  {
    /// Keeps the output linear, with values inside the deadzone being clamped to 0.
    ///
    /// This will act as if the deadzone is a null void for input values.
    Linear,
    /// Clamps the output to 0 inside the deadzone, then scales the output so that the first potential value outside the deadzone starts just outside 0.
    /// 
    /// This will act as if the deadzone is the actual physical end-stop on the input.
    Adaptive
  }

}
