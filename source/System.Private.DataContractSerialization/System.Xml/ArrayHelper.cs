namespace System.Xml;

internal abstract class ArrayHelper<TArgument, TArray>
{
	public TArray[] ReadArray(XmlDictionaryReader reader, TArgument localName, TArgument namespaceUri, int maxArrayLength)
	{
		TArray[][] array = null;
		TArray[] array2 = null;
		int num = 0;
		int num2 = 0;
		if (reader.TryGetArrayLength(out var count))
		{
			if (count > 65535)
			{
				count = 65535;
			}
		}
		else
		{
			count = 32;
		}
		while (true)
		{
			array2 = new TArray[count];
			int i;
			int num3;
			for (i = 0; i < array2.Length; i += num3)
			{
				num3 = ReadArray(reader, localName, namespaceUri, array2, i, array2.Length - i);
				if (num3 == 0)
				{
					break;
				}
			}
			num2 += i;
			if (i < array2.Length || reader.NodeType == XmlNodeType.EndElement)
			{
				break;
			}
			if (array == null)
			{
				array = new TArray[32][];
			}
			array[num++] = array2;
			count *= 2;
		}
		if (num2 != array2.Length || num > 0)
		{
			TArray[] array3 = new TArray[num2];
			int num4 = 0;
			for (int j = 0; j < num; j++)
			{
				Array.Copy(array[j], 0, array3, num4, array[j].Length);
				num4 += array[j].Length;
			}
			Array.Copy(array2, 0, array3, num4, num2 - num4);
			array2 = array3;
		}
		return array2;
	}

	public void WriteArray(XmlDictionaryWriter writer, string prefix, TArgument localName, TArgument namespaceUri, XmlDictionaryReader reader)
	{
		int count = ((!reader.TryGetArrayLength(out count)) ? 256 : Math.Min(count, 256));
		TArray[] array = new TArray[count];
		while (true)
		{
			int num = ReadArray(reader, localName, namespaceUri, array, 0, array.Length);
			if (num != 0)
			{
				WriteArray(writer, prefix, localName, namespaceUri, array, 0, num);
				continue;
			}
			break;
		}
	}

	protected abstract int ReadArray(XmlDictionaryReader reader, TArgument localName, TArgument namespaceUri, TArray[] array, int offset, int count);

	protected abstract void WriteArray(XmlDictionaryWriter writer, string prefix, TArgument localName, TArgument namespaceUri, TArray[] array, int offset, int count);
}
