using System.Diagnostics.CodeAnalysis;

namespace System.Diagnostics.SymbolStore;

public readonly struct SymbolToken
{
	private readonly int _token;

	public SymbolToken(int val)
	{
		_token = val;
	}

	public int GetToken()
	{
		return _token;
	}

	public override int GetHashCode()
	{
		return _token;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is SymbolToken)
		{
			return Equals((SymbolToken)obj);
		}
		return false;
	}

	public bool Equals(SymbolToken obj)
	{
		return obj._token == _token;
	}

	public static bool operator ==(SymbolToken a, SymbolToken b)
	{
		return a.Equals(b);
	}

	public static bool operator !=(SymbolToken a, SymbolToken b)
	{
		return !(a == b);
	}
}
