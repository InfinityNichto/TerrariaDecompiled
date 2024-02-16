using System.Diagnostics.CodeAnalysis;

namespace System;

public readonly struct ConsoleKeyInfo : IEquatable<ConsoleKeyInfo>
{
	private readonly char _keyChar;

	private readonly ConsoleKey _key;

	private readonly ConsoleModifiers _mods;

	public char KeyChar => _keyChar;

	public ConsoleKey Key => _key;

	public ConsoleModifiers Modifiers => _mods;

	public ConsoleKeyInfo(char keyChar, ConsoleKey key, bool shift, bool alt, bool control)
	{
		if (key < (ConsoleKey)0 || key > (ConsoleKey)255)
		{
			throw new ArgumentOutOfRangeException("key", System.SR.ArgumentOutOfRange_ConsoleKey);
		}
		_keyChar = keyChar;
		_key = key;
		_mods = (ConsoleModifiers)0;
		if (shift)
		{
			_mods |= ConsoleModifiers.Shift;
		}
		if (alt)
		{
			_mods |= ConsoleModifiers.Alt;
		}
		if (control)
		{
			_mods |= ConsoleModifiers.Control;
		}
	}

	public override bool Equals([NotNullWhen(true)] object? value)
	{
		if (value is ConsoleKeyInfo obj)
		{
			return Equals(obj);
		}
		return false;
	}

	public bool Equals(ConsoleKeyInfo obj)
	{
		if (obj._keyChar == _keyChar && obj._key == _key)
		{
			return obj._mods == _mods;
		}
		return false;
	}

	public static bool operator ==(ConsoleKeyInfo a, ConsoleKeyInfo b)
	{
		return a.Equals(b);
	}

	public static bool operator !=(ConsoleKeyInfo a, ConsoleKeyInfo b)
	{
		return !(a == b);
	}

	public override int GetHashCode()
	{
		return _keyChar | ((int)_key << 16) | ((int)_mods << 24);
	}
}
