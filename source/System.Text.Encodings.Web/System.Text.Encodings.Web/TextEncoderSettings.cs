using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Unicode;

namespace System.Text.Encodings.Web;

public class TextEncoderSettings
{
	private AllowedBmpCodePointsBitmap _allowedCodePointsBitmap;

	public TextEncoderSettings()
	{
	}

	public TextEncoderSettings(TextEncoderSettings other)
	{
		if (other == null)
		{
			throw new ArgumentNullException("other");
		}
		_allowedCodePointsBitmap = other.GetAllowedCodePointsBitmap();
	}

	public TextEncoderSettings(params UnicodeRange[] allowedRanges)
	{
		if (allowedRanges == null)
		{
			throw new ArgumentNullException("allowedRanges");
		}
		AllowRanges(allowedRanges);
	}

	public virtual void AllowCharacter(char character)
	{
		_allowedCodePointsBitmap.AllowChar(character);
	}

	public virtual void AllowCharacters(params char[] characters)
	{
		if (characters == null)
		{
			throw new ArgumentNullException("characters");
		}
		for (int i = 0; i < characters.Length; i++)
		{
			_allowedCodePointsBitmap.AllowChar(characters[i]);
		}
	}

	public virtual void AllowCodePoints(IEnumerable<int> codePoints)
	{
		if (codePoints == null)
		{
			throw new ArgumentNullException("codePoints");
		}
		foreach (int codePoint in codePoints)
		{
			if (System.Text.UnicodeUtility.IsBmpCodePoint((uint)codePoint))
			{
				_allowedCodePointsBitmap.AllowChar((char)codePoint);
			}
		}
	}

	public virtual void AllowRange(UnicodeRange range)
	{
		if (range == null)
		{
			throw new ArgumentNullException("range");
		}
		int firstCodePoint = range.FirstCodePoint;
		int length = range.Length;
		for (int i = 0; i < length; i++)
		{
			int num = firstCodePoint + i;
			_allowedCodePointsBitmap.AllowChar((char)num);
		}
	}

	public virtual void AllowRanges(params UnicodeRange[] ranges)
	{
		if (ranges == null)
		{
			throw new ArgumentNullException("ranges");
		}
		for (int i = 0; i < ranges.Length; i++)
		{
			AllowRange(ranges[i]);
		}
	}

	public virtual void Clear()
	{
		_allowedCodePointsBitmap = default(AllowedBmpCodePointsBitmap);
	}

	public virtual void ForbidCharacter(char character)
	{
		_allowedCodePointsBitmap.ForbidChar(character);
	}

	public virtual void ForbidCharacters(params char[] characters)
	{
		if (characters == null)
		{
			throw new ArgumentNullException("characters");
		}
		for (int i = 0; i < characters.Length; i++)
		{
			_allowedCodePointsBitmap.ForbidChar(characters[i]);
		}
	}

	public virtual void ForbidRange(UnicodeRange range)
	{
		if (range == null)
		{
			throw new ArgumentNullException("range");
		}
		int firstCodePoint = range.FirstCodePoint;
		int length = range.Length;
		for (int i = 0; i < length; i++)
		{
			int num = firstCodePoint + i;
			_allowedCodePointsBitmap.ForbidChar((char)num);
		}
	}

	public virtual void ForbidRanges(params UnicodeRange[] ranges)
	{
		if (ranges == null)
		{
			throw new ArgumentNullException("ranges");
		}
		for (int i = 0; i < ranges.Length; i++)
		{
			ForbidRange(ranges[i]);
		}
	}

	public virtual IEnumerable<int> GetAllowedCodePoints()
	{
		for (int i = 0; i <= 65535; i++)
		{
			if (_allowedCodePointsBitmap.IsCharAllowed((char)i))
			{
				yield return i;
			}
		}
	}

	internal ref readonly AllowedBmpCodePointsBitmap GetAllowedCodePointsBitmap()
	{
		if (GetType() == typeof(TextEncoderSettings))
		{
			return ref _allowedCodePointsBitmap;
		}
		StrongBox<AllowedBmpCodePointsBitmap> strongBox = new StrongBox<AllowedBmpCodePointsBitmap>();
		foreach (int allowedCodePoint in GetAllowedCodePoints())
		{
			if ((uint)allowedCodePoint <= 65535u)
			{
				strongBox.Value.AllowChar((char)allowedCodePoint);
			}
		}
		return ref strongBox.Value;
	}
}
