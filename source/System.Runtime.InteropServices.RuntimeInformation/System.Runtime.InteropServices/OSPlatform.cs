using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.InteropServices;

public readonly struct OSPlatform : IEquatable<OSPlatform>
{
	public static OSPlatform FreeBSD { get; } = new OSPlatform("FREEBSD");


	public static OSPlatform Linux { get; } = new OSPlatform("LINUX");


	public static OSPlatform OSX { get; } = new OSPlatform("OSX");


	public static OSPlatform Windows { get; } = new OSPlatform("WINDOWS");


	internal string Name { get; }

	private OSPlatform(string osPlatform)
	{
		if (osPlatform == null)
		{
			throw new ArgumentNullException("osPlatform");
		}
		if (osPlatform.Length == 0)
		{
			throw new ArgumentException(System.SR.Argument_EmptyValue, "osPlatform");
		}
		Name = osPlatform;
	}

	public static OSPlatform Create(string osPlatform)
	{
		return new OSPlatform(osPlatform);
	}

	public bool Equals(OSPlatform other)
	{
		return Equals(other.Name);
	}

	internal bool Equals(string other)
	{
		return string.Equals(Name, other, StringComparison.OrdinalIgnoreCase);
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is OSPlatform other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		if (Name != null)
		{
			return StringComparer.OrdinalIgnoreCase.GetHashCode(Name);
		}
		return 0;
	}

	public override string ToString()
	{
		return Name ?? string.Empty;
	}

	public static bool operator ==(OSPlatform left, OSPlatform right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(OSPlatform left, OSPlatform right)
	{
		return !(left == right);
	}
}
