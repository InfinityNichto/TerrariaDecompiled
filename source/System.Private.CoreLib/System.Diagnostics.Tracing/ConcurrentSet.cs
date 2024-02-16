using System.Threading;

namespace System.Diagnostics.Tracing;

internal struct ConcurrentSet<KeyType, ItemType> where ItemType : ConcurrentSetItem<KeyType, ItemType>
{
	private ItemType[] items;

	public ItemType TryGet(KeyType key)
	{
		ItemType[] array = items;
		if (array == null)
		{
			goto IL_0045;
		}
		int num = 0;
		int num2 = array.Length;
		ItemType val;
		while (true)
		{
			int num3 = (num + num2) / 2;
			val = array[num3];
			int num4 = val.Compare(key);
			if (num4 == 0)
			{
				break;
			}
			if (num4 < 0)
			{
				num = num3 + 1;
			}
			else
			{
				num2 = num3;
			}
			if (num != num2)
			{
				continue;
			}
			goto IL_0045;
		}
		goto IL_004d;
		IL_004d:
		return val;
		IL_0045:
		val = null;
		goto IL_004d;
	}

	public ItemType GetOrAdd(ItemType newItem)
	{
		ItemType[] array = items;
		ItemType val;
		while (true)
		{
			ItemType[] array2;
			if (array == null)
			{
				array2 = new ItemType[1] { newItem };
				goto IL_0088;
			}
			int num = 0;
			int num2 = array.Length;
			while (true)
			{
				int num3 = (num + num2) / 2;
				val = array[num3];
				int num4 = val.Compare(newItem);
				if (num4 == 0)
				{
					break;
				}
				if (num4 < 0)
				{
					num = num3 + 1;
				}
				else
				{
					num2 = num3;
				}
				if (num != num2)
				{
					continue;
				}
				goto IL_005a;
			}
			break;
			IL_0088:
			array2 = Interlocked.CompareExchange(ref items, array2, array);
			if (array != array2)
			{
				array = array2;
				continue;
			}
			val = newItem;
			break;
			IL_005a:
			int num5 = array.Length;
			array2 = new ItemType[num5 + 1];
			Array.Copy(array, array2, num);
			array2[num] = newItem;
			Array.Copy(array, num, array2, num + 1, num5 - num);
			goto IL_0088;
		}
		return val;
	}
}
