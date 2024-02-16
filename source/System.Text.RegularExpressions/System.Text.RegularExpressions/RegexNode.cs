using System.Collections.Generic;

namespace System.Text.RegularExpressions;

internal sealed class RegexNode
{
	private object Children;

	public RegexOptions Options;

	public RegexNode Next;

	public int Type { get; private set; }

	public string Str { get; private set; }

	public char Ch { get; private set; }

	public int M { get; private set; }

	public int N { get; private set; }

	public RegexNode(int type, RegexOptions options)
	{
		Type = type;
		Options = options;
	}

	public RegexNode(int type, RegexOptions options, char ch)
	{
		Type = type;
		Options = options;
		Ch = ch;
	}

	public RegexNode(int type, RegexOptions options, string str)
	{
		Type = type;
		Options = options;
		Str = str;
	}

	public RegexNode(int type, RegexOptions options, int m)
	{
		Type = type;
		Options = options;
		M = m;
	}

	public RegexNode(int type, RegexOptions options, int m, int n)
	{
		Type = type;
		Options = options;
		M = m;
		N = n;
	}

	public bool UseOptionR()
	{
		return (Options & RegexOptions.RightToLeft) != 0;
	}

	public RegexNode ReverseLeft()
	{
		if (UseOptionR() && Type == 25 && ChildCount() > 1)
		{
			((List<RegexNode>)Children).Reverse();
		}
		return this;
	}

	private void MakeRep(int type, int min, int max)
	{
		Type += type - 9;
		M = min;
		N = max;
	}

	private void MakeLoopAtomic()
	{
		switch (Type)
		{
		case 3:
			Type = 43;
			break;
		case 4:
			Type = 44;
			break;
		default:
			Type = 45;
			break;
		}
	}

	internal RegexNode FinalOptimize()
	{
		if ((Options & RegexOptions.RightToLeft) == 0)
		{
			EliminateEndingBacktracking(Child(0), 20u);
			RegexNode regexNode = Child(0);
			while (true)
			{
				RegexNode next;
				switch (regexNode.Type)
				{
				case 25:
				case 32:
					goto IL_006f;
				case 3:
					if (regexNode.N != int.MaxValue)
					{
						break;
					}
					goto IL_00d4;
				case 43:
					if (regexNode.N != int.MaxValue)
					{
						break;
					}
					goto IL_00d4;
				case 4:
					if (regexNode.N != int.MaxValue)
					{
						break;
					}
					goto IL_00d4;
				case 44:
					if (regexNode.N != int.MaxValue)
					{
						break;
					}
					goto IL_00d4;
				case 5:
					if (regexNode.N != int.MaxValue)
					{
						break;
					}
					goto IL_00d4;
				case 45:
					{
						if (regexNode.N != int.MaxValue)
						{
							break;
						}
						goto IL_00d4;
					}
					IL_00d4:
					next = regexNode.Next;
					if (next != null && next.Type == 25)
					{
						next.InsertChild(1, new RegexNode(46, regexNode.Options));
					}
					break;
				}
				break;
				IL_006f:
				regexNode = regexNode.Child(0);
			}
		}
		while (Child(0).Type == 32)
		{
			ReplaceChild(0, Child(0).Child(0));
		}
		return this;
	}

	private static void EliminateEndingBacktracking(RegexNode node, uint maxDepth)
	{
		if (maxDepth == 0)
		{
			return;
		}
		while (true)
		{
			switch (node.Type)
			{
			case 3:
			case 4:
			case 5:
				node.MakeLoopAtomic();
				return;
			case 25:
			case 28:
			{
				RegexNode regexNode2 = node.Child(node.ChildCount() - 1);
				if ((regexNode2.Type == 24 || regexNode2.Type == 26 || regexNode2.Type == 27) && (node.Next == null || node.Next.Type != 32))
				{
					RegexNode regexNode3 = new RegexNode(32, regexNode2.Options);
					regexNode3.AddChild(regexNode2);
					node.ReplaceChild(node.ChildCount() - 1, regexNode3);
				}
				node = regexNode2;
				break;
			}
			case 24:
			{
				int num = node.ChildCount();
				for (int i = 1; i < num; i++)
				{
					EliminateEndingBacktracking(node.Child(i), maxDepth - 1);
				}
				node = node.Child(0);
				break;
			}
			case 26:
			{
				RegexNode regexNode = FindLastExpressionInLoopForAutoAtomic(node, maxDepth - 1);
				if (regexNode != null)
				{
					node = regexNode;
					break;
				}
				return;
			}
			default:
				return;
			}
		}
	}

