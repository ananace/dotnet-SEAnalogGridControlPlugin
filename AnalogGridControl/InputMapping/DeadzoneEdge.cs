namespace AnanaceDev.AnalogGridControl.InputMapping
{

  /// Which "point" of the input that the deadzone should apply to.
  public enum DeadzonePoint
  {
    /// Don't apply the deadzone anywhere on the range
    None,
    /// Apply the deadzone near the lower/upper end
    End,
    /// Apply the deadzone in the middle of the range
    Mid
  }

}
