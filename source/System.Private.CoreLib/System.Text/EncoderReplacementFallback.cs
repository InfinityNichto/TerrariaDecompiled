using System.Diagnostics.CodeAnalysis;

namespace System.Text;

public sealed class EncoderReplacementFallback : EncoderFallback
{
	internal static readonly EncoderReplacementFallback s_default = new EncoderReplacementFallback();

	private readonly string _strDefault;

	public string DefaultString => _strDefault;

	public override int MaxCharCount => _strDefault.Length;

	public EncoderReplacementFallback()
		: this("?")
	{
	}

	public EncoderReplacementFallback(string replacement)
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

	public override EncoderFallbackBuffer CreateFallbackBuffer()
	{
		return new EncoderReplacementFallbackBuffer(this);
	}

	public override bool Equals([NotNullWhen(true)] object? value)
	{
		if (value is EncoderReplacementFallback encoderReplacementFallback)
		{
			return _strDefault == encoderReplacementFallback._strDefault;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return _strDefault.GetHashCode();
	}
}