	public bool IsAtomicByParent()
	{
		RegexNode next = Next;
		if (next == null)
		{
			return false;
		}
		if (next.Type == 32)
		{
			return true;
		}
		if ((next.Type != 25 && next.Type != 28) || next.Child(next.ChildCount() - 1) != this)
		{
			return false;
		}
		next = next.Next;
		if (next != null)
		{
			return next.Type == 32;
		}
		return false;
	}

	private RegexNode Reduce()
	{
		switch (Type)
		{
		case 24:
			return ReduceAlternation();
		case 25:
			return ReduceConcatenation();
		case 26:
		case 27:
			return ReduceLoops();
		case 32:
			return ReduceAtomic();
		case 29:
			return ReduceGroup();
		case 5:
		case 8:
		case 11:
		case 45:
			return ReduceSet();
		default:
			return this;
		}
	}

	private RegexNode ReplaceNodeIfUnnecessary(int emptyTypeIfNoChildren)
	{
		return ChildCount() switch
		{
			0 => new RegexNode(emptyTypeIfNoChildren, Options), 
			1 => Child(0), 
			_ => this, 
		};
	}

	private RegexNode ReduceGroup()
	{
		RegexNode regexNode = this;
		while (regexNode.Type == 29)
		{
			regexNode = regexNode.Child(0);
		}
		return regexNode;
	}

	private RegexNode ReduceAtomic()
	{
		RegexNode regexNode = this;
		RegexNode regexNode2 = Child(0);
		while (regexNode2.Type == 32)
		{
			regexNode = regexNode2;
			regexNode2 = regexNode.Child(0);
		}
		switch (regexNode2.Type)
		{
		case 43:
		case 44:
		case 45:
			return regexNode2;
		case 3:
		case 4:
		case 5:
			regexNode2.MakeLoopAtomic();
			return regexNode2;
		default:
			EliminateEndingBacktracking(regexNode2, 20u);
			return regexNode;
		}
	}

	private RegexNode ReduceLoops()
	{
		RegexNode regexNode = this;
		int type = Type;
		int num = M;
		int num2 = N;
		while (regexNode.ChildCount() > 0)
		{
			RegexNode regexNode2 = regexNode.Child(0);
			if (regexNode2.Type != type)
			{
				bool flag = false;
				if (type == 26)
				{
					int type2 = regexNode2.Type;
					if ((uint)(type2 - 3) <= 2u || (uint)(type2 - 43) <= 2u)
					{
						flag = true;
					}
				}
				else
				{
					int type3 = regexNode2.Type;
					if ((uint)(type3 - 6) <= 2u)
					{
						flag = true;
					}
				}
				if (!flag)
				{
					break;
				}
			}
			if ((regexNode.M == 0 && regexNode2.M > 1) || regexNode2.N < regexNode2.M * 2)
			{
				break;
			}
			regexNode = regexNode2;
			if (regexNode.M > 0)
			{
				num = (regexNode.M = ((2147483646 / regexNode.M < num) ? int.MaxValue : (regexNode.M * num)));
			}
			if (regexNode.N > 0)
			{
				num2 = (regexNode.N = ((2147483646 / regexNode.N < num2) ? int.MaxValue : (regexNode.N * num2)));
			}
		}
		if (num == int.MaxValue)
		{
			return new RegexNode(22, Options);
		}
		if (regexNode.ChildCount() == 1)
		{
			RegexNode regexNode3 = regexNode.Child(0);
			int type4 = regexNode3.Type;
			if ((uint)(type4 - 9) <= 2u)
			{
				regexNode3.MakeRep((regexNode.Type == 27) ? 6 : 3, regexNode.M, regexNode.N);
				regexNode = regexNode3;
			}
		}
		return regexNode;
	}

	private RegexNode ReduceSet()
	{
		if (RegexCharClass.IsEmpty(Str))
		{
			Type = 22;
			Str = null;
		}
		else if (RegexCharClass.IsSingleton(Str))
		{
			Ch = RegexCharClass.SingletonChar(Str);
			Str = null;
			Type = ((Type == 11) ? 9 : ((Type == 5) ? 3 : ((Type == 45) ? 43 : 6)));
		}
		else if (RegexCharClass.IsSingletonInverse(Str))
		{
			Ch = RegexCharClass.SingletonChar(Str);
			Str = null;
			Type = ((Type == 11) ? 10 : ((Type == 5) ? 4 : ((Type == 45) ? 44 : 7)));
		}
		return this;
	}

