using System.Diagnostics.CodeAnalysis;

namespace System.Text;

public sealed class DecoderReplacementFallback : DecoderFallback
{
	internal static readonly DecoderReplacementFallback s_default = new DecoderReplacementFallback();

	private readonly string _strDefault;

	public string DefaultString => _strDefault;

	public override int MaxCharCount => _strDefault.Length;

	public DecoderReplacementFallback()
		: this("?")
	{
	}

	public DecoderReplacementFallback(string replacement)
	{
		if (replacement == null)
		{
			throw new ArgumentNullException("replacement");
		}
		bool flag = false;
		foreach (char c in replacement)
		{
			if (char.IsSurrogate(c))
			{
				if (char.IsHighSurrogate(c))
				{
					if (flag)
					{
						break;
					}
					flag = true;
					continue;
				}
				if (!flag)
				{
					flag = true;
					break;
				}
				flag = false;
			}
			else if (flag)
			{
				break;
			}
		}
		if (flag)
		{
			throw new ArgumentException(SR.Format(SR.Argument_InvalidCharSequenceNoIndex, "replacement"));
		}
		_strDefault = replacement;
	}

	public override DecoderFallbackBuffer CreateFallbackBuffer()
	{
		return new DecoderReplacementFallbackBuffer(this);
	}

	public override bool Equals([NotNullWhen(true)] object? value)
	{
		if (value is DecoderReplacementFallback decoderReplacementFallback)
		{
			return _strDefault == decoderReplacementFallback._strDefault;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return _strDefault.GetHashCode();
	}
}
