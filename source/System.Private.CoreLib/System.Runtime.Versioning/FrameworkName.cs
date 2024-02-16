using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.Versioning;

public sealed class FrameworkName : IEquatable<FrameworkName?>
{
	private readonly string _identifier;

	private readonly Version _version;

	private readonly string _profile;

	private string _fullName;

	public string Identifier => _identifier;

	public Version Version => _version;

	public string Profile => _profile;

	public string FullName
	{
		get
		{
			if (_fullName == null)
			{
				_fullName = (string.IsNullOrEmpty(Profile) ? $"{Identifier}{",Version=v"}{Version}" : $"{Identifier}{",Version=v"}{Version}{",Profile="}{Profile}");
			}
			return _fullName;
		}
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		return Equals(obj as FrameworkName);
	}

	public bool Equals([NotNullWhen(true)] FrameworkName? other)
	{
		if ((object)other == null)
		{
			return false;
		}
		if (Identifier == other.Identifier && Version == other.Version)
		{
			return Profile == other.Profile;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return Identifier.GetHashCode() ^ Version.GetHashCode() ^ Profile.GetHashCode();
	}

	public override string ToString()
	{
		return FullName;
	}

	public FrameworkName(string identifier, Version version)
		: this(identifier, version, null)
	{
	}

	public FrameworkName(string identifier, Version version, string? profile)
	{
		if (identifier == null)
		{
			throw new ArgumentNullException("identifier");
		}
		identifier = identifier.Trim();
		if (identifier.Length == 0)
		{
			throw new ArgumentException(SR.Format(SR.net_emptystringcall, "identifier"), "identifier");
		}
		if (version == null)
		{
			throw new ArgumentNullException("version");
		}
		_identifier = identifier;
		_version = version;
		_profile = ((profile == null) ? string.Empty : profile.Trim());
	}

	public FrameworkName(string frameworkName)
	{
		if (frameworkName == null)
		{
			throw new ArgumentNullException("frameworkName");
		}
		if (frameworkName.Length == 0)
		{
			throw new ArgumentException(SR.Format(SR.net_emptystringcall, "frameworkName"), "frameworkName");
		}
		string[] array = frameworkName.Split(',');
		if (array.Length < 2 || array.Length > 3)
		{
			throw new ArgumentException(SR.Argument_FrameworkNameTooShort, "frameworkName");
		}
		_identifier = array[0].Trim();
		if (_identifier.Length == 0)
		{
			throw new ArgumentException(SR.Argument_FrameworkNameInvalid, "frameworkName");
		}
		bool flag = false;
		_profile = string.Empty;
		for (int i = 1; i < array.Length; i++)
		{
			string text = array[i];
			int num = text.IndexOf('=');
			if (num == -1 || num != text.LastIndexOf('='))
			{
				throw new ArgumentException(SR.Argument_FrameworkNameInvalid, "frameworkName");
			}
			ReadOnlySpan<char> span = text.AsSpan(0, num).Trim();
			ReadOnlySpan<char> input = text.AsSpan(num + 1).Trim();
			if (MemoryExtensions.Equals(span, "Version", StringComparison.OrdinalIgnoreCase))
			{
				flag = true;
				if (input.Length > 0 && (input[0] == 'v' || input[0] == 'V'))
				{
					input = input.Slice(1);
				}
				try
				{
					_version = System.Version.Parse(input);
				}
				catch (Exception innerException)
				{
					throw new ArgumentException(SR.Argument_FrameworkNameInvalidVersion, "frameworkName", innerException);
				}
			}
			else
			{
				if (!MemoryExtensions.Equals(span, "Profile", StringComparison.OrdinalIgnoreCase))
				{
					throw new ArgumentException(SR.Argument_FrameworkNameInvalid, "frameworkName");
				}
				if (input.Length > 0)
				{
					_profile = input.ToString();
				}
			}
		}
		if (!flag)
		{
			throw new ArgumentException(SR.Argument_FrameworkNameMissingVersion, "frameworkName");
		}
	}

	public static bool operator ==(FrameworkName? left, FrameworkName? right)
	{
		return left?.Equals(right) ?? ((object)right == null);
	}

	public static bool operator !=(FrameworkName? left, FrameworkName? right)
	{
		return !(left == right);
	}
}