	private RegexNode ReduceAlternation()
	{
		switch (ChildCount())
		{
		case 0:
			return new RegexNode(22, Options);
		case 1:
			return Child(0);
		default:
		{
			ReduceSingleLetterAndNestedAlternations();
			RegexNode regexNode = ReplaceNodeIfUnnecessary(22);
			if (regexNode == this)
			{
				return ExtractCommonPrefix();
			}
			return regexNode;
		}
		}
		RegexNode ExtractCommonPrefix()
		{
			List<RegexNode> list = (List<RegexNode>)Children;
			if ((Options & RegexOptions.RightToLeft) != 0)
			{
				return this;
			}
			RegexNode regexNode2 = FindBranchOneMultiStart(list[0]);
			if (regexNode2 == null)
			{
				return this;
			}
			RegexOptions options = regexNode2.Options;
			string str = regexNode2.Str;
			ReadOnlySpan<char> readOnlySpan;
			if (regexNode2.Type == 9)
			{
				Span<char> span = stackalloc char[1] { regexNode2.Ch };
				readOnlySpan = span;
			}
			else
			{
				readOnlySpan = str;
			}
			ReadOnlySpan<char> startingSpan2 = readOnlySpan;
			for (int i = 1; i < list.Count; i++)
			{
				regexNode2 = FindBranchOneMultiStart(list[i]);
				if (regexNode2 == null || regexNode2.Options != options)
				{
					return this;
				}
				if (regexNode2.Type == 9)
				{
					if (startingSpan2[0] != regexNode2.Ch)
					{
						return this;
					}
					if (startingSpan2.Length != 1)
					{
						startingSpan2 = startingSpan2.Slice(0, 1);
					}
				}
				else
				{
					int num = Math.Min(startingSpan2.Length, regexNode2.Str.Length);
					int j;
					for (j = 0; j < num && startingSpan2[j] == regexNode2.Str[j]; j++)
					{
					}
					if (j == 0)
					{
						return this;
					}
					startingSpan2 = startingSpan2.Slice(0, j);
				}
			}
			for (int k = 0; k < list.Count; k++)
			{
				RegexNode regexNode3 = list[k];
				if (regexNode3.Type == 25)
				{
					ProcessOneOrMulti(regexNode3.Child(0), startingSpan2);
					ReplaceChild(k, regexNode3.Reduce());
				}
				else
				{
					ProcessOneOrMulti(regexNode3, startingSpan2);
				}
			}
			for (int l = 0; l < list.Count; l++)
			{
				if (list[l].Type == 23)
				{
					int m = l + 1;
					int num2 = m;
					for (; m < list.Count; m++)
					{
						if (list[m].Type != 23)
						{
							if (num2 != m)
							{
								list[num2] = list[m];
							}
							num2++;
						}
					}
					if (num2 < m)
					{
						list.RemoveRange(num2, m - num2);
					}
					break;
				}
			}
			RegexNode regexNode4 = new RegexNode(25, Options);
			regexNode4.AddChild((startingSpan2.Length == 1) ? new RegexNode(9, options)
			{
				Ch = startingSpan2[0]
			} : new RegexNode(12, options)
			{
				Str = ((str?.Length == startingSpan2.Length) ? str : startingSpan2.ToString())
			});
			regexNode4.AddChild(this);
			return regexNode4;
		}
		static RegexNode FindBranchOneMultiStart(RegexNode branch)
		{
			if (branch.Type == 25)
			{
				branch = branch.Child(0);
			}
			if (branch.Type != 9 && branch.Type != 12)
			{
				return null;
			}
			return branch;
		}
		static void ProcessOneOrMulti(RegexNode node, ReadOnlySpan<char> startingSpan)
		{
			if (node.Type == 9)
			{
				node.Type = 23;
				node.Ch = '\0';
			}
			else if (node.Str.Length == startingSpan.Length)
			{
				node.Type = 23;
				node.Str = null;
			}
			else if (node.Str.Length - 1 == startingSpan.Length)
			{
				node.Type = 9;
				node.Ch = node.Str[^1];
				node.Str = null;
			}
			else
			{
				node.Str = node.Str.Substring(startingSpan.Length);
			}
		}
		void ReduceSingleLetterAndNestedAlternations()
		{
			bool flag = false;
			bool flag2 = false;
			RegexOptions regexOptions = RegexOptions.None;
			List<RegexNode> list2 = (List<RegexNode>)Children;
			int n = 0;
			int num3;
			for (num3 = 0; n < list2.Count; n++, num3++)
			{
				RegexNode regexNode5 = list2[n];
				if (num3 < n)
				{
					list2[num3] = regexNode5;
				}
				if (regexNode5.Type == 24)
				{
					if (regexNode5.Children is List<RegexNode> list3)
					{
						for (int num4 = 0; num4 < list3.Count; num4++)
						{
							list3[num4].Next = this;
						}
						list2.InsertRange(n + 1, list3);
					}
					else
					{
						RegexNode regexNode6 = (RegexNode)regexNode5.Children;
						regexNode6.Next = this;
						list2.Insert(n + 1, regexNode6);
					}
					num3--;
				}
				else if (regexNode5.Type == 11 || regexNode5.Type == 9)
				{
					RegexOptions regexOptions2 = regexNode5.Options & (RegexOptions.IgnoreCase | RegexOptions.RightToLeft);
					if (regexNode5.Type == 11)
					{
						if (!flag || regexOptions != regexOptions2 || flag2 || !RegexCharClass.IsMergeable(regexNode5.Str))
						{
							flag = true;
							flag2 = !RegexCharClass.IsMergeable(regexNode5.Str);
							regexOptions = regexOptions2;
							continue;
						}
					}
					else if (!flag || regexOptions != regexOptions2 || flag2)
					{
						flag = true;
						flag2 = false;
						regexOptions = regexOptions2;
						continue;
					}
					num3--;
					RegexNode regexNode7 = list2[num3];
					RegexCharClass regexCharClass;
					if (regexNode7.Type == 9)
					{
						regexCharClass = new RegexCharClass();
						regexCharClass.AddChar(regexNode7.Ch);
					}
					else
					{
						regexCharClass = RegexCharClass.Parse(regexNode7.Str);
					}
					if (regexNode5.Type == 9)
					{
						regexCharClass.AddChar(regexNode5.Ch);
					}
					else
					{
						RegexCharClass cc = RegexCharClass.Parse(regexNode5.Str);
						regexCharClass.AddCharClass(cc);
					}
					regexNode7.Type = 11;
					regexNode7.Str = regexCharClass.ToStringClass();
				}
				else if (regexNode5.Type == 22)
				{
					num3--;
				}
				else
				{
					flag = false;
					flag2 = false;
				}
			}
			if (num3 < n)
			{
				list2.RemoveRange(num3, n - num3);
			}
		}
	}

