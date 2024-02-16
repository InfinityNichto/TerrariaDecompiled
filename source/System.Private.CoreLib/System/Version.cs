using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public sealed class Version : ICloneable, IComparable, IComparable<Version?>, IEquatable<Version?>, ISpanFormattable, IFormattable
{
	private readonly int _Major;

	private readonly int _Minor;

	private readonly int _Build;

	private readonly int _Revision;

	public int Major => _Major;

	public int Minor => _Minor;

	public int Build => _Build;

	public int Revision => _Revision;

	public short MajorRevision => (short)(_Revision >> 16);

	public short MinorRevision => (short)(_Revision & 0xFFFF);

	private int DefaultFormatFieldCount
	{
		get
		{
			if (_Build != -1)
			{
				if (_Revision != -1)
				{
					return 4;
				}
				return 3;
			}
			return 2;
		}
	}

	public Version(int major, int minor, int build, int revision)
	{
		if (major < 0)
		{
			throw new ArgumentOutOfRangeException("major", SR.ArgumentOutOfRange_Version);
		}
		if (minor < 0)
		{
			throw new ArgumentOutOfRangeException("minor", SR.ArgumentOutOfRange_Version);
		}
		if (build < 0)
		{
			throw new ArgumentOutOfRangeException("build", SR.ArgumentOutOfRange_Version);
		}
		if (revision < 0)
		{
			throw new ArgumentOutOfRangeException("revision", SR.ArgumentOutOfRange_Version);
		}
		_Major = major;
		_Minor = minor;
		_Build = build;
		_Revision = revision;
	}

	public Version(int major, int minor, int build)
	{
		if (major < 0)
		{
			throw new ArgumentOutOfRangeException("major", SR.ArgumentOutOfRange_Version);
		}
		if (minor < 0)
		{
			throw new ArgumentOutOfRangeException("minor", SR.ArgumentOutOfRange_Version);
		}
		if (build < 0)
		{
			throw new ArgumentOutOfRangeException("build", SR.ArgumentOutOfRange_Version);
		}
		_Major = major;
		_Minor = minor;
		_Build = build;
		_Revision = -1;
	}

	public Version(int major, int minor)
	{
		if (major < 0)
		{
			throw new ArgumentOutOfRangeException("major", SR.ArgumentOutOfRange_Version);
		}
		if (minor < 0)
		{
			throw new ArgumentOutOfRangeException("minor", SR.ArgumentOutOfRange_Version);
		}
		_Major = major;
		_Minor = minor;
		_Build = -1;
		_Revision = -1;
	}

	public Version(string version)
	{
		Version version2 = Parse(version);
		_Major = version2.Major;
		_Minor = version2.Minor;
		_Build = version2.Build;
		_Revision = version2.Revision;
	}

	public Version()
	{
		_Build = -1;
		_Revision = -1;
	}

	private Version(Version version)
	{
		_Major = version._Major;
		_Minor = version._Minor;
		_Build = version._Build;
		_Revision = version._Revision;
	}

	public object Clone()
	{
		return new Version(this);
	}

	public int CompareTo(object? version)
	{
		if (version == null)
		{
			return 1;
		}
		if (version is Version value)
		{
			return CompareTo(value);
		}
		throw new ArgumentException(SR.Arg_MustBeVersion);
	}

	public int CompareTo(Version? value)
	{
		if ((object)value != this)
		{
			if ((object)value != null)
			{
				if (_Major == value._Major)
				{
					if (_Minor == value._Minor)
					{
						if (_Build == value._Build)
						{
							if (_Revision == value._Revision)
							{
								return 0;
							}
							if (_Revision <= value._Revision)
							{
								return -1;
							}
							return 1;
						}
						if (_Build <= value._Build)
						{
							return -1;
						}
						return 1;
					}
					if (_Minor <= value._Minor)
					{
						return -1;
					}
					return 1;
				}
				if (_Major <= value._Major)
				{
					return -1;
				}
				return 1;
			}
			return 1;
		}
		return 0;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		return Equals(obj as Version);
	}

	public bool Equals([NotNullWhen(true)] Version? obj)
	{
		if ((object)obj != this)
		{
			if ((object)obj != null && _Major == obj._Major && _Minor == obj._Minor && _Build == obj._Build)
			{
				return _Revision == obj._Revision;
			}
			return false;
		}
		return true;
	}

	public override int GetHashCode()
	{
		int num = 0;
		num |= (_Major & 0xF) << 28;
		num |= (_Minor & 0xFF) << 20;
		num |= (_Build & 0xFF) << 12;
		return num | (_Revision & 0xFFF);
	}

	public override string ToString()
	{
		return ToString(DefaultFormatFieldCount);
	}

	public string ToString(int fieldCount)
	{
		Span<char> destination = stackalloc char[47];
		int charsWritten;
		bool flag = TryFormat(destination, fieldCount, out charsWritten);
		return destination.Slice(0, charsWritten).ToString();
	}

	string IFormattable.ToString(string format, IFormatProvider formatProvider)
	{
		return ToString();
	}

	public bool TryFormat(Span<char> destination, out int charsWritten)
	{
		return TryFormat(destination, DefaultFormatFieldCount, out charsWritten);
	}

	public bool TryFormat(Span<char> destination, int fieldCount, out int charsWritten)
	{
		switch (fieldCount)
		{
		default:
			ThrowArgumentException("4");
			break;
		case 3:
		case 4:
			if (_Build != -1)
			{
				if (fieldCount == 4 && _Revision == -1)
				{
					ThrowArgumentException("3");
				}
			}
			else
			{
				ThrowArgumentException("2");
			}
			break;
		case 0:
		case 1:
		case 2:
			break;
		}
		int num = 0;
		for (int i = 0; i < fieldCount; i++)
		{
			if (i != 0)
			{
				if (destination.IsEmpty)
				{
					charsWritten = 0;
					return false;
				}
				destination[0] = '.';
				destination = destination.Slice(1);
				num++;
			}
			if (!(i switch
			{
				0 => (uint)_Major, 
				1 => (uint)_Minor, 
				2 => (uint)_Build, 
				_ => (uint)_Revision, 
			}).TryFormat(destination, out var charsWritten2))
			{
				charsWritten = 0;
				return false;
			}
			num += charsWritten2;
			destination = destination.Slice(charsWritten2);
		}
		charsWritten = num;
		return true;
		static void ThrowArgumentException(string failureUpperBound)
		{
			throw new ArgumentException(SR.Format(SR.ArgumentOutOfRange_Bounds_Lower_Upper, "0", failureUpperBound), "fieldCount");
		}
	}

	bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider provider)
	{
		return TryFormat(destination, DefaultFormatFieldCount, out charsWritten);
	}

	public static Version Parse(string input)
	{
		if (input == null)
		{
			throw new ArgumentNullException("input");
		}
		return ParseVersion(input.AsSpan(), throwOnFailure: true);
	}

	public static Version Parse(ReadOnlySpan<char> input)
	{
		return ParseVersion(input, throwOnFailure: true);
	}

	public static bool TryParse([NotNullWhen(true)] string? input, [NotNullWhen(true)] out Version? result)
	{
		if (input == null)
		{
			result = null;
			return false;
		}
		return (result = ParseVersion(input.AsSpan(), throwOnFailure: false)) != null;
	}

	public static bool TryParse(ReadOnlySpan<char> input, [NotNullWhen(true)] out Version? result)
	{
		return (result = ParseVersion(input, throwOnFailure: false)) != null;
	}

	private static Version ParseVersion(ReadOnlySpan<char> input, bool throwOnFailure)
	{
		int num = input.IndexOf('.');
		if (num < 0)
		{
			if (throwOnFailure)
			{
				throw new ArgumentException(SR.Arg_VersionString, "input");
			}
			return null;
		}
		int num2 = -1;
		int num3 = input.Slice(num + 1).IndexOf('.');
		if (num3 != -1)
		{
			num3 += num + 1;
			num2 = input.Slice(num3 + 1).IndexOf('.');
			if (num2 != -1)
			{
				num2 += num3 + 1;
				if (input.Slice(num2 + 1).Contains('.'))
				{
					if (throwOnFailure)
					{
						throw new ArgumentException(SR.Arg_VersionString, "input");
					}
					return null;
				}
			}
		}
		if (!TryParseComponent(input.Slice(0, num), "input", throwOnFailure, out var parsedComponent))
		{
			return null;
		}
		int parsedComponent2;
		if (num3 != -1)
		{
			if (!TryParseComponent(input.Slice(num + 1, num3 - num - 1), "input", throwOnFailure, out parsedComponent2))
			{
				return null;
			}
			int parsedComponent3;
			if (num2 != -1)
			{
				if (!TryParseComponent(input.Slice(num3 + 1, num2 - num3 - 1), "build", throwOnFailure, out parsedComponent3) || !TryParseComponent(input.Slice(num2 + 1), "revision", throwOnFailure, out var parsedComponent4))
				{
					return null;
				}
				return new Version(parsedComponent, parsedComponent2, parsedComponent3, parsedComponent4);
			}
			if (!TryParseComponent(input.Slice(num3 + 1), "build", throwOnFailure, out parsedComponent3))
			{
				return null;
			}
			return new Version(parsedComponent, parsedComponent2, parsedComponent3);
		}
		if (!TryParseComponent(input.Slice(num + 1), "input", throwOnFailure, out parsedComponent2))
		{
			return null;
		}
		return new Version(parsedComponent, parsedComponent2);
	}

	private static bool TryParseComponent(ReadOnlySpan<char> component, string componentName, bool throwOnFailure, out int parsedComponent)
	{
		if (throwOnFailure)
		{
			if ((parsedComponent = int.Parse(component, NumberStyles.Integer, CultureInfo.InvariantCulture)) < 0)
			{
				throw new ArgumentOutOfRangeException(componentName, SR.ArgumentOutOfRange_Version);
			}
			return true;
		}
		if (int.TryParse(component, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedComponent))
		{
			return parsedComponent >= 0;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator ==(Version? v1, Version? v2)
	{
		if ((object)v2 == null)
		{
			if ((object)v1 != null)
			{
				return false;
			}
			return true;
		}
		if ((object)v2 != v1)
		{
			return v2.Equals(v1);
		}
		return true;
	}

	public static bool operator !=(Version? v1, Version? v2)
	{
		return !(v1 == v2);
	}

	public static bool operator <(Version? v1, Version? v2)
	{
		if ((object)v1 == null)
		{
			return (object)v2 != null;
		}
		return v1.CompareTo(v2) < 0;
	}

	public static bool operator <=(Version? v1, Version? v2)
	{
		if ((object)v1 == null)
		{
			return true;
		}
		return v1.CompareTo(v2) <= 0;
	}

	public static bool operator >(Version? v1, Version? v2)
	{
		return v2 < v1;
	}

	public static bool operator >=(Version? v1, Version? v2)
	{
		return v2 <= v1;
	}
}
