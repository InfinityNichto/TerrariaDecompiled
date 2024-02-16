namespace System.Text.RegularExpressions;

public class Capture
{
	public int Index { get; private protected set; }

	public int Length { get; private protected set; }

	internal string Text { get; set; }

	public string Value => Text.Substring(Index, Length);

	public ReadOnlySpan<char> ValueSpan => Text.AsSpan(Index, Length);

	internal Capture(string text, int index, int length)
	{
		Text = text;
		Index = index;
		Length = length;
	}

	public override string ToString()
	{
		return Value;
	}

	internal ReadOnlyMemory<char> GetLeftSubstring()
	{
		return Text.AsMemory(0, Index);
	}

	internal ReadOnlyMemory<char> GetRightSubstring()
	{
		return Text.AsMemory(Index + Length, Text.Length - Index - Length);
	}
}
