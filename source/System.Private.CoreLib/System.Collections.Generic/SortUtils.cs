namespace System.Collections.Generic;

internal static class SortUtils
{
	public static int MoveNansToFront<TKey, TValue>(Span<TKey> keys, Span<TValue> values)
	{
		int num = 0;
		for (int i = 0; i < keys.Length; i++)
		{
			if ((typeof(TKey) == typeof(double) && double.IsNaN((double)(object)keys[i])) || (typeof(TKey) == typeof(float) && float.IsNaN((float)(object)keys[i])) || (typeof(TKey) == typeof(Half) && Half.IsNaN((Half)(object)keys[i])))
			{
				TKey val = keys[num];
				keys[num] = keys[i];
				keys[i] = val;
				if ((uint)i < (uint)values.Length)
				{
					TValue val2 = values[num];
					values[num] = values[i];
					values[i] = val2;
				}
				num++;
			}
		}
		return num;
	}
}