	private RegexNode ReduceConcatenation()
	{
		switch (ChildCount())
		{
		case 0:
			return new RegexNode(23, Options);
		case 1:
			return Child(0);
		default:
			ReduceConcatenationWithAdjacentStrings();
			ReduceConcatenationWithAdjacentLoops();
			if ((Options & RegexOptions.RightToLeft) == 0)
			{
				ReduceConcatenationWithAutoAtomic();
			}
			return ReplaceNodeIfUnnecessary(23);
		}
	}

	private void ReduceConcatenationWithAdjacentStrings()
	{
		bool flag = false;
		RegexOptions regexOptions = RegexOptions.None;
		List<RegexNode> list = (List<RegexNode>)Children;
		int num = 0;
		int num2 = 0;
		while (num < list.Count)
		{
			RegexNode regexNode = list[num];
			if (num2 < num)
			{
				list[num2] = regexNode;
			}
			if (regexNode.Type == 25 && (regexNode.Options & RegexOptions.RightToLeft) == (Options & RegexOptions.RightToLeft))
			{
				if (regexNode.Children is List<RegexNode> list2)
				{
					for (int i = 0; i < list2.Count; i++)
					{
						list2[i].Next = this;
					}
					list.InsertRange(num + 1, list2);
				}
				else
				{
					RegexNode regexNode2 = (RegexNode)regexNode.Children;
					regexNode2.Next = this;
					list.Insert(num + 1, regexNode2);
				}
				num2--;
			}
			else if (regexNode.Type == 12 || regexNode.Type == 9)
			{
				RegexOptions regexOptions2 = regexNode.Options & (RegexOptions.IgnoreCase | RegexOptions.RightToLeft);
				if (!flag || regexOptions != regexOptions2)
				{
					flag = true;
					regexOptions = regexOptions2;
				}
				else
				{
					RegexNode regexNode3 = list[--num2];
					if (regexNode3.Type == 9)
					{
						regexNode3.Type = 12;
						regexNode3.Str = regexNode3.Ch.ToString();
					}
					if ((regexOptions2 & RegexOptions.RightToLeft) == 0)
					{
						regexNode3.Str = ((regexNode.Type == 9) ? $"{regexNode3.Str}{regexNode.Ch}" : (regexNode3.Str + regexNode.Str));
					}
					else
					{
						regexNode3.Str = ((regexNode.Type == 9) ? $"{regexNode.Ch}{regexNode3.Str}" : (regexNode.Str + regexNode3.Str));
					}
				}
			}
			else if (regexNode.Type == 23)
			{
				num2--;
			}
			else
			{
				flag = false;
			}
			num++;
			num2++;
		}
		if (num2 < num)
		{
			list.RemoveRange(num2, num - num2);
		}
	}

