using System.Collections.Specialized;

namespace Compiler.Util;

public static class EnumerableUtil
{
    public static OrderedDictionary ToOrderedDictionary<T>(
        this IEnumerable<T> source,
        Func<T, object> keySelector,
        Func<T, object?> valueSelector
    )
    {
        var dict = new OrderedDictionary();
        foreach (var el in source)
        {
            dict.Add(keySelector(el), valueSelector(el));
        }
        return dict;
    }
}
