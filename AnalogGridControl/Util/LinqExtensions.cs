using System.Collections.Generic;
using System.Linq;

namespace AnanaceDev.AnalogGridControl.Util
{

  public static class LinqExtensions
  {
    public static bool ContainsDuplicates<T>(this IEnumerable<T> enumerable)
    {
      var knownKeys = new HashSet<T>();
      return enumerable.Any(item => !knownKeys.Add(item));
    }
  }

}
