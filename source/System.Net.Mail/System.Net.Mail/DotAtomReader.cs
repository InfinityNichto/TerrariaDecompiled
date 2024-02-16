using System.Net.Mime;

namespace System.Net.Mail;

internal static class DotAtomReader
{
	internal static bool TryReadReverse(string data, int index, out int outIndex, bool throwExceptionIfFail)
	{
		int num = index;
		while (0 <= index && (data[index] > '\u007f' || data[index] == '.' || MailBnfHelper.Atext[(uint)data[index]]))
		{
			index--;
		}
		if (num == index)
		{
			if (throwExceptionIfFail)
			{
				throw new FormatException(System.SR.Format(System.SR.MailHeaderFieldInvalidCharacter, data[index]));
			}
			outIndex = 0;
			return false;
		}
		if (data[index + 1] == '.')
		{
			if (throwExceptionIfFail)
			{
				throw new FormatException(System.SR.Format(System.SR.MailHeaderFieldInvalidCharacter, '.'));
			}
			outIndex = 0;
			return false;
		}
		outIndex = index;
		return true;
	}
}