	private void ReduceConcatenationWithAdjacentLoops()
	{
		List<RegexNode> list = (List<RegexNode>)Children;
		int index = 0;
		int num = 1;
		int num2 = 1;
		while (num < list.Count)
		{
			RegexNode regexNode = list[index];
			RegexNode regexNode2 = list[num];
			if (regexNode.Options == regexNode2.Options)
			{
				switch (regexNode.Type)
				{
				case 3:
					if (regexNode2.Type == 3 && regexNode.Ch == regexNode2.Ch)
					{
						goto IL_01de;
					}
					if (regexNode2.Type != 9 || regexNode.Ch != regexNode2.Ch)
					{
						break;
					}
					goto IL_03b2;
				case 43:
					if (regexNode2.Type == 43 && regexNode.Ch == regexNode2.Ch)
					{
						goto IL_01de;
					}
					if (regexNode2.Type != 9 || regexNode.Ch != regexNode2.Ch)
					{
						break;
					}
					goto IL_03b2;
				case 6:
					if (regexNode2.Type == 6 && regexNode.Ch == regexNode2.Ch)
					{
						goto IL_01de;
					}
					if (regexNode2.Type != 9 || regexNode.Ch != regexNode2.Ch)
					{
						break;
					}
					goto IL_03b2;
				case 4:
					if (regexNode2.Type == 4 && regexNode.Ch == regexNode2.Ch)
					{
						goto IL_01de;
					}
					if (regexNode2.Type != 10 || regexNode.Ch != regexNode2.Ch)
					{
						break;
					}
					goto IL_03b2;
				case 44:
					if (regexNode2.Type == 44 && regexNode.Ch == regexNode2.Ch)
					{
						goto IL_01de;
					}
					if (regexNode2.Type != 10 || regexNode.Ch != regexNode2.Ch)
					{
						break;
					}
					goto IL_03b2;
				case 7:
					if (regexNode2.Type == 7 && regexNode.Ch == regexNode2.Ch)
					{
						goto IL_01de;
					}
					if (regexNode2.Type != 10 || regexNode.Ch != regexNode2.Ch)
					{
						break;
					}
					goto IL_03b2;
				case 5:
					if (regexNode2.Type == 5 && regexNode.Str == regexNode2.Str)
					{
						goto IL_01de;
					}
					if (regexNode2.Type != 11 || !(regexNode.Str == regexNode2.Str))
					{
						break;
					}
					goto IL_03b2;
				case 45:
					if (regexNode2.Type == 45 && regexNode.Str == regexNode2.Str)
					{
						goto IL_01de;
					}
					if (regexNode2.Type != 11 || !(regexNode.Str == regexNode2.Str))
					{
						break;
					}
					goto IL_03b2;
				case 8:
					if (regexNode2.Type == 8 && regexNode.Str == regexNode2.Str)
					{
						goto IL_01de;
					}
					if (regexNode2.Type != 11 || !(regexNode.Str == regexNode2.Str))
					{
						break;
					}
					goto IL_03b2;
				case 9:
					if ((regexNode2.Type == 3 || regexNode2.Type == 43 || regexNode2.Type == 6) && regexNode.Ch == regexNode2.Ch)
					{
						goto IL_04b1;
					}
					if (regexNode2.Type != 9 || regexNode.Ch != regexNode2.Ch)
					{
						break;
					}
					goto IL_0571;
				case 10:
					if ((regexNode2.Type == 4 || regexNode2.Type == 44 || regexNode2.Type == 7) && regexNode.Ch == regexNode2.Ch)
					{
						goto IL_04b1;
					}
					if (regexNode2.Type != 10 || regexNode.Ch != regexNode2.Ch)
					{
						break;
					}
					goto IL_0571;
				case 11:
					{
						if ((regexNode2.Type == 5 || regexNode2.Type == 45 || regexNode2.Type == 8) && regexNode.Str == regexNode2.Str)
						{
							goto IL_04b1;
						}
						if (regexNode2.Type != 11 || !(regexNode.Str == regexNode2.Str))
						{
							break;
						}
						goto IL_0571;
					}
					IL_0571:
					regexNode.MakeRep(3, 2, 2);
					num++;
					continue;
					IL_04b1:
					if (CanCombineCounts(1, 1, regexNode2.M, regexNode2.N))
					{
						regexNode.Type = regexNode2.Type;
						regexNode.M = regexNode2.M + 1;
						regexNode.N = ((regexNode2.N == int.MaxValue) ? int.MaxValue : (regexNode2.N + 1));
						num++;
						continue;
					}
					break;
					IL_03b2:
					if (CanCombineCounts(regexNode.M, regexNode.N, 1, 1))
					{
						regexNode.M++;
						if (regexNode.N != int.MaxValue)
						{
							regexNode.N++;
						}
						num++;
						continue;
					}
					break;
					IL_01de:
					if (CanCombineCounts(regexNode.M, regexNode.N, regexNode2.M, regexNode2.N))
					{
						regexNode.M += regexNode2.M;
						if (regexNode.N != int.MaxValue)
						{
							regexNode.N = ((regexNode2.N == int.MaxValue) ? int.MaxValue : (regexNode.N + regexNode2.N));
						}
						num++;
						continue;
					}
					break;
				}
			}
			list[num2++] = list[num];
			index = num;
			num++;
		}
		if (num2 < list.Count)
		{
			list.RemoveRange(num2, list.Count - num2);
		}
		static bool CanCombineCounts(int nodeMin, int nodeMax, int nextMin, int nextMax)
		{
			if (nodeMin == int.MaxValue || nextMin == int.MaxValue || (uint)(nodeMin + nextMin) >= 2147483647u)
			{
				return false;
			}
			if (nodeMax != int.MaxValue && nextMax != int.MaxValue && (uint)(nodeMax + nextMax) >= 2147483647u)
			{
				return false;
			}
			return true;
		}
	}

	private void ReduceConcatenationWithAutoAtomic()
	{
		List<RegexNode> list = (List<RegexNode>)Children;
		for (int i = 0; i < list.Count - 1; i++)
		{
			ProcessNode(list[i], list[i + 1], 20u);
		}
		static void ProcessNode(RegexNode node, RegexNode subsequent, uint maxDepth)
		{
			while (true)
			{
				if (node.Type == 28 || node.Type == 25)
				{
					node = node.Child(node.ChildCount() - 1);
				}
				else
				{
					if (node.Type != 26)
					{
						break;
					}
					RegexNode regexNode = FindLastExpressionInLoopForAutoAtomic(node, maxDepth - 1);
					if (regexNode == null)
					{
						break;
					}
					node = regexNode;
				}
			}
			switch (node.Type)
			{
			case 3:
				if (!CanBeMadeAtomic(node, subsequent, maxDepth - 1))
				{
					break;
				}
				goto IL_0089;
			case 4:
				if (!CanBeMadeAtomic(node, subsequent, maxDepth - 1))
				{
					break;
				}
				goto IL_0089;
			case 5:
				if (!CanBeMadeAtomic(node, subsequent, maxDepth - 1))
				{
					break;
				}
				goto IL_0089;
			case 24:
				{
					int num = node.ChildCount();
					for (int j = 0; j < num; j++)
					{
						ProcessNode(node.Child(j), subsequent, maxDepth - 1);
					}
					break;
				}
				IL_0089:
				node.MakeLoopAtomic();
				break;
			}
		}
	}

	private static RegexNode FindLastExpressionInLoopForAutoAtomic(RegexNode node, uint maxDepth)
	{
		node = node.Child(0);
		while (node.Type == 28)
		{
			node = node.Child(0);
		}
		if (node.Type == 25)
		{
			int num = node.ChildCount();
			RegexNode regexNode = node.Child(num - 1);
			if (CanBeMadeAtomic(regexNode, node.Child(0), maxDepth - 1))
			{
				return regexNode;
			}
		}
		return null;
	}

