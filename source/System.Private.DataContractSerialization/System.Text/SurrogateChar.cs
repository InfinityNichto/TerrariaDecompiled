using System.Globalization;
using System.Runtime.Serialization;

namespace System.Text;

internal struct SurrogateChar
{
	private readonly char _lowChar;

	private readonly char _highChar;

	public const int MinValue = 65536;

	public const int MaxValue = 1114111;

	private const char surHighMin = '\ud800';

	private const char surHighMax = '\udbff';

	private const char surLowMin = '\udc00';

	private const char surLowMax = '\udfff';

	public char LowChar => _lowChar;

	public char HighChar => _highChar;

	public int Char => (_lowChar - 56320) | ((_highChar - 55296 << 10) + 65536);

	public SurrogateChar(int ch)
	{
		if (ch < 65536 || ch > 1114111)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(System.SR.Format(System.SR.XmlInvalidSurrogate, ch.ToString("X", CultureInfo.InvariantCulture)), "ch"));
		}
		_lowChar = (char)(((ch - 65536) & 0x3FF) + 56320);
		_highChar = (char)(((ch - 65536 >> 10) & 0x3FF) + 55296);
	}

	public SurrogateChar(char lowChar, char highChar)
	{
		if (lowChar < '\udc00' || lowChar > '\udfff')
		{
			string xmlInvalidLowSurrogate = System.SR.XmlInvalidLowSurrogate;
			int num = lowChar;
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(System.SR.Format(xmlInvalidLowSurrogate, num.ToString("X", CultureInfo.InvariantCulture)), "lowChar"));
		}
		if (highChar < '\ud800' || highChar > '\udbff')
		{
			string xmlInvalidHighSurrogate = System.SR.XmlInvalidHighSurrogate;
			int num = highChar;
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(System.SR.Format(xmlInvalidHighSurrogate, num.ToString("X", CultureInfo.InvariantCulture)), "highChar"));
		}
		_lowChar = lowChar;
		_highChar = highChar;
	}
}
