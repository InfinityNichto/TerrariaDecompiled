namespace System.Xml;

internal sealed class TernaryTreeReadOnly
{
	private readonly byte[] _nodeBuffer;

	public TernaryTreeReadOnly(byte[] nodeBuffer)
	{
		_nodeBuffer = nodeBuffer;
	}

	public byte FindCaseInsensitiveString(string stringToFind)
	{
		int num = 0;
		int num2 = 0;
		byte[] nodeBuffer = _nodeBuffer;
		int num3 = stringToFind[num];
		if (num3 > 122)
		{
			return 0;
		}
		if (num3 >= 97)
		{
			num3 -= 32;
		}
		while (true)
		{
			int num4 = num2 * 4;
			int num5 = nodeBuffer[num4];
			if (num3 < num5)
			{
				if (nodeBuffer[num4 + 1] == 0)
				{
					break;
				}
				num2 += nodeBuffer[num4 + 1];
				continue;
			}
			if (num3 > num5)
			{
				if (nodeBuffer[num4 + 2] == 0)
				{
					break;
				}
				num2 += nodeBuffer[num4 + 2];
				continue;
			}
			if (num3 == 0)
			{
				return nodeBuffer[num4 + 3];
			}
			num2++;
			num++;
			if (num == stringToFind.Length)
			{
				num3 = 0;
				continue;
			}
			num3 = stringToFind[num];
			if (num3 > 122)
			{
				return 0;
			}
			if (num3 >= 97)
			{
				num3 -= 32;
			}
		}
		return 0;
	}
}