	private static bool CanBeMadeAtomic(RegexNode node, RegexNode subsequent, uint maxDepth)
	{
		if (maxDepth == 0)
		{
			return false;
		}
		for (; subsequent.ChildCount() > 0; subsequent = subsequent.Child(0))
		{
			switch (subsequent.Type)
			{
			case 30:
				if ((subsequent.Options & RegexOptions.RightToLeft) == 0)
				{
					continue;
				}
				break;
			case 26:
				if (subsequent.M > 0)
				{
					continue;
				}
				break;
			case 27:
				if (subsequent.M > 0)
				{
					continue;
				}
				break;
			case 25:
			case 28:
			case 32:
				continue;
			}
			break;
		}
		if (node.Options != subsequent.Options)
		{
			return false;
		}
		if (subsequent.Type == 24)
		{
			int num = subsequent.ChildCount();
			for (int i = 0; i < num; i++)
			{
				if (!CanBeMadeAtomic(node, subsequent.Child(i), maxDepth - 1))
				{
					return false;
				}
			}
			return true;
		}
		switch (node.Type)
		{
		case 3:
			switch (subsequent.Type)
			{
			case 9:
				if (node.Ch == subsequent.Ch)
				{
					break;
				}
				goto case 21;
			case 6:
				if (subsequent.M <= 0 || node.Ch == subsequent.Ch)
				{
					break;
				}
				goto case 21;
			case 3:
				if (subsequent.M <= 0 || node.Ch == subsequent.Ch)
				{
					break;
				}
				goto case 21;
			case 43:
				if (subsequent.M <= 0 || node.Ch == subsequent.Ch)
				{
					break;
				}
				goto case 21;
			case 10:
				if (node.Ch != subsequent.Ch)
				{
					break;
				}
				goto case 21;
			case 7:
				if (subsequent.M <= 0 || node.Ch != subsequent.Ch)
				{
					break;
				}
				goto case 21;
			case 4:
				if (subsequent.M <= 0 || node.Ch != subsequent.Ch)
				{
					break;
				}
				goto case 21;
			case 44:
				if (subsequent.M <= 0 || node.Ch != subsequent.Ch)
				{
					break;
				}
				goto case 21;
			case 12:
				if (node.Ch == subsequent.Str[0])
				{
					break;
				}
				goto case 21;
			case 11:
				if (RegexCharClass.CharInClass(node.Ch, subsequent.Str))
				{
					break;
				}
				goto case 21;
			case 8:
				if (subsequent.M <= 0 || RegexCharClass.CharInClass(node.Ch, subsequent.Str))
				{
					break;
				}
				goto case 21;
			case 5:
				if (subsequent.M <= 0 || RegexCharClass.CharInClass(node.Ch, subsequent.Str))
				{
					break;
				}
				goto case 21;
			case 45:
				if (subsequent.M <= 0 || RegexCharClass.CharInClass(node.Ch, subsequent.Str))
				{
					break;
				}
				goto case 21;
			case 20:
				if (node.Ch == '\n')
				{
					break;
				}
				goto case 21;
			case 15:
				if (node.Ch == '\n')
				{
					break;
				}
				goto case 21;
			case 16:
				if (!RegexCharClass.IsWordChar(node.Ch))
				{
					break;
				}
				goto case 21;
			case 17:
				if (RegexCharClass.IsWordChar(node.Ch))
				{
					break;
				}
				goto case 21;
			case 41:
				if (!RegexCharClass.IsECMAWordChar(node.Ch))
				{
					break;
				}
				goto case 21;
			case 42:
				if (RegexCharClass.IsECMAWordChar(node.Ch))
				{
					break;
				}
				goto case 21;
			case 21:
				return true;
			}
			break;
		case 4:
		{
			int type = subsequent.Type;
			if (type <= 9)
			{
				if (type != 3)
				{
					if (type != 6)
					{
						if (type != 9 || node.Ch != subsequent.Ch)
						{
							break;
						}
					}
					else if (subsequent.M <= 0 || node.Ch != subsequent.Ch)
					{
						break;
					}
				}
				else if (subsequent.M <= 0 || node.Ch != subsequent.Ch)
				{
					break;
				}
			}
			else if (type != 12)
			{
				if (type != 21 && (type != 43 || subsequent.M <= 0 || node.Ch != subsequent.Ch))
				{
					break;
				}
			}
			else if (node.Ch != subsequent.Str[0])
			{
				break;
			}
			return true;
		}
		case 5:
			switch (subsequent.Type)
			{
			case 9:
				if (RegexCharClass.CharInClass(subsequent.Ch, node.Str))
				{
					break;
				}
				goto case 21;
			case 6:
				if (subsequent.M <= 0 || RegexCharClass.CharInClass(subsequent.Ch, node.Str))
				{
					break;
				}
				goto case 21;
			case 3:
				if (subsequent.M <= 0 || RegexCharClass.CharInClass(subsequent.Ch, node.Str))
				{
					break;
				}
				goto case 21;
			case 43:
				if (subsequent.M <= 0 || RegexCharClass.CharInClass(subsequent.Ch, node.Str))
				{
					break;
				}
				goto case 21;
			case 12:
				if (RegexCharClass.CharInClass(subsequent.Str[0], node.Str))
				{
					break;
				}
				goto case 21;
			case 11:
				if (RegexCharClass.MayOverlap(node.Str, subsequent.Str))
				{
					break;
				}
				goto case 21;
			case 8:
				if (subsequent.M <= 0 || RegexCharClass.MayOverlap(node.Str, subsequent.Str))
				{
					break;
				}
				goto case 21;
			case 5:
				if (subsequent.M <= 0 || RegexCharClass.MayOverlap(node.Str, subsequent.Str))
				{
					break;
				}
				goto case 21;
			case 45:
				if (subsequent.M <= 0 || RegexCharClass.MayOverlap(node.Str, subsequent.Str))
				{
					break;
				}
				goto case 21;
			case 20:
				if (RegexCharClass.CharInClass('\n', node.Str))
				{
					break;
				}
				goto case 21;
			case 15:
				if (RegexCharClass.CharInClass('\n', node.Str))
				{
					break;
				}
				goto case 21;
			case 16:
				if (!(node.Str == "\0\0\n\0\u0002\u0004\u0005\u0003\u0001\u0006\t\u0013\0") && !(node.Str == "\0\0\u0001\t"))
				{
					break;
				}
				goto case 21;
			case 17:
				if (!(node.Str == "\u0001\0\n\0\u0002\u0004\u0005\u0003\u0001\u0006\t\u0013\0") && !(node.Str == "\0\0\u0001\ufff7"))
				{
					break;
				}
				goto case 21;
			case 41:
				if (!(node.Str == "\0\n\00:A[_`a{İı") && !(node.Str == "\0\u0002\00:"))
				{
					break;
				}
				goto case 21;
			case 42:
				if (!(node.Str == "\u0001\n\00:A[_`a{İı") && !(node.Str == "\0\0\u0001\ufff7"))
				{
					break;
				}
				goto case 21;
			case 21:
				return true;
			}
			break;
		}
		return false;
	}

