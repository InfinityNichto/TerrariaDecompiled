using System.Diagnostics.CodeAnalysis;

namespace System.Threading;

public struct LockCookie
{
	internal LockCookieFlags _flags;

	internal ushort _readerLevel;

	internal ushort _writerLevel;

	internal int _threadID;

	public override int GetHashCode()
	{
		return (int)(_flags + _readerLevel + _writerLevel + _threadID);
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is LockCookie)
		{
			return Equals((LockCookie)obj);
		}
		return false;
	}

	public bool Equals(LockCookie obj)
	{
		if (_flags == obj._flags && _readerLevel == obj._readerLevel && _writerLevel == obj._writerLevel)
		{
			return _threadID == obj._threadID;
		}
		return false;
	}

	public static bool operator ==(LockCookie a, LockCookie b)
	{
		return a.Equals(b);
	}

	public static bool operator !=(LockCookie a, LockCookie b)
	{
		return !(a == b);
	}
}
