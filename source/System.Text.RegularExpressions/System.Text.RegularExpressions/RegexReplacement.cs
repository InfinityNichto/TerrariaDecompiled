using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace System.Text.RegularExpressions;

internal sealed class RegexReplacement
{
	private struct FourStackStrings
	{
		public string Item1;

		public string Item2;

		public string Item3;

		public string Item4;
	}

	private readonly string[] _strings;

	private readonly int[] _rules;

	public string Pattern { get; }

	public RegexReplacement(string rep, RegexNode concat, Hashtable _caps)
	{
		if (concat.Type != 25)
		{
			throw ThrowHelper.CreateArgumentException(ExceptionResource.ReplacementError);
		}
		Span<char> initialBuffer = stackalloc char[256];
		System.Text.ValueStringBuilder valueStringBuilder = new System.Text.ValueStringBuilder(initialBuffer);
		FourStackStrings fourStackStrings = default(FourStackStrings);
		System.Collections.Generic.ValueListBuilder<string> valueListBuilder = new System.Collections.Generic.ValueListBuilder<string>(MemoryMarshal.CreateSpan(ref fourStackStrings.Item1, 4));
		Span<int> initialSpan = stackalloc int[64];
		System.Collections.Generic.ValueListBuilder<int> valueListBuilder2 = new System.Collections.Generic.ValueListBuilder<int>(initialSpan);
		int num = concat.ChildCount();
		for (int i = 0; i < num; i++)
		{
			RegexNode regexNode = concat.Child(i);
			switch (regexNode.Type)
			{
			case 12:
				valueStringBuilder.Append(regexNode.Str);
				break;
			case 9:
				valueStringBuilder.Append(regexNode.Ch);
				break;
			case 13:
			{
				if (valueStringBuilder.Length > 0)
				{
					valueListBuilder2.Append(valueListBuilder.Length);
					valueListBuilder.Append(valueStringBuilder.ToString());
					valueStringBuilder = new System.Text.ValueStringBuilder(initialBuffer);
				}
				int num2 = regexNode.M;
				if (_caps != null && num2 >= 0)
				{
					num2 = (int)_caps[num2];
				}
				valueListBuilder2.Append(-5 - num2);
				break;
			}
			default:
				throw ThrowHelper.CreateArgumentException(ExceptionResource.ReplacementError);
			}
		}
		if (valueStringBuilder.Length > 0)
		{
			valueListBuilder2.Append(valueListBuilder.Length);
			valueListBuilder.Append(valueStringBuilder.ToString());
		}
		Pattern = rep;
		_strings = valueListBuilder.AsSpan().ToArray();
		_rules = valueListBuilder2.AsSpan().ToArray();
		valueListBuilder2.Dispose();
	}

	public static RegexReplacement GetOrCreate(WeakReference<RegexReplacement> replRef, string replacement, Hashtable caps, int capsize, Hashtable capnames, RegexOptions roptions)
	{
		if (!replRef.TryGetTarget(out var target) || !target.Pattern.Equals(replacement))
		{
			target = RegexParser.ParseReplacement(replacement, roptions, caps, capsize, capnames);
			replRef.SetTarget(target);
		}
		return target;
	}

	public void ReplacementImpl(ref SegmentStringBuilder segments, Match match)
	{
		int[] rules = _rules;
		foreach (int num in rules)
		{
			ReadOnlyMemory<char> readOnlyMemory;
			if (num >= 0)
			{
				readOnlyMemory = _strings[num].AsMemory();
			}
			else
			{
				ReadOnlyMemory<char> readOnlyMemory2 = ((num >= -4) ? ((-5 - num) switch
				{
					-1 => match.GetLeftSubstring(), 
					-2 => match.GetRightSubstring(), 
					-3 => match.LastGroupToStringImpl(), 
					-4 => match.Text.AsMemory(), 
					_ => default(ReadOnlyMemory<char>), 
				}) : match.GroupToStringImpl(-5 - num));
				readOnlyMemory = readOnlyMemory2;
			}
			ReadOnlyMemory<char> segment = readOnlyMemory;
			if (segment.Length != 0)
			{
				segments.Add(segment);
			}
		}
	}

	public void ReplacementImplRTL(ref SegmentStringBuilder segments, Match match)
	{
		for (int num = _rules.Length - 1; num >= 0; num--)
		{
			int num2 = _rules[num];
			ReadOnlyMemory<char> readOnlyMemory;
			if (num2 >= 0)
			{
				readOnlyMemory = _strings[num2].AsMemory();
			}
			else
			{
				ReadOnlyMemory<char> readOnlyMemory2 = ((num2 >= -4) ? ((-5 - num2) switch
				{
					-1 => match.GetLeftSubstring(), 
					-2 => match.GetRightSubstring(), 
					-3 => match.LastGroupToStringImpl(), 
					-4 => match.Text.AsMemory(), 
					_ => default(ReadOnlyMemory<char>), 
				}) : match.GroupToStringImpl(-5 - num2));
				readOnlyMemory = readOnlyMemory2;
			}
			ReadOnlyMemory<char> segment = readOnlyMemory;
			if (segment.Length != 0)
			{
				segments.Add(segment);
			}
		}
	}

	public string Replace(Regex regex, string input, int count, int startat)
	{
		if (count < -1)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.CountTooSmall);
		}
		if ((uint)startat > (uint)input.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startat, ExceptionResource.BeginIndexNotNegative);
		}
		if (count == 0)
		{
			return input;
		}
		(RegexReplacement, SegmentStringBuilder, ReadOnlyMemory<char>, int, int) state2 = (this, SegmentStringBuilder.Create(), input.AsMemory(), 0, count);
		if (!regex.RightToLeft)
		{
			regex.Run<(RegexReplacement, SegmentStringBuilder, ReadOnlyMemory<char>, int, int)>(input, startat, ref state2, delegate(ref (RegexReplacement thisRef, SegmentStringBuilder segments, ReadOnlyMemory<char> inputMemory, int prevat, int count) state, Match match)
			{
				state.segments.Add(state.inputMemory.Slice(state.prevat, match.Index - state.prevat));
				state.prevat = match.Index + match.Length;
				state.thisRef.ReplacementImpl(ref state.segments, match);
				return --state.count != 0;
			}, reuseMatchObject: true);
			if (state2.Item2.Count == 0)
			{
				return input;
			}
			state2.Item2.Add(state2.Item3.Slice(state2.Item4, input.Length - state2.Item4));
		}
		else
		{
			state2.Item4 = input.Length;
			regex.Run<(RegexReplacement, SegmentStringBuilder, ReadOnlyMemory<char>, int, int)>(input, startat, ref state2, delegate(ref (RegexReplacement thisRef, SegmentStringBuilder segments, ReadOnlyMemory<char> inputMemory, int prevat, int count) state, Match match)
			{
				state.segments.Add(state.inputMemory.Slice(match.Index + match.Length, state.prevat - match.Index - match.Length));
				state.prevat = match.Index;
				state.thisRef.ReplacementImplRTL(ref state.segments, match);
				return --state.count != 0;
			}, reuseMatchObject: true);
			if (state2.Item2.Count == 0)
			{
				return input;
			}
			state2.Item2.Add(state2.Item3.Slice(0, state2.Item4));
			state2.Item2.AsSpan().Reverse();
		}
		return state2.Item2.ToString();
	}
}
