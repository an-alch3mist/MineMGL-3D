using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public static class IEnumerableUtilsPhaseAll
{
	#region IEnumerableExtension contents inside SPACE_UTIL for ref.
	/*
	#region find/findIndex
	/// <summary>
	/// find elem(null if not found) based on predicate.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="collection"></param>
	/// <param name="predicate"></param>
	/// <returns></returns>
	public static T find<T>(this IEnumerable<T> collection, Func<T, bool> predicate)
	{
		if (collection == null) throw new ArgumentNullException(nameof(collection));
		if (predicate == null) throw new ArgumentNullException(nameof(predicate));

		// collection.MoveNext(), or a foreach loop
		foreach (var item in collection)
			if (predicate(item))
				return item;
		Debug.Log($"elem doesnt exist returning { default(T).tryNullToString()}");
		return default(T); // Returns null for reference types, default value for value types
	}
	/// <summary>
	/// find elem index(-1 if not found) based on predicate.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="collection"></param>
	/// <param name="predicate"></param>
	/// <returns></returns>
	public static int findIndex<T>(this IEnumerable<T> collection, Func<T, bool> predicate)
	{
		if (collection == null) throw new ArgumentNullException(nameof(collection));
		if (predicate == null) throw new ArgumentNullException(nameof(predicate));

		int index = 0;
		// collection.MoveNext(), or a foreach loop
		foreach (var item in collection)
		{
			if (predicate(item))
				return index;
			index += 1;
		}
		return -1; // Returns -1 if found none
	}
	#endregion

	#region minMax
	/// <summary>
	/// to get min: (a) => float; the a with least float is returned
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="list"></param>
	/// <param name="cmpFunc"></param>
	/// <param name="splice"></param>
	/// <returns></returns>
	public static T minMaxA<T>(this List<T> list, Func<T, float> cmpFunc, bool splice = false)
	{
		if (list.Count < 1)
		{
			Debug.Log("minMaxA require atleast Count Of 1".colorTag("red"));
			throw new ArgumentException();
		}
		int minIndex = 0;
		for (int i0 = 0; i0 < list.Count; i0 += 1)
			if (cmpFunc(list[i0]) < cmpFunc(list[minIndex]))
				minIndex = i0;
		var min = list[minIndex];
		if (splice)
			list.RemoveAt(minIndex);
		return min;
	}
	/// <summary>
	/// to get min (a, b) => float; (the pair with least float is returned)
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="list"></param>
	/// <param name="cmpFunc"></param>
	/// <param name="splice"></param>
	/// <returns></returns>
	public static List<T> minMaxAB<T>(this List<T> list, Func<T, T, double> cmpFunc, bool splice = false)
	{
		if (list.Count < 2)
		{
			Debug.Log("minMaxAB require atleast 2 elements to compare");
			throw new ArgumentNullException();
		}

		int[] minIndexPair = new int[] { 0, 1 };
		for (int i0 = 0; i0 <= list.Count - 2; i0 += 1)
			for (int i1 = i0 + 1; i1 <= list.Count - 1; i1 += 1)
			{
				if (cmpFunc(list[i0], list[i1]) < cmpFunc(list[minIndexPair[0]], list[minIndexPair[1]])) // smaller connection than before
				{
					minIndexPair[0] = i0;
					minIndexPair[1] = i1;
				}
			}
		var minAB = new List<T>() { list[minIndexPair[0]], list[minIndexPair[1]] }; ;
		if (splice)
		{
			if (minIndexPair[1] > minIndexPair[0])
			{
				list.RemoveAt(minIndexPair[1]);
				list.RemoveAt(minIndexPair[0]);

			}
			else
			{
				list.RemoveAt(minIndexPair[0]);
				list.RemoveAt(minIndexPair[1]);
			}
		}
		return minAB;
	}
	#endregion

	#region get
	/// <summary>
	/// get an elem with index relative to last, 0 for last, 1 for 1th from last etc
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="collection"></param>
	/// <param name="indexFromLast"></param>
	/// <returns></returns>
	public static T getAtLast<T>(this IEnumerable<T> collection, int indexFromLast)
	{
		if (collection == null) throw new ArgumentNullException(nameof(collection)); // nameof(collection) = "collection"

		if (indexFromLast < 0 || indexFromLast >= collection.Count())
			Debug.LogError($"in {nameof(collection)} gl{indexFromLast} > count: {collection.Count()}");

		return collection.ElementAt(collection.Count() - 1 - indexFromLast);
	}

	/// <summary>
	/// Index accessor for IEnumerable (like JavaScript arrays).
	/// Usage: items.getAt(1) instead of items.ToList()[1]
	/// </summary>
	public static T getAt<T>(this IEnumerable<T> source, int index)
	{
		if (index < 0)
		{
			var list = source as IList<T> ?? source.ToList();
			index += list.Count;
			if (index < 0) throw new IndexOutOfRangeException();
			return list[index];
		}
		return source.ElementAtOrDefault(index);
	}

	/// <summary>
	/// getRandom between 0, .count - 1
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="collection"></param>
	/// <returns></returns>
	public static T getRandom<T>(this IEnumerable<T> source)
	{
		if (source == null) throw new ArgumentNullException(nameof(source));
		// Fast path for IList<T> (arrays, List<T>, etc.)
		if (source is IList<T> list)
		{
			if (list.Count == 0)
				throw new InvalidOperationException("Sequence was empty to fetch random.");
			return list[UnityEngine.Random.Range(0, list.Count)];
		}
		// Materialize once so we can pick an index uniformly.
		// (If the sequence is huge/streaming, avoid this overload and use a list directly.)
		var array = source.ToArray();
		if (array.Length == 0)
			throw new InvalidOperationException("Sequence was empty.");
		return array[UnityEngine.Random.Range(0, array.Length)];
	}

	#region dicitonary extension
	// Version 1: for types with parameterless ctor like List<T>, your classes
	public static TValue GetOrCreate<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key)
		where TValue : new()
	{
		if (!dict.TryGetValue(key, out TValue val))
		{
			val = new TValue();
			dict[key] = val;
		}
		return val;
	}

	// Version 2: for string, int, etc - pass default value explicitly
	public static TValue GetOrCreate<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue defaultValue)
	{
		if (!dict.TryGetValue(key, out TValue val))
		{
			val = defaultValue;
			dict[key] = val;
		}
		return val;
	}
	#endregion
	#endregion

	#region tryGet.... version
	/// <summary>
	/// Tries to get an element with index relative to last. 0 = last, 1 = second to last, etc.
	/// Returns default(T) if index is out of range or collection is null/empty.
	/// </summary>
	public static T TryGetAtLast<T>(this IEnumerable<T> source, int indexFromLast)
	{
		if (source == null || indexFromLast < 0)
			return default(T);

		// Fast path for IList<T> to avoid multiple enumeration
		if (source is IList<T> list)
		{
			int index = list.Count - 1 - indexFromLast;
			return index >= 0 && index < list.Count ? list[index] : default(T);
		}

		// For general IEnumerable - need count, so we materialize once
		var array = source.ToArray();
		int targetIndex = array.Length - 1 - indexFromLast;
		return targetIndex >= 0 && targetIndex < array.Length ? array[targetIndex] : default(T);
	}

	/// <summary>
	/// Tries to get element at index. Supports negative indices: -1 = last, -2 = second to last.
	/// Returns default(T) if index is out of range or collection is null.
	/// </summary>
	public static T TryGetAt<T>(this IEnumerable<T> source, int index)
	{
		if (source == null)
			return default(T);

		// Handle negative indices
		if (index < 0)
		{
			var list = source as IList<T> ?? source.ToList();
			index += list.Count;
			return index >= 0 && index < list.Count ? list[index] : default(T);
		}

		// Positive indices - use ElementAtOrDefault to avoid exceptions
		return source.ElementAtOrDefault(index);
	}

	/// <summary>
	/// Tries to get a random element from the sequence.
	/// Returns default(T) if collection is null or empty.
	/// </summary>
	public static T TryGetRandom<T>(this IEnumerable<T> source)
	{
		if (source == null)
			return default(T);

		// Fast path for IList<T>
		if (source is IList<T> list)
		{
			return list.Count == 0 ? default(T) : list[UnityEngine.Random.Range(0, list.Count)];
		}

		// Materialize once for IEnumerable
		var array = source.ToArray();
		return array.Length == 0 ? default(T) : array[UnityEngine.Random.Range(0, array.Length)];
	}
	#endregion

	#region all/any
	/// <summary>
	/// true if All of predicate(elem) == true.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="source"></param>
	/// <param name="predicate"></param>
	/// <returns></returns>
	public static bool all<T>(this IEnumerable<T> source, Func<T, bool> predicate)
	{
		if (source == null) throw new ArgumentNullException(nameof(source));
		if (predicate == null) throw new ArgumentNullException(nameof(predicate));

		foreach (var item in source)
			if (!predicate(item)) return false;
		return true;
	}
	/// <summary>
	///  true if Any of predicate(elem) == true.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="source"></param>
	/// <param name="predicate"></param>
	/// <returns></returns>
	public static bool any<T>(this IEnumerable<T> source, Func<T, bool> predicate)
	{
		if (source == null) throw new ArgumentNullException(nameof(source));
		if (predicate == null) throw new ArgumentNullException(nameof(predicate));

		foreach (var item in source)
			if (predicate(item)) return true;
		return false;
	}
	#endregion

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
	*/
	#endregion
	// >> Has Been Moved To SPACE_UTIL namespace
}