using System.Configuration.Assemblies;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;

namespace System.Reflection;

public sealed class AssemblyName : ICloneable, IDeserializationCallback, ISerializable
{
	private string _name;

	private byte[] _publicKey;

	private byte[] _publicKeyToken;

	private CultureInfo _cultureInfo;

	private string _codeBase;

	private Version _version;

	private AssemblyHashAlgorithm _hashAlgorithm;

	private AssemblyVersionCompatibility _versionCompatibility;

	private AssemblyNameFlags _flags;

	internal const char c_DummyChar = '\uffff';

	private const short c_MaxAsciiCharsReallocate = 40;

	private const short c_MaxUnicodeCharsReallocate = 40;

	private const short c_MaxUTF_8BytesPerUnicodeChar = 4;

	private const short c_EncodedCharsPerByte = 3;

	private const string RFC3986ReservedMarks = ":/?#[]@!$&'()*+,;=";

	private const string RFC3986UnreservedMarks = "-._~";

	public string? Name
	{
		get
		{
			return _name;
		}
		set
		{
			_name = value;
		}
	}

	public Version? Version
	{
		get
		{
			return _version;
		}
		set
		{
			_version = value;
		}
	}

	public CultureInfo? CultureInfo
	{
		get
		{
			return _cultureInfo;
		}
		set
		{
			_cultureInfo = value;
		}
	}

	public string? CultureName
	{
		get
		{
			return _cultureInfo?.Name;
		}
		set
		{
			_cultureInfo = ((value == null) ? null : new CultureInfo(value));
		}
	}

	public string? CodeBase
	{
		[RequiresAssemblyFiles("The code will return an empty string for assemblies embedded in a single-file app")]
		get
		{
			return _codeBase;
		}
		set
		{
			_codeBase = value;
		}
	}

	[RequiresAssemblyFiles("The code will return an empty string for assemblies embedded in a single-file app")]
	public string? EscapedCodeBase
	{
		get
		{
			if (_codeBase == null)
			{
				return null;
			}
			return EscapeCodeBase(_codeBase);
		}
	}

	public ProcessorArchitecture ProcessorArchitecture
	{
		get
		{
			int num = (int)(_flags & (AssemblyNameFlags)112) >> 4;
			if (num > 5)
			{
				num = 0;
			}
			return (ProcessorArchitecture)num;
		}
		set
		{
			int num = (int)(value & (ProcessorArchitecture)7);
			if (num <= 5)
			{
				_flags = (AssemblyNameFlags)((long)_flags & 0xFFFFFF0FL);
				_flags |= (AssemblyNameFlags)(num << 4);
			}
		}
	}

	public AssemblyContentType ContentType
	{
		get
		{
			int num = (int)(_flags & (AssemblyNameFlags)3584) >> 9;
			if (num > 1)
			{
				num = 0;
			}
			return (AssemblyContentType)num;
		}
		set
		{
			int num = (int)(value & (AssemblyContentType)7);
			if (num <= 1)
			{
				_flags = (AssemblyNameFlags)((long)_flags & 0xFFFFF1FFL);
				_flags |= (AssemblyNameFlags)(num << 9);
			}
		}
	}

	public AssemblyNameFlags Flags
	{
		get
		{
			return _flags & (AssemblyNameFlags)(-3825);
		}
		set
		{
			_flags &= (AssemblyNameFlags)3824;
			_flags |= value & (AssemblyNameFlags)(-3825);
		}
	}

	public AssemblyHashAlgorithm HashAlgorithm
	{
		get
		{
			return _hashAlgorithm;
		}
		set
		{
			_hashAlgorithm = value;
		}
	}

	public AssemblyVersionCompatibility VersionCompatibility
	{
		get
		{
			return _versionCompatibility;
		}
		set
		{
			_versionCompatibility = value;
		}
	}