	public int ComputeMinLength()
	{
		return ComputeMinLength(this, 20u);
		static int ComputeMinLength(RegexNode node, uint maxDepth)
		{
			if (maxDepth == 0)
			{
				return 0;
			}
			switch (node.Type)
			{
			case 9:
			case 10:
			case 11:
				return 1;
			case 12:
				return node.Str.Length;
			case 3:
			case 4:
			case 5:
			case 6:
			case 7:
			case 8:
			case 43:
			case 44:
			case 45:
				return node.M;
			case 26:
			case 27:
				return (int)Math.Min(2147483647L, (long)node.M * (long)ComputeMinLength(node.Child(0), maxDepth - 1));
			case 24:
			{
				int num3 = node.ChildCount();
				int num4 = ComputeMinLength(node.Child(0), maxDepth - 1);
				for (int j = 1; j < num3; j++)
				{
					if (num4 <= 0)
					{
						break;
					}
					num4 = Math.Min(num4, ComputeMinLength(node.Child(j), maxDepth - 1));
				}
				return num4;
			}
			case 25:
			{
				long num = 0L;
				int num2 = node.ChildCount();
				for (int i = 0; i < num2; i++)
				{
					num += ComputeMinLength(node.Child(i), maxDepth - 1);
				}
				return (int)Math.Min(2147483647L, num);
			}
			case 28:
			case 29:
			case 32:
				return ComputeMinLength(node.Child(0), maxDepth - 1);
			default:
				return 0;
			}
		}
	}

	public RegexNode MakeQuantifier(bool lazy, int min, int max)
	{
		if (min == 0 && max == 0)
		{
			return new RegexNode(23, Options);
		}
		if (min == 1 && max == 1)
		{
			return this;
		}
		int type = Type;
		if ((uint)(type - 9) <= 2u)
		{
			MakeRep(lazy ? 6 : 3, min, max);
			return this;
		}
		RegexNode regexNode = new RegexNode(lazy ? 27 : 26, Options, min, max);
		regexNode.AddChild(this);
		return regexNode;
	}

	public void AddChild(RegexNode newChild)
	{
		newChild.Next = this;
		newChild = newChild.Reduce();
		newChild.Next = this;
		if (Children == null)
		{
			Children = newChild;
		}
		else if (Children is RegexNode item)
		{
			Children = new List<RegexNode> { item, newChild };
		}
		else
		{
			((List<RegexNode>)Children).Add(newChild);
		}
	}

	public void InsertChild(int index, RegexNode newChild)
	{
		newChild.Next = this;
		newChild = newChild.Reduce();
		newChild.Next = this;
		((List<RegexNode>)Children).Insert(index, newChild);
	}

	public void ReplaceChild(int index, RegexNode newChild)
	{
		newChild.Next = this;
		if (Children is RegexNode)
		{
			Children = newChild;
		}
		else
		{
			((List<RegexNode>)Children)[index] = newChild;
		}
	}

	public RegexNode Child(int i)
	{
		if (Children is RegexNode result)
		{
			return result;
		}
		return ((List<RegexNode>)Children)[i];
	}

	public int ChildCount()
	{
		if (Children == null)
		{
			return 0;
		}
		if (Children is List<RegexNode> list)
		{
			return list.Count;
		}
		return 1;
	}
}
