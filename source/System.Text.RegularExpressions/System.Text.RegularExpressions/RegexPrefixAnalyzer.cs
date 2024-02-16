using System.Collections.Generic;
using System.Globalization;

namespace System.Text.RegularExpressions;

internal ref struct RegexPrefixAnalyzer
{
	private readonly List<RegexFC> _fcStack;

	private System.Collections.Generic.ValueListBuilder<int> _intStack;

	private bool _skipAllChildren;

	private bool _skipchild;

	private bool _failed;

	private RegexPrefixAnalyzer(Span<int> intStack)
	{
		_fcStack = new List<RegexFC>(32);
		_intStack = new System.Collections.Generic.ValueListBuilder<int>(intStack);
		_failed = false;
		_skipchild = false;
		_skipAllChildren = false;
	}

	public static (string Prefix, bool CaseInsensitive) ComputeLeadingSubstring(RegexTree tree)
	{
		RegexNode regexNode = tree.Root;
		RegexNode regexNode2 = null;
		int num = 0;
		while (true)
		{
			switch (regexNode.Type)
			{
			case 25:
				if (regexNode.ChildCount() > 0)
				{
					regexNode2 = regexNode;
					num = 0;
				}
				break;
			case 28:
			case 32:
				regexNode = regexNode.Child(0);
				regexNode2 = null;
				continue;
			case 3:
			case 6:
			case 43:
				if (regexNode.M > 0 && regexNode.M < 50000)
				{
					return (Prefix: new string(regexNode.Ch, regexNode.M), CaseInsensitive: (regexNode.Options & RegexOptions.IgnoreCase) != 0);
				}
				return (Prefix: string.Empty, CaseInsensitive: false);
			case 9:
				return (Prefix: regexNode.Ch.ToString(), CaseInsensitive: (regexNode.Options & RegexOptions.IgnoreCase) != 0);
			case 12:
				return (Prefix: regexNode.Str, CaseInsensitive: (regexNode.Options & RegexOptions.IgnoreCase) != 0);
			default:
				return (Prefix: string.Empty, CaseInsensitive: false);
			case 14:
			case 15:
			case 16:
			case 18:
			case 19:
			case 20:
			case 21:
			case 23:
			case 30:
			case 31:
			case 41:
				break;
			}
			if (regexNode2 == null || num >= regexNode2.ChildCount())
			{
				break;
			}
			regexNode = regexNode2.Child(num++);
		}
		return (Prefix: string.Empty, CaseInsensitive: false);
	}

	public static (string CharClass, bool CaseInsensitive)[] ComputeFirstCharClass(RegexTree tree)
	{
		Span<int> intStack = stackalloc int[32];
		RegexPrefixAnalyzer regexPrefixAnalyzer = new RegexPrefixAnalyzer(intStack);
		RegexFC regexFC = regexPrefixAnalyzer.RegexFCFromRegexTree(tree);
		regexPrefixAnalyzer.Dispose();
		if (regexFC == null || regexFC._nullable)
		{
			return null;
		}
		if (regexFC.CaseInsensitive)
		{
			regexFC.AddLowercase(((tree.Options & RegexOptions.CultureInvariant) != 0) ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture);
		}
		return new(string, bool)[1] { (regexFC.GetFirstChars(), regexFC.CaseInsensitive) };
	}

	public static (string CharClass, bool CaseInsensitive)[] ComputeMultipleCharClasses(RegexTree tree, int maxChars)
	{
		if ((tree.Options & RegexOptions.RightToLeft) != 0)
		{
			return null;
		}
		maxChars = Math.Min(tree.MinRequiredLength, maxChars);
		if (maxChars <= 1)
		{
			return null;
		}
		RegexNode regexNode = tree.Root;
		while (regexNode.Type != 24)
		{
			int type = regexNode.Type;
			if (type == 25 || type == 28 || type == 32)
			{
				regexNode = regexNode.Child(0);
				continue;
			}
			return null;
		}
		RegexCharClass[] array = new RegexCharClass[maxChars];
		bool flag = false;
		int num = regexNode.ChildCount();
		for (int i = 0; i < num; i++)
		{
			RegexNode regexNode2 = regexNode.Child(i);
			flag |= (regexNode2.Options & RegexOptions.IgnoreCase) != 0;
			switch (regexNode2.Type)
			{
			case 12:
			{
				maxChars = Math.Min(maxChars, regexNode2.Str.Length);
				for (int l = 0; l < maxChars; l++)
				{
					ref RegexCharClass reference = ref array[l];
					(reference ?? (reference = new RegexCharClass())).AddChar(regexNode2.Str[l]);
				}
				break;
			}
			case 25:
			{
				int num2 = 0;
				int num3 = regexNode2.ChildCount();
				for (int j = 0; j < num3; j++)
				{
					if (num2 >= array.Length)
					{
						break;
					}
					RegexNode regexNode3 = regexNode2.Child(j);
					flag |= (regexNode3.Options & RegexOptions.IgnoreCase) != 0;
					switch (regexNode3.Type)
					{
					case 9:
					{
						ref RegexCharClass reference = ref array[num2++];
						(reference ?? (reference = new RegexCharClass())).AddChar(regexNode3.Ch);
						break;
					}
					case 11:
					{
						ref RegexCharClass reference = ref array[num2++];
						if (!(reference ?? (reference = new RegexCharClass())).TryAddCharClass(RegexCharClass.Parse(regexNode3.Str)))
						{
							return null;
						}
						break;
					}
					case 12:
					{
						for (int k = 0; k < regexNode3.Str.Length; k++)
						{
							if (num2 >= array.Length)
							{
								break;
							}
							ref RegexCharClass reference = ref array[num2++];
							(reference ?? (reference = new RegexCharClass())).AddChar(regexNode3.Str[k]);
						}
						break;
					}
					default:
						j = num3;
						break;
					}
				}
				maxChars = Math.Min(maxChars, num2);
				break;
			}
			default:
				return null;
			}
		}
		for (int m = 0; m < maxChars; m++)
		{
			if (array[m] == null)
			{
				maxChars = m;
				break;
			}
		}
		if (maxChars == 0)
		{
			return null;
		}
		(string, bool)[] array2 = new(string, bool)[maxChars];
		CultureInfo culture = null;
		if (flag)
		{
			culture = (((tree.Options & RegexOptions.CultureInvariant) != 0) ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture);
		}
		for (int n = 0; n < array2.Length; n++)
		{
			if (flag)
			{
				array[n].AddLowercase(culture);
			}
			array2[n] = (array[n].ToStringClass(), flag);
		}
		return array2;
	}

	public static int FindLeadingAnchor(RegexTree tree)
	{
		RegexNode regexNode = tree.Root;
		RegexNode regexNode2 = null;
		int num = 0;
		while (true)
		{
			switch (regexNode.Type)
			{
			case 14:
				return 2;
			case 15:
				return 8;
			case 16:
				return 64;
			case 41:
				return 128;
			case 18:
				return 1;
			case 19:
				return 4;
			case 20:
				return 16;
			case 21:
				return 32;
			case 25:
				if (regexNode.ChildCount() > 0)
				{
					regexNode2 = regexNode;
					num = 0;
				}
				break;
			case 28:
			case 32:
				regexNode = regexNode.Child(0);
				regexNode2 = null;
				continue;
			default:
				return 0;
			case 23:
			case 30:
			case 31:
				break;
			}
			if (regexNode2 == null || num >= regexNode2.ChildCount())
			{
				break;
			}
			regexNode = regexNode2.Child(num++);
		}
		return 0;
	}

	private void PushInt(int i)
	{
		_intStack.Append(i);
	}

	private bool IntIsEmpty()
	{
		return _intStack.Length == 0;
	}

	private int PopInt()
	{
		return _intStack.Pop();
	}

	private void PushFC(RegexFC fc)
	{
		_fcStack.Add(fc);
	}

	private bool FCIsEmpty()
	{
		return _fcStack.Count == 0;
	}

	private RegexFC PopFC()
	{
		RegexFC result = TopFC();
		_fcStack.RemoveAt(_fcStack.Count - 1);
		return result;
	}

	private RegexFC TopFC()
	{
		return _fcStack[_fcStack.Count - 1];
	}

	public void Dispose()
	{
		_intStack.Dispose();
	}

	private RegexFC RegexFCFromRegexTree(RegexTree tree)
	{
		RegexNode regexNode = tree.Root;
		int num = 0;
		while (true)
		{
			int num2 = regexNode.ChildCount();
			if (num2 == 0)
			{
				CalculateFC(regexNode.Type, regexNode, 0);
			}
			else if (num < num2 && !_skipAllChildren)
			{
				CalculateFC(regexNode.Type | 0x40, regexNode, num);
				if (!_skipchild)
				{
					regexNode = regexNode.Child(num);
					PushInt(num);
					num = 0;
				}
				else
				{
					num++;
					_skipchild = false;
				}
				continue;
			}
			_skipAllChildren = false;
			if (IntIsEmpty())
			{
				break;
			}
			num = PopInt();
			regexNode = regexNode.Next;
			CalculateFC(regexNode.Type | 0x80, regexNode, num);
			if (_failed)
			{
				return null;
			}
			num++;
		}
		if (FCIsEmpty())
		{
			return null;
		}
		return PopFC();
	}

	private void SkipChild()
	{
		_skipchild = true;
	}

	private void CalculateFC(int NodeType, RegexNode node, int CurIndex)
	{
		bool caseInsensitive = (node.Options & RegexOptions.IgnoreCase) != 0;
		bool flag = (node.Options & RegexOptions.RightToLeft) != 0;
		switch (NodeType)
		{
		case 98:
			if (CurIndex == 0)
			{
				SkipChild();
			}
			break;
		case 23:
			PushFC(new RegexFC(nullable: true));
			break;
		case 153:
			if (CurIndex != 0)
			{
				RegexFC fc3 = PopFC();
				RegexFC regexFC3 = TopFC();
				_failed = !regexFC3.AddFC(fc3, concatenate: true);
			}
			if (!TopFC()._nullable)
			{
				_skipAllChildren = true;
			}
			break;
		case 162:
			if (CurIndex > 1)
			{
				RegexFC fc2 = PopFC();
				RegexFC regexFC2 = TopFC();
				_failed = !regexFC2.AddFC(fc2, concatenate: false);
			}
			break;
		case 152:
		case 161:
			if (CurIndex != 0)
			{
				RegexFC fc = PopFC();
				RegexFC regexFC = TopFC();
				_failed = !regexFC.AddFC(fc, concatenate: false);
			}
			break;
		case 154:
		case 155:
			if (node.M == 0)
			{
				TopFC()._nullable = true;
			}
			break;
		case 94:
		case 95:
			SkipChild();
			PushFC(new RegexFC(nullable: true));
			break;
		case 9:
		case 10:
			PushFC(new RegexFC(node.Ch, NodeType == 10, nullable: false, caseInsensitive));
			break;
		case 3:
		case 6:
		case 43:
			PushFC(new RegexFC(node.Ch, not: false, node.M == 0, caseInsensitive));
			break;
		case 4:
		case 7:
		case 44:
			PushFC(new RegexFC(node.Ch, not: true, node.M == 0, caseInsensitive));
			break;
		case 12:
			if (node.Str.Length == 0)
			{
				PushFC(new RegexFC(nullable: true));
			}
			else if (!flag)
			{
				PushFC(new RegexFC(node.Str[0], not: false, nullable: false, caseInsensitive));
			}
			else
			{
				PushFC(new RegexFC(node.Str[node.Str.Length - 1], not: false, nullable: false, caseInsensitive));
			}
			break;
		case 11:
			PushFC(new RegexFC(node.Str, nullable: false, caseInsensitive));
			break;
		case 5:
		case 8:
		case 45:
			PushFC(new RegexFC(node.Str, node.M == 0, caseInsensitive));
			break;
		case 13:
			PushFC(new RegexFC("\0\u0001\0\0", nullable: true, caseInsensitive: false));
			break;
		case 14:
		case 15:
		case 16:
		case 17:
		case 18:
		case 19:
		case 20:
		case 21:
		case 22:
		case 41:
		case 42:
		case 46:
			PushFC(new RegexFC(nullable: true));
			break;
		default:
			throw new ArgumentException(System.SR.Format(System.SR.UnexpectedOpcode, NodeType.ToString(CultureInfo.CurrentCulture)));
		case 88:
		case 89:
		case 90:
		case 91:
		case 92:
		case 93:
		case 96:
		case 97:
		case 156:
		case 157:
		case 158:
		case 159:
		case 160:
			break;
		}
	}
}
