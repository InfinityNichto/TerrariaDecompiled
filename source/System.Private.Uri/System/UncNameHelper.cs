namespace System;

internal static class UncNameHelper
{
	public static string ParseCanonicalName(string str, int start, int end, ref bool loopback)
	{
		return DomainNameHelper.ParseCanonicalName(str, start, end, ref loopback);
	}

	public unsafe static bool IsValid(char* name, int start, ref int returnedEnd, bool notImplicitFile)
	{
		int num = returnedEnd;
		if (start == num)
		{
			return false;
		}
		bool flag = false;
		int i;
		for (i = start; i < num; i++)
		{
			if (name[i] == '/' || name[i] == '\\' || (notImplicitFile && (name[i] == ':' || name[i] == '?' || name[i] == '#')))
			{
				num = i;
				break;
			}
			if (name[i] == '.')
			{
				i++;
				break;
			}
			if (char.IsLetter(name[i]) || name[i] == '-' || name[i] == '_')
			{
				flag = true;
			}
			else if (name[i] < '0' || name[i] > '9')
			{
				return false;
			}
		}
		if (!flag)
		{
			return false;
		}
		for (; i < num; i++)
		{
			if (name[i] == '/' || name[i] == '\\' || (notImplicitFile && (name[i] == ':' || name[i] == '?' || name[i] == '#')))
			{
				num = i;
				break;
			}
			if (name[i] == '.')
			{
				if (!flag || (i - 1 >= start && name[i - 1] == '.'))
				{
					return false;
				}
				flag = false;
				continue;
			}
			if (name[i] == '-' || name[i] == '_')
			{
				if (!flag)
				{
					return false;
				}
				continue;
			}
			if (char.IsLetter(name[i]) || (name[i] >= '0' && name[i] <= '9'))
			{
				if (!flag)
				{
					flag = true;
				}
				continue;
			}
			return false;
		}
		if (i - 1 >= start && name[i - 1] == '.')
		{
			flag = true;
		}
		if (!flag)
		{
			return false;
		}
		returnedEnd = num;
		return true;
	}
}
