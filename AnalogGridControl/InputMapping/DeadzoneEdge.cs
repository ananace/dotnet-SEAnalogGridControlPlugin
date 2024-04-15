using System;

namespace AnanaceDev.AnalogGridControl.InputMapping
{

  /// Which "edges" of the input that the deadzone should apply to.
  [Flags]
  public enum DeadzoneEdge
  {
    /// Apply the deadzone near the lower edge. (near -1)
    Lower  = 1 << 0,
    /// Apply the deadzone near the center. (near 0)
    Center = 1 << 1,
    /// Apply the deadzone near the upper edge. (near 1)
    Upper  = 1 << 2,

    /// Apply the deadzone to all edges.
    All = Lower | Center | Upper
  }

}
