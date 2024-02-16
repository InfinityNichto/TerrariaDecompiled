using System.Diagnostics.CodeAnalysis;

namespace System.Xml.Xsl;

internal struct Int32Pair
{
	private readonly int _left;

	private readonly int _right;

	public int Left => _left;

	public int Right => _right;

	public Int32Pair(int left, int right)
	{
		_left = left;
		_right = right;
	}

	public override bool Equals([NotNullWhen(true)] object other)
	{
		if (other is Int32Pair int32Pair)
		{
			if (_left == int32Pair._left)
			{
				return _right == int32Pair._right;
			}
			return false;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return _left.GetHashCode() ^ _right.GetHashCode();
	}
}
