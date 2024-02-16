using System.Buffers;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Text.Unicode;

internal static class TextSegmentationUtility
{
	private delegate OperationStatus DecodeFirstRune<T>(ReadOnlySpan<T> input, out Rune rune, out int elementsConsumed);

	[StructLayout(LayoutKind.Auto)]
	private ref struct Processor<T>
	{
		private readonly ReadOnlySpan<T> _buffer;

		private readonly DecodeFirstRune<T> _decoder;

		private int _codeUnitLengthOfCurrentScalar;

		public int CurrentCodeUnitOffset { get; private set; }

		public GraphemeClusterBreakType CurrentType { get; private set; }

		internal Processor(ReadOnlySpan<T> buffer, DecodeFirstRune<T> decoder)
		{
			_buffer = buffer;
			_decoder = decoder;
			_codeUnitLengthOfCurrentScalar = 0;
			CurrentType = GraphemeClusterBreakType.Other;
			CurrentCodeUnitOffset = 0;
		}

		public void MoveNext()
		{
			CurrentCodeUnitOffset += _codeUnitLengthOfCurrentScalar;
			_decoder(_buffer.Slice(CurrentCodeUnitOffset), out var rune, out _codeUnitLengthOfCurrentScalar);
			CurrentType = CharUnicodeInfo.GetGraphemeClusterBreakType(rune);
		}
	}

	private static readonly DecodeFirstRune<char> _utf16Decoder = Rune.DecodeFromUtf16;

	private static int GetLengthOfFirstExtendedGraphemeCluster<T>(ReadOnlySpan<T> input, DecodeFirstRune<T> decoder)
	{
		Processor<T> processor = new Processor<T>(input, decoder);
		processor.MoveNext();
		while (processor.CurrentType == GraphemeClusterBreakType.Prepend)
		{
			processor.MoveNext();
		}
		if (processor.CurrentCodeUnitOffset <= 0 || (processor.CurrentType != GraphemeClusterBreakType.Control && processor.CurrentType != GraphemeClusterBreakType.CR && processor.CurrentType != GraphemeClusterBreakType.LF))
		{
			GraphemeClusterBreakType currentType = processor.CurrentType;
			processor.MoveNext();
			switch (currentType)
			{
			case GraphemeClusterBreakType.CR:
				if (processor.CurrentType == GraphemeClusterBreakType.LF)
				{
					processor.MoveNext();
				}
				break;
			case GraphemeClusterBreakType.L:
				while (processor.CurrentType == GraphemeClusterBreakType.L)
				{
					processor.MoveNext();
				}
				if (processor.CurrentType == GraphemeClusterBreakType.V)
				{
					processor.MoveNext();
				}
				else
				{
					if (processor.CurrentType != GraphemeClusterBreakType.LV)
					{
						if (processor.CurrentType == GraphemeClusterBreakType.LVT)
						{
							processor.MoveNext();
							goto case GraphemeClusterBreakType.T;
						}
						goto default;
					}
					processor.MoveNext();
				}
				goto case GraphemeClusterBreakType.V;
			case GraphemeClusterBreakType.V:
			case GraphemeClusterBreakType.LV:
				while (processor.CurrentType == GraphemeClusterBreakType.V)
				{
					processor.MoveNext();
				}
				if (processor.CurrentType == GraphemeClusterBreakType.T)
				{
					processor.MoveNext();
					goto case GraphemeClusterBreakType.T;
				}
				goto default;
			case GraphemeClusterBreakType.T:
			case GraphemeClusterBreakType.LVT:
				while (processor.CurrentType == GraphemeClusterBreakType.T)
				{
					processor.MoveNext();
				}
				goto default;
			case GraphemeClusterBreakType.Extended_Pictograph:
				while (true)
				{
					if (processor.CurrentType == GraphemeClusterBreakType.Extend)
					{
						processor.MoveNext();
						continue;
					}
					if (processor.CurrentType != GraphemeClusterBreakType.ZWJ)
					{
						break;
					}
					processor.MoveNext();
					if (processor.CurrentType != GraphemeClusterBreakType.Extended_Pictograph)
					{
						break;
					}
					processor.MoveNext();
				}
				goto default;
			case GraphemeClusterBreakType.Regional_Indicator:
				if (processor.CurrentType == GraphemeClusterBreakType.Regional_Indicator)
				{
					processor.MoveNext();
				}
				goto default;
			default:
				while (processor.CurrentType == GraphemeClusterBreakType.Extend || processor.CurrentType == GraphemeClusterBreakType.ZWJ || processor.CurrentType == GraphemeClusterBreakType.SpacingMark)
				{
					processor.MoveNext();
				}
				break;
			case GraphemeClusterBreakType.LF:
			case GraphemeClusterBreakType.Control:
				break;
			}
		}
		return processor.CurrentCodeUnitOffset;
	}

	public static int GetLengthOfFirstUtf16ExtendedGraphemeCluster(ReadOnlySpan<char> input)
	{
		return GetLengthOfFirstExtendedGraphemeCluster(input, _utf16Decoder);
	}
}
