namespace System.Text.Unicode;

public sealed class UnicodeRange
{
	public int FirstCodePoint { get; private set; }

	public int Length { get; private set; }

	public UnicodeRange(int firstCodePoint, int length)
	{
		if (firstCodePoint < 0 || firstCodePoint > 65535)
		{
			throw new ArgumentOutOfRangeException("firstCodePoint");
		}
		if (length < 0 || (long)firstCodePoint + (long)length > 65536)
		{
			throw new ArgumentOutOfRangeException("length");
		}
		FirstCodePoint = firstCodePoint;
		Length = length;
	}

	public static UnicodeRange Create(char firstCharacter, char lastCharacter)
	{
		if (lastCharacter < firstCharacter)
		{
			throw new ArgumentOutOfRangeException("lastCharacter");
		}
		return new UnicodeRange(firstCharacter, 1 + (lastCharacter - firstCharacter));
	}
}