	[Obsolete("Strong name signing is not supported and throws PlatformNotSupportedException.", DiagnosticId = "SYSLIB0017", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public StrongNameKeyPair? KeyPair
	{
		get
		{
			throw new PlatformNotSupportedException(SR.PlatformNotSupported_StrongNameSigning);
		}
		set
		{
			throw new PlatformNotSupportedException(SR.PlatformNotSupported_StrongNameSigning);
		}
	}

	public string FullName
	{
		get
		{
			if (string.IsNullOrEmpty(Name))
			{
				return string.Empty;
			}
			byte[] pkt = _publicKeyToken ?? ComputePublicKeyToken();
			return AssemblyNameFormatter.ComputeDisplayName(Name, Version, CultureName, pkt, Flags, ContentType);
		}
	}

	public AssemblyName(string assemblyName)
	{
		if (assemblyName == null)
		{
			throw new ArgumentNullException("assemblyName");
		}
		if (assemblyName.Length == 0 || assemblyName[0] == '\0')
		{
			throw new ArgumentException(SR.Format_StringZeroLength);
		}
		_name = assemblyName;
		nInit();
	}

	internal AssemblyName(string name, byte[] publicKey, byte[] publicKeyToken, Version version, CultureInfo cultureInfo, AssemblyHashAlgorithm hashAlgorithm, AssemblyVersionCompatibility versionCompatibility, string codeBase, AssemblyNameFlags flags)
	{
		_name = name;
		_publicKey = publicKey;
		_publicKeyToken = publicKeyToken;
		_version = version;
		_cultureInfo = cultureInfo;
		_hashAlgorithm = hashAlgorithm;
		_versionCompatibility = versionCompatibility;
		_codeBase = codeBase;
		_flags = flags;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal extern void nInit();

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern AssemblyName nGetFileInformation(string s);

	internal static AssemblyName GetFileInformationCore(string assemblyFile)
	{
		string fullPath = Path.GetFullPath(assemblyFile);
		return nGetFileInformation(fullPath);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private extern byte[] ComputePublicKeyToken();

	internal void SetProcArchIndex(PortableExecutableKinds pek, ImageFileMachine ifm)
	{
		ProcessorArchitecture = CalculateProcArchIndex(pek, ifm, _flags);
	}

	internal static ProcessorArchitecture CalculateProcArchIndex(PortableExecutableKinds pek, ImageFileMachine ifm, AssemblyNameFlags flags)
	{
		if ((flags & (AssemblyNameFlags)240) == (AssemblyNameFlags)112)
		{
			return ProcessorArchitecture.None;
		}
		if ((pek & PortableExecutableKinds.PE32Plus) == PortableExecutableKinds.PE32Plus)
		{
			switch (ifm)
			{
			case ImageFileMachine.IA64:
				return ProcessorArchitecture.IA64;
			case ImageFileMachine.AMD64:
				return ProcessorArchitecture.Amd64;
			case ImageFileMachine.I386:
				if ((pek & PortableExecutableKinds.ILOnly) == PortableExecutableKinds.ILOnly)
				{
					return ProcessorArchitecture.MSIL;
				}
				break;
			}
		}
		else
		{
			switch (ifm)
			{
			case ImageFileMachine.I386:
				if ((pek & PortableExecutableKinds.Required32Bit) == PortableExecutableKinds.Required32Bit)
				{
					return ProcessorArchitecture.X86;
				}
				if ((pek & PortableExecutableKinds.ILOnly) == PortableExecutableKinds.ILOnly)
				{
					return ProcessorArchitecture.MSIL;
				}
				return ProcessorArchitecture.X86;
			case ImageFileMachine.ARM:
				return ProcessorArchitecture.Arm;
			}
		}
		return ProcessorArchitecture.None;
	}

	public AssemblyName()
	{
		_versionCompatibility = AssemblyVersionCompatibility.SameMachine;
	}

	public object Clone()
	{
		return new AssemblyName
		{
			_name = _name,
			_publicKey = (byte[])_publicKey?.Clone(),
			_publicKeyToken = (byte[])_publicKeyToken?.Clone(),
			_cultureInfo = _cultureInfo,
			_version = _version,
			_flags = _flags,
			_codeBase = _codeBase,
			_hashAlgorithm = _hashAlgorithm,
			_versionCompatibility = _versionCompatibility
		};
	}

	public static AssemblyName GetAssemblyName(string assemblyFile)
	{
		if (assemblyFile == null)
		{
			throw new ArgumentNullException("assemblyFile");
		}
		return GetFileInformationCore(assemblyFile);
	}

	public byte[]? GetPublicKey()
	{
		return _publicKey;
	}

	public void SetPublicKey(byte[]? publicKey)
	{
		_publicKey = publicKey;
		if (publicKey == null)
		{
			_flags &= ~AssemblyNameFlags.PublicKey;
		}
		else
		{
			_flags |= AssemblyNameFlags.PublicKey;
		}
	}

	public byte[]? GetPublicKeyToken()
	{
		return _publicKeyToken ?? (_publicKeyToken = ComputePublicKeyToken());
	}

	public void SetPublicKeyToken(byte[]? publicKeyToken)
	{
		_publicKeyToken = publicKeyToken;
	}

	public override string ToString()
	{
		string fullName = FullName;
		if (fullName == null)
		{
			return base.ToString();
		}
		return fullName;
	}

	public void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		throw new PlatformNotSupportedException();
	}

	public void OnDeserialization(object? sender)
	{
		throw new PlatformNotSupportedException();
	}

	public static bool ReferenceMatchesDefinition(AssemblyName? reference, AssemblyName? definition)
	{
		if (reference == definition)
		{
			return true;
		}
		if (reference == null)
		{
			throw new ArgumentNullException("reference");
		}
		if (definition == null)
		{
			throw new ArgumentNullException("definition");
		}
		string text = reference.Name ?? string.Empty;
		string value = definition.Name ?? string.Empty;
		return text.Equals(value, StringComparison.OrdinalIgnoreCase);
	}

	[RequiresAssemblyFiles("The code will return an empty string for assemblies embedded in a single-file app")]
	internal static string EscapeCodeBase(string codebase)
	{
		if (codebase == null)
		{
			return string.Empty;
		}
		int destPos = 0;
		char[] array = EscapeString(codebase, 0, codebase.Length, null, ref destPos, isUriString: true, '\uffff', '\uffff', '\uffff');
		if (array == null)
		{
			return codebase;
		}
		return new string(array, 0, destPos);
	}

	internal unsafe static char[] EscapeString(string input, int start, int end, char[] dest, ref int destPos, bool isUriString, char force1, char force2, char rsvd)
	{
		int i = start;
		int num = start;
		byte* ptr = stackalloc byte[160];
		fixed (char* ptr2 = input)
		{
			for (; i < end; i++)
			{
				char c = ptr2[i];
				if (c > '\u007f')
				{
					short num2 = (short)Math.Min(end - i, 39);
					short num3 = 1;
					while (num3 < num2 && ptr2[i + num3] > '\u007f')
					{
						num3++;
					}
					if (ptr2[i + num3 - 1] >= '\ud800' && ptr2[i + num3 - 1] <= '\udbff')
					{
						if (num3 == 1 || num3 == end - i)
						{
							throw new FormatException(SR.Arg_FormatException);
						}
						num3++;
					}
					dest = EnsureDestinationSize(ptr2, dest, i, (short)(num3 * 4 * 3), 480, ref destPos, num);
					short num4 = (short)Encoding.UTF8.GetBytes(ptr2 + i, num3, ptr, 160);
					if (num4 == 0)
					{
						throw new FormatException(SR.Arg_FormatException);
					}
					i += num3 - 1;
					for (num3 = 0; num3 < num4; num3++)
					{
						EscapeAsciiChar((char)ptr[num3], dest, ref destPos);
					}
					num = i + 1;
				}
				else if (c == '%' && rsvd == '%')
				{
					dest = EnsureDestinationSize(ptr2, dest, i, 3, 120, ref destPos, num);
					if (i + 2 < end && HexConverter.IsHexChar(ptr2[i + 1]) && HexConverter.IsHexChar(ptr2[i + 2]))
					{
						dest[destPos++] = '%';
						dest[destPos++] = ptr2[i + 1];
						dest[destPos++] = ptr2[i + 2];
						i += 2;
					}
					else
					{
						EscapeAsciiChar('%', dest, ref destPos);
					}
					num = i + 1;
				}
				else if (c == force1 || c == force2 || (c != rsvd && (isUriString ? (!IsReservedUnreservedOrHash(c)) : (!IsUnreserved(c)))))
				{
					dest = EnsureDestinationSize(ptr2, dest, i, 3, 120, ref destPos, num);
					EscapeAsciiChar(c, dest, ref destPos);
					num = i + 1;
				}
			}
			if (num != i && (num != start || dest != null))
			{
				dest = EnsureDestinationSize(ptr2, dest, i, 0, 0, ref destPos, num);
			}
		}
		return dest;
	}

	private unsafe static char[] EnsureDestinationSize(char* pStr, char[] dest, int currentInputPos, short charsToAdd, short minReallocateChars, ref int destPos, int prevInputPos)
	{
		if (dest == null || dest.Length < destPos + (currentInputPos - prevInputPos) + charsToAdd)
		{
			char[] array = new char[destPos + (currentInputPos - prevInputPos) + minReallocateChars];
			if (dest != null && destPos != 0)
			{
				Buffer.BlockCopy(dest, 0, array, 0, destPos << 1);
			}
			dest = array;
		}
		while (prevInputPos != currentInputPos)
		{
			dest[destPos++] = pStr[prevInputPos++];
		}
		return dest;
	}

	internal static void EscapeAsciiChar(char ch, char[] to, ref int pos)
	{
		to[pos++] = '%';
		to[pos++] = HexConverter.ToCharUpper((int)ch >> 4);
		to[pos++] = HexConverter.ToCharUpper(ch);
	}

	private static bool IsReservedUnreservedOrHash(char c)
	{
		if (IsUnreserved(c))
		{
			return true;
		}
		return ":/?#[]@!$&'()*+,;=".Contains(c);
	}

	internal static bool IsUnreserved(char c)
	{
		if (IsAsciiLetterOrDigit(c))
		{
			return true;
		}
		return "-._~".Contains(c);
	}

	internal static bool IsAsciiLetter(char character)
	{
		if (character < 'a' || character > 'z')
		{
			if (character >= 'A')
			{
				return character <= 'Z';
			}
			return false;
		}
		return true;
	}

	internal static bool IsAsciiLetterOrDigit(char character)
	{
		if (!IsAsciiLetter(character))
		{
			if (character >= '0')
			{
				return character <= '9';
			}
			return false;
		}
		return true;
	}
}
