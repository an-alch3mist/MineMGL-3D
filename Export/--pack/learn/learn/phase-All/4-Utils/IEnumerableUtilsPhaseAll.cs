using System;
using System.Collections.Generic;

/// <summary>
/// IEnumerable/Dictionary extensions shared across ALL phases — .sum() matches SPACE_UTIL style,
/// .GetOrCreate() eliminates the repeated TryGetValue + new pattern on dictionaries.
/// </summary>
public static class IEnumerableUtilsPhaseAll
{
	#region sum
	/// <summary> sum with selector — lowercase to match SPACE_UTIL .map()/.find() style </summary>
	public static float sum<T>(this IEnumerable<T> source, Func<T, float> selector)
	{
		float total = 0f;
		foreach (var item in source) total += selector(item);
		return total;
	}
	/// <summary> int overload </summary>
	public static int sum<T>(this IEnumerable<T> source, Func<T, int> selector)
	{
		int total = 0;
		foreach (var item in source) total += selector(item);
		return total;
	}
	#endregion

	#region GetOrCreate
	/// <summary> returns existing value for key, or creates + stores + returns a new TValue() </summary>
	public static TValue GetOrCreate<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key)
		where TValue : new()
	{
		if (!dict.TryGetValue(key, out var val))
		{
			val = new TValue();
			dict[key] = val;
		}
		return val;
	}
	#endregion
}