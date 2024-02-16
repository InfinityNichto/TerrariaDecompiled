using System.Collections;

namespace System.Text.RegularExpressions;

internal sealed class RegexCode
{
	public readonly RegexTree Tree;

	public readonly int[] Codes;

	public readonly string[] Strings;

	public readonly int[][] StringsAsciiLookup;

	public readonly int TrackCount;

	public readonly Hashtable Caps;

	public readonly int CapSize;

	public readonly (string CharClass, bool CaseInsensitive)[] LeadingCharClasses;

	public int[] LeadingCharClassAsciiLookup;

	public readonly RegexBoyerMoore BoyerMoorePrefix;

	public readonly int LeadingAnchor;

	public readonly bool RightToLeft;

	public RegexCode(RegexTree tree, int[] codes, string[] strings, int trackcount, Hashtable caps, int capsize, RegexBoyerMoore boyerMoorePrefix, (string CharClass, bool CaseInsensitive)[] leadingCharClasses, int leadingAnchor, bool rightToLeft)
	{
		Tree = tree;
		Codes = codes;
		Strings = strings;
		StringsAsciiLookup = new int[strings.Length][];
		TrackCount = trackcount;
		Caps = caps;
		CapSize = capsize;
		BoyerMoorePrefix = boyerMoorePrefix;
		LeadingCharClasses = leadingCharClasses;
		LeadingAnchor = leadingAnchor;
		RightToLeft = rightToLeft;
	}

	public static bool OpcodeBacktracks(int Op)
	{
		Op &= 0x3F;
		switch (Op)
		{
		case 3:
		case 4:
		case 5:
		case 6:
		case 7:
		case 8:
		case 23:
		case 24:
		case 25:
		case 26:
		case 27:
		case 28:
		case 29:
		case 31:
		case 32:
		case 33:
		case 34:
		case 35:
		case 36:
		case 38:
			return true;
		default:
			return false;
		}
	}

	public static int OpcodeSize(int opcode)
	{
		opcode &= 0x3F;
		switch (opcode)
		{
		case 14:
		case 15:
		case 16:
		case 17:
		case 18:
		case 19:
		case 20:
		case 21:
		case 22:
		case 30:
		case 31:
		case 33:
		case 34:
		case 35:
		case 36:
		case 40:
		case 41:
		case 42:
		case 46:
			return 1;
		case 9:
		case 10:
		case 11:
		case 12:
		case 13:
		case 23:
		case 24:
		case 25:
		case 26:
		case 27:
		case 37:
		case 38:
			return 2;
		case 0:
		case 1:
		case 2:
		case 3:
		case 4:
		case 5:
		case 6:
		case 7:
		case 8:
		case 28:
		case 29:
		case 32:
		case 43:
		case 44:
		case 45:
			return 3;
		default:
			throw new ArgumentException(System.SR.Format(System.SR.UnexpectedOpcode, opcode.ToString()));
		}
	}
}
