using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace System.IO;

public static class Path
{
	private readonly struct Join3Payload
	{
		public unsafe readonly char* First;

		public readonly int FirstLength;

		public unsafe readonly char* Second;

		public readonly int SecondLength;

		public unsafe readonly char* Third;

		public readonly int ThirdLength;

		public readonly byte Separators;

		public unsafe Join3Payload(char* first, int firstLength, char* second, int secondLength, char* third, int thirdLength, byte separators)
		{
			First = first;
			FirstLength = firstLength;
			Second = second;
			SecondLength = secondLength;
			Third = third;
			ThirdLength = thirdLength;
			Separators = separators;
		}
	}

	private readonly struct Join4Payload
	{
		public unsafe readonly char* First;

		public readonly int FirstLength;

		public unsafe readonly char* Second;

		public readonly int SecondLength;

		public unsafe readonly char* Third;

		public readonly int ThirdLength;

		public unsafe readonly char* Fourth;

		public readonly int FourthLength;

		public readonly byte Separators;

		public unsafe Join4Payload(char* first, int firstLength, char* second, int secondLength, char* third, int thirdLength, char* fourth, int fourthLength, byte separators)
		{
			First = first;
			FirstLength = firstLength;
			Second = second;
			SecondLength = secondLength;
			Third = third;
			ThirdLength = thirdLength;
			Fourth = fourth;
			FourthLength = fourthLength;
			Separators = separators;
		}
	}

	public static readonly char DirectorySeparatorChar = '\\';

	public static readonly char AltDirectorySeparatorChar = '/';

	public static readonly char VolumeSeparatorChar = ':';

	public static readonly char PathSeparator = ';';

	[Obsolete("Path.InvalidPathChars has been deprecated. Use GetInvalidPathChars or GetInvalidFileNameChars instead.")]
	public static readonly char[] InvalidPathChars = GetInvalidPathChars();

	private static ReadOnlySpan<byte> Base32Char => "abcdefghijklmnopqrstuvwxyz012345"u8;

	[return: NotNullIfNotNull("path")]
	public static string? ChangeExtension(string? path, string? extension)
	{
		if (path == null)
		{
			return null;
		}
		int num = path.Length;
		if (num == 0)
		{
			return string.Empty;
		}
		for (int num2 = path.Length - 1; num2 >= 0; num2--)
		{
			char c = path[num2];
			if (c == '.')
			{
				num = num2;
				break;
			}
			if (PathInternal.IsDirectorySeparator(c))
			{
				break;
			}
		}
		if (extension == null)
		{
			return path.Substring(0, num);
		}
		ReadOnlySpan<char> readOnlySpan = path.AsSpan(0, num);
		if (!extension.StartsWith('.'))
		{
			return string.Concat(readOnlySpan, ".", extension);
		}
		return string.Concat(readOnlySpan, extension);
	}

	public static string? GetDirectoryName(string? path)
	{
		if (path == null || PathInternal.IsEffectivelyEmpty(path.AsSpan()))
		{
			return null;
		}
		int directoryNameOffset = GetDirectoryNameOffset(path.AsSpan());
		if (directoryNameOffset < 0)
		{
			return null;
		}
		return PathInternal.NormalizeDirectorySeparators(path.Substring(0, directoryNameOffset));
	}

	public static ReadOnlySpan<char> GetDirectoryName(ReadOnlySpan<char> path)
	{
		if (PathInternal.IsEffectivelyEmpty(path))
		{
			return ReadOnlySpan<char>.Empty;
		}
		int directoryNameOffset = GetDirectoryNameOffset(path);
		if (directoryNameOffset < 0)
		{
			return ReadOnlySpan<char>.Empty;
		}
		return path.Slice(0, directoryNameOffset);
	}

	internal static int GetDirectoryNameOffset(ReadOnlySpan<char> path)
	{
		int rootLength = PathInternal.GetRootLength(path);
		int num = path.Length;
		if (num <= rootLength)
		{
			return -1;
		}
		while (num > rootLength && !PathInternal.IsDirectorySeparator(path[--num]))
		{
		}
		while (num > rootLength && PathInternal.IsDirectorySeparator(path[num - 1]))
		{
			num--;
		}
		return num;
	}

	[return: NotNullIfNotNull("path")]
	public static string? GetExtension(string? path)
	{
		if (path == null)
		{
			return null;
		}
		return GetExtension(path.AsSpan()).ToString();
	}

	public static ReadOnlySpan<char> GetExtension(ReadOnlySpan<char> path)
	{
		int length = path.Length;
		for (int num = length - 1; num >= 0; num--)
		{
			char c = path[num];
			if (c == '.')
			{
				if (num != length - 1)
				{
					return path.Slice(num, length - num);
				}
				return ReadOnlySpan<char>.Empty;
			}
			if (PathInternal.IsDirectorySeparator(c))
			{
				break;
			}
		}
		return ReadOnlySpan<char>.Empty;
	}

	[return: NotNullIfNotNull("path")]
	public static string? GetFileName(string? path)
	{
		if (path == null)
		{
			return null;
		}
		ReadOnlySpan<char> fileName = GetFileName(path.AsSpan());
		if (path.Length == fileName.Length)
		{
			return path;
		}
		return fileName.ToString();
	}

	public static ReadOnlySpan<char> GetFileName(ReadOnlySpan<char> path)
	{
		int length = GetPathRoot(path).Length;
		int num = path.Length;
		while (--num >= 0)
		{
			if (num < length || PathInternal.IsDirectorySeparator(path[num]))
			{
				return path.Slice(num + 1, path.Length - num - 1);
			}
		}
		return path;
	}

	[return: NotNullIfNotNull("path")]
	public static string? GetFileNameWithoutExtension(string? path)
	{
		if (path == null)
		{
			return null;
		}
		ReadOnlySpan<char> fileNameWithoutExtension = GetFileNameWithoutExtension(path.AsSpan());
		if (path.Length == fileNameWithoutExtension.Length)
		{
			return path;
		}
		return fileNameWithoutExtension.ToString();
	}

	public static ReadOnlySpan<char> GetFileNameWithoutExtension(ReadOnlySpan<char> path)
	{
		ReadOnlySpan<char> fileName = GetFileName(path);
		int num = fileName.LastIndexOf('.');
		if (num != -1)
		{
			return fileName.Slice(0, num);
		}
		return fileName;
	}

	public unsafe static string GetRandomFileName()
	{
		byte* ptr = stackalloc byte[8];
		Interop.GetRandomBytes(ptr, 8);
		return string.Create(12, (IntPtr)ptr, delegate(Span<char> span, IntPtr key)
		{
			Populate83FileNameFromRandomBytes((byte*)(void*)key, 8, span);
		});
	}

	public static bool IsPathFullyQualified(string path)
	{
		if (path == null)
		{
			throw new ArgumentNullException("path");
		}
		return IsPathFullyQualified(path.AsSpan());
	}

	public static bool IsPathFullyQualified(ReadOnlySpan<char> path)
	{
		return !PathInternal.IsPartiallyQualified(path);
	}

	public static bool HasExtension([NotNullWhen(true)] string? path)
	{
		if (path != null)
		{
			return HasExtension(path.AsSpan());
		}
		return false;
	}

	public static bool HasExtension(ReadOnlySpan<char> path)
	{
		for (int num = path.Length - 1; num >= 0; num--)
		{
			char c = path[num];
			if (c == '.')
			{
				return num != path.Length - 1;
			}
			if (PathInternal.IsDirectorySeparator(c))
			{
				break;
			}
		}
		return false;
	}

	public static string Combine(string path1, string path2)
	{
		if (path1 == null || path2 == null)
		{
			throw new ArgumentNullException((path1 == null) ? "path1" : "path2");
		}
		return CombineInternal(path1, path2);
	}

	public static string Combine(string path1, string path2, string path3)
	{
		if (path1 == null || path2 == null || path3 == null)
		{
			throw new ArgumentNullException((path1 == null) ? "path1" : ((path2 == null) ? "path2" : "path3"));
		}
		return CombineInternal(path1, path2, path3);
	}

	public static string Combine(string path1, string path2, string path3, string path4)
	{
		if (path1 == null || path2 == null || path3 == null || path4 == null)
		{
			throw new ArgumentNullException((path1 == null) ? "path1" : ((path2 == null) ? "path2" : ((path3 == null) ? "path3" : "path4")));
		}
		return CombineInternal(path1, path2, path3, path4);
	}

	public static string Combine(params string[] paths)
	{
		if (paths == null)
		{
			throw new ArgumentNullException("paths");
		}
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < paths.Length; i++)
		{
			if (paths[i] == null)
			{
				throw new ArgumentNullException("paths");
			}
			if (paths[i].Length != 0)
			{
				if (IsPathRooted(paths[i]))
				{
					num2 = i;
					num = paths[i].Length;
				}
				else
				{
					num += paths[i].Length;
				}
				char c = paths[i][paths[i].Length - 1];
				if (!PathInternal.IsDirectorySeparator(c))
				{
					num++;
				}
			}
		}
		Span<char> initialBuffer = stackalloc char[260];
		ValueStringBuilder valueStringBuilder = new ValueStringBuilder(initialBuffer);
		valueStringBuilder.EnsureCapacity(num);
		for (int j = num2; j < paths.Length; j++)
		{
			if (paths[j].Length == 0)
			{
				continue;
			}
			if (valueStringBuilder.Length == 0)
			{
				valueStringBuilder.Append(paths[j]);
				continue;
			}
			char c2 = valueStringBuilder[valueStringBuilder.Length - 1];
			if (!PathInternal.IsDirectorySeparator(c2))
			{
				valueStringBuilder.Append('\\');
			}
			valueStringBuilder.Append(paths[j]);
		}
		return valueStringBuilder.ToString();
	}

	public static string Join(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2)
	{
		if (path1.Length == 0)
		{
			return path2.ToString();
		}
		if (path2.Length == 0)
		{
			return path1.ToString();
		}
		return JoinInternal(path1, path2);
	}

	public static string Join(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, ReadOnlySpan<char> path3)
	{
		if (path1.Length == 0)
		{
			return Join(path2, path3);
		}
		if (path2.Length == 0)
		{
			return Join(path1, path3);
		}
		if (path3.Length == 0)
		{
			return Join(path1, path2);
		}
		return JoinInternal(path1, path2, path3);
	}

	public static string Join(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, ReadOnlySpan<char> path3, ReadOnlySpan<char> path4)
	{
		if (path1.Length == 0)
		{
			return Join(path2, path3, path4);
		}
		if (path2.Length == 0)
		{
			return Join(path1, path3, path4);
		}
		if (path3.Length == 0)
		{
			return Join(path1, path2, path4);
		}
		if (path4.Length == 0)
		{
			return Join(path1, path2, path3);
		}
		return JoinInternal(path1, path2, path3, path4);
	}

	public static string Join(string? path1, string? path2)
	{
		return Join(path1.AsSpan(), path2.AsSpan());
	}

	public static string Join(string? path1, string? path2, string? path3)
	{
		return Join(path1.AsSpan(), path2.AsSpan(), path3.AsSpan());
	}

	public static string Join(string? path1, string? path2, string? path3, string? path4)
	{
		return Join(path1.AsSpan(), path2.AsSpan(), path3.AsSpan(), path4.AsSpan());
	}

	public static string Join(params string?[] paths)
	{
		if (paths == null)
		{
			throw new ArgumentNullException("paths");
		}
		if (paths.Length == 0)
		{
			return string.Empty;
		}
		int num = 0;
		for (int i = 0; i < paths.Length; i++)
		{
			num += paths[i]?.Length ?? 0;
		}
		num += paths.Length - 1;
		Span<char> initialBuffer = stackalloc char[260];
		ValueStringBuilder valueStringBuilder = new ValueStringBuilder(initialBuffer);
		valueStringBuilder.EnsureCapacity(num);
		foreach (string text in paths)
		{
			if (string.IsNullOrEmpty(text))
			{
				continue;
			}
			if (valueStringBuilder.Length == 0)
			{
				valueStringBuilder.Append(text);
				continue;
			}
			if (!PathInternal.IsDirectorySeparator(valueStringBuilder[valueStringBuilder.Length - 1]) && !PathInternal.IsDirectorySeparator(text[0]))
			{
				valueStringBuilder.Append('\\');
			}
			valueStringBuilder.Append(text);
		}
		return valueStringBuilder.ToString();
	}

	public static bool TryJoin(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, Span<char> destination, out int charsWritten)
	{
		charsWritten = 0;
		if (path1.Length == 0 && path2.Length == 0)
		{
			return true;
		}
		if (path1.Length == 0 || path2.Length == 0)
		{
			ref ReadOnlySpan<char> reference = ref path1.Length == 0 ? ref path2 : ref path1;
			if (destination.Length < reference.Length)
			{
				return false;
			}
			reference.CopyTo(destination);
			charsWritten = reference.Length;
			return true;
		}
		bool flag = !EndsInDirectorySeparator(path1) && !PathInternal.StartsWithDirectorySeparator(path2);
		int num = path1.Length + path2.Length + (flag ? 1 : 0);
		if (destination.Length < num)
		{
			return false;
		}
		path1.CopyTo(destination);
		if (flag)
		{
			destination[path1.Length] = DirectorySeparatorChar;
		}
		path2.CopyTo(destination.Slice(path1.Length + (flag ? 1 : 0)));
		charsWritten = num;
		return true;
	}

	public static bool TryJoin(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, ReadOnlySpan<char> path3, Span<char> destination, out int charsWritten)
	{
		charsWritten = 0;
		if (path1.Length == 0 && path2.Length == 0 && path3.Length == 0)
		{
			return true;
		}
		if (path1.Length == 0)
		{
			return TryJoin(path2, path3, destination, out charsWritten);
		}
		if (path2.Length == 0)
		{
			return TryJoin(path1, path3, destination, out charsWritten);
		}
		if (path3.Length == 0)
		{
			return TryJoin(path1, path2, destination, out charsWritten);
		}
		int num = ((!EndsInDirectorySeparator(path1) && !PathInternal.StartsWithDirectorySeparator(path2)) ? 1 : 0);
		bool flag = !EndsInDirectorySeparator(path2) && !PathInternal.StartsWithDirectorySeparator(path3);
		if (flag)
		{
			num++;
		}
		int num2 = path1.Length + path2.Length + path3.Length + num;
		if (destination.Length < num2)
		{
			return false;
		}
		bool flag2 = TryJoin(path1, path2, destination, out charsWritten);
		if (flag)
		{
			destination[charsWritten++] = DirectorySeparatorChar;
		}
		path3.CopyTo(destination.Slice(charsWritten));
		charsWritten += path3.Length;
		return true;
	}

	private static string CombineInternal(string first, string second)
	{
		if (string.IsNullOrEmpty(first))
		{
			return second;
		}
		if (string.IsNullOrEmpty(second))
		{
			return first;
		}
		if (IsPathRooted(second.AsSpan()))
		{
			return second;
		}
		return JoinInternal(first.AsSpan(), second.AsSpan());
	}

	private static string CombineInternal(string first, string second, string third)
	{
		if (string.IsNullOrEmpty(first))
		{
			return CombineInternal(second, third);
		}
		if (string.IsNullOrEmpty(second))
		{
			return CombineInternal(first, third);
		}
		if (string.IsNullOrEmpty(third))
		{
			return CombineInternal(first, second);
		}
		if (IsPathRooted(third.AsSpan()))
		{
			return third;
		}
		if (IsPathRooted(second.AsSpan()))
		{
			return CombineInternal(second, third);
		}
		return JoinInternal(first.AsSpan(), second.AsSpan(), third.AsSpan());
	}

	private static string CombineInternal(string first, string second, string third, string fourth)
	{
		if (string.IsNullOrEmpty(first))
		{
			return CombineInternal(second, third, fourth);
		}
		if (string.IsNullOrEmpty(second))
		{
			return CombineInternal(first, third, fourth);
		}
		if (string.IsNullOrEmpty(third))
		{
			return CombineInternal(first, second, fourth);
		}
		if (string.IsNullOrEmpty(fourth))
		{
			return CombineInternal(first, second, third);
		}
		if (IsPathRooted(fourth.AsSpan()))
		{
			return fourth;
		}
		if (IsPathRooted(third.AsSpan()))
		{
			return CombineInternal(third, fourth);
		}
		if (IsPathRooted(second.AsSpan()))
		{
			return CombineInternal(second, third, fourth);
		}
		return JoinInternal(first.AsSpan(), second.AsSpan(), third.AsSpan(), fourth.AsSpan());
	}

	private static string JoinInternal(ReadOnlySpan<char> first, ReadOnlySpan<char> second)
	{
		if (!PathInternal.IsDirectorySeparator(first[first.Length - 1]) && !PathInternal.IsDirectorySeparator(second[0]))
		{
			return string.Concat(first, "\\", second);
		}
		return string.Concat(first, second);
	}

	private unsafe static string JoinInternal(ReadOnlySpan<char> first, ReadOnlySpan<char> second, ReadOnlySpan<char> third)
	{
		byte b = (byte)((!PathInternal.IsDirectorySeparator(first[first.Length - 1]) && !PathInternal.IsDirectorySeparator(second[0])) ? 1 : 0);
		byte b2 = (byte)((!PathInternal.IsDirectorySeparator(second[second.Length - 1]) && !PathInternal.IsDirectorySeparator(third[0])) ? 1 : 0);
		fixed (char* first2 = &MemoryMarshal.GetReference(first))
		{
			fixed (char* second2 = &MemoryMarshal.GetReference(second))
			{
				fixed (char* third2 = &MemoryMarshal.GetReference(third))
				{
					Join3Payload join3Payload = new Join3Payload(first2, first.Length, second2, second.Length, third2, third.Length, (byte)(b | (b2 << 1)));
					return string.Create(first.Length + second.Length + third.Length + b + b2, (IntPtr)(&join3Payload), delegate(Span<char> destination, IntPtr statePtr)
					{
						ref Join3Payload reference = ref *(Join3Payload*)(void*)statePtr;
						new Span<char>(reference.First, reference.FirstLength).CopyTo(destination);
						if (((uint)reference.Separators & (true ? 1u : 0u)) != 0)
						{
							destination[reference.FirstLength] = '\\';
						}
						new Span<char>(reference.Second, reference.SecondLength).CopyTo(destination.Slice(reference.FirstLength + (reference.Separators & 1)));
						if ((reference.Separators & 2u) != 0)
						{
							destination[destination.Length - reference.ThirdLength - 1] = '\\';
						}
						new Span<char>(reference.Third, reference.ThirdLength).CopyTo(destination.Slice(destination.Length - reference.ThirdLength));
					});
				}
			}
		}
	}

	private unsafe static string JoinInternal(ReadOnlySpan<char> first, ReadOnlySpan<char> second, ReadOnlySpan<char> third, ReadOnlySpan<char> fourth)
	{
		byte b = (byte)((!PathInternal.IsDirectorySeparator(first[first.Length - 1]) && !PathInternal.IsDirectorySeparator(second[0])) ? 1 : 0);
		byte b2 = (byte)((!PathInternal.IsDirectorySeparator(second[second.Length - 1]) && !PathInternal.IsDirectorySeparator(third[0])) ? 1 : 0);
		byte b3 = (byte)((!PathInternal.IsDirectorySeparator(third[third.Length - 1]) && !PathInternal.IsDirectorySeparator(fourth[0])) ? 1 : 0);
		fixed (char* first2 = &MemoryMarshal.GetReference(first))
		{
			fixed (char* second2 = &MemoryMarshal.GetReference(second))
			{
				fixed (char* third2 = &MemoryMarshal.GetReference(third))
				{
					fixed (char* fourth2 = &MemoryMarshal.GetReference(fourth))
					{
						Join4Payload join4Payload = new Join4Payload(first2, first.Length, second2, second.Length, third2, third.Length, fourth2, fourth.Length, (byte)(b | (b2 << 1) | (b3 << 2)));
						return string.Create(first.Length + second.Length + third.Length + fourth.Length + b + b2 + b3, (IntPtr)(&join4Payload), delegate(Span<char> destination, IntPtr statePtr)
						{
							ref Join4Payload reference = ref *(Join4Payload*)(void*)statePtr;
							new Span<char>(reference.First, reference.FirstLength).CopyTo(destination);
							int firstLength = reference.FirstLength;
							if (((uint)reference.Separators & (true ? 1u : 0u)) != 0)
							{
								destination[firstLength++] = '\\';
							}
							new Span<char>(reference.Second, reference.SecondLength).CopyTo(destination.Slice(firstLength));
							firstLength += reference.SecondLength;
							if ((reference.Separators & 2u) != 0)
							{
								destination[firstLength++] = '\\';
							}
							new Span<char>(reference.Third, reference.ThirdLength).CopyTo(destination.Slice(firstLength));
							firstLength += reference.ThirdLength;
							if ((reference.Separators & 4u) != 0)
							{
								destination[firstLength++] = '\\';
							}
							new Span<char>(reference.Fourth, reference.FourthLength).CopyTo(destination.Slice(firstLength));
						});
					}
				}
			}
		}
	}

	private unsafe static void Populate83FileNameFromRandomBytes(byte* bytes, int byteCount, Span<char> chars)
	{
		byte b = *bytes;
		byte b2 = bytes[1];
		byte b3 = bytes[2];
		byte b4 = bytes[3];
		byte b5 = bytes[4];
		chars[11] = (char)Base32Char[bytes[7] & 0x1F];
		chars[0] = (char)Base32Char[b & 0x1F];
		chars[1] = (char)Base32Char[b2 & 0x1F];
		chars[2] = (char)Base32Char[b3 & 0x1F];
		chars[3] = (char)Base32Char[b4 & 0x1F];
		chars[4] = (char)Base32Char[b5 & 0x1F];
		chars[5] = (char)Base32Char[((b & 0xE0) >> 5) | ((b4 & 0x60) >> 2)];
		chars[6] = (char)Base32Char[((b2 & 0xE0) >> 5) | ((b5 & 0x60) >> 2)];
		b3 >>= 5;
		if ((b4 & 0x80u) != 0)
		{
			b3 = (byte)(b3 | 8u);
		}
		if ((b5 & 0x80u) != 0)
		{
			b3 = (byte)(b3 | 0x10u);
		}
		chars[7] = (char)Base32Char[b3];
		chars[8] = '.';
		chars[9] = (char)Base32Char[bytes[5] & 0x1F];
		chars[10] = (char)Base32Char[bytes[6] & 0x1F];
	}

	public static string GetRelativePath(string relativeTo, string path)
	{
		return GetRelativePath(relativeTo, path, PathInternal.StringComparison);
	}

	private static string GetRelativePath(string relativeTo, string path, StringComparison comparisonType)
	{
		if (relativeTo == null)
		{
			throw new ArgumentNullException("relativeTo");
		}
		if (PathInternal.IsEffectivelyEmpty(relativeTo.AsSpan()))
		{
			throw new ArgumentException(SR.Arg_PathEmpty, "relativeTo");
		}
		if (path == null)
		{
			throw new ArgumentNullException("path");
		}
		if (PathInternal.IsEffectivelyEmpty(path.AsSpan()))
		{
			throw new ArgumentException(SR.Arg_PathEmpty, "path");
		}
		relativeTo = GetFullPath(relativeTo);
		path = GetFullPath(path);
		if (!PathInternal.AreRootsEqual(relativeTo, path, comparisonType))
		{
			return path;
		}
		int num = PathInternal.GetCommonPathLength(relativeTo, path, comparisonType == StringComparison.OrdinalIgnoreCase);
		if (num == 0)
		{
			return path;
		}
		int num2 = relativeTo.Length;
		if (EndsInDirectorySeparator(relativeTo.AsSpan()))
		{
			num2--;
		}
		bool flag = EndsInDirectorySeparator(path.AsSpan());
		int num3 = path.Length;
		if (flag)
		{
			num3--;
		}
		if (num2 == num3 && num >= num2)
		{
			return ".";
		}
		Span<char> initialBuffer = stackalloc char[260];
		ValueStringBuilder valueStringBuilder = new ValueStringBuilder(initialBuffer);
		valueStringBuilder.EnsureCapacity(Math.Max(relativeTo.Length, path.Length));
		if (num < num2)
		{
			valueStringBuilder.Append("..");
			for (int i = num + 1; i < num2; i++)
			{
				if (PathInternal.IsDirectorySeparator(relativeTo[i]))
				{
					valueStringBuilder.Append(DirectorySeparatorChar);
					valueStringBuilder.Append("..");
				}
			}
		}
		else if (PathInternal.IsDirectorySeparator(path[num]))
		{
			num++;
		}
		int num4 = num3 - num;
		if (flag)
		{
			num4++;
		}
		if (num4 > 0)
		{
			if (valueStringBuilder.Length > 0)
			{
				valueStringBuilder.Append(DirectorySeparatorChar);
			}
			valueStringBuilder.Append(path.AsSpan(num, num4));
		}
		return valueStringBuilder.ToString();
	}

	public static string TrimEndingDirectorySeparator(string path)
	{
		return PathInternal.TrimEndingDirectorySeparator(path);
	}

	public static ReadOnlySpan<char> TrimEndingDirectorySeparator(ReadOnlySpan<char> path)
	{
		return PathInternal.TrimEndingDirectorySeparator(path);
	}

	public static bool EndsInDirectorySeparator(ReadOnlySpan<char> path)
	{
		return PathInternal.EndsInDirectorySeparator(path);
	}

	public static bool EndsInDirectorySeparator(string path)
	{
		return PathInternal.EndsInDirectorySeparator(path);
	}

	public static char[] GetInvalidFileNameChars()
	{
		return new char[41]
		{
			'"', '<', '>', '|', '\0', '\u0001', '\u0002', '\u0003', '\u0004', '\u0005',
			'\u0006', '\a', '\b', '\t', '\n', '\v', '\f', '\r', '\u000e', '\u000f',
			'\u0010', '\u0011', '\u0012', '\u0013', '\u0014', '\u0015', '\u0016', '\u0017', '\u0018', '\u0019',
			'\u001a', '\u001b', '\u001c', '\u001d', '\u001e', '\u001f', ':', '*', '?', '\\',
			'/'
		};
	}

	public static char[] GetInvalidPathChars()
	{
		return new char[33]
		{
			'|', '\0', '\u0001', '\u0002', '\u0003', '\u0004', '\u0005', '\u0006', '\a', '\b',
			'\t', '\n', '\v', '\f', '\r', '\u000e', '\u000f', '\u0010', '\u0011', '\u0012',
			'\u0013', '\u0014', '\u0015', '\u0016', '\u0017', '\u0018', '\u0019', '\u001a', '\u001b', '\u001c',
			'\u001d', '\u001e', '\u001f'
		};
	}

	public static string GetFullPath(string path)
	{
		if (path == null)
		{
			throw new ArgumentNullException("path");
		}
		if (PathInternal.IsEffectivelyEmpty(path.AsSpan()))
		{
			throw new ArgumentException(SR.Arg_PathEmpty, "path");
		}
		if (path.Contains('\0'))
		{
			throw new ArgumentException(SR.Argument_InvalidPathChars, "path");
		}
		return GetFullPathInternal(path);
	}

	public static string GetFullPath(string path, string basePath)
	{
		if (path == null)
		{
			throw new ArgumentNullException("path");
		}
		if (basePath == null)
		{
			throw new ArgumentNullException("basePath");
		}
		if (!IsPathFullyQualified(basePath))
		{
			throw new ArgumentException(SR.Arg_BasePathNotFullyQualified, "basePath");
		}
		if (basePath.Contains('\0') || path.Contains('\0'))
		{
			throw new ArgumentException(SR.Argument_InvalidPathChars);
		}
		if (IsPathFullyQualified(path))
		{
			return GetFullPathInternal(path);
		}
		if (PathInternal.IsEffectivelyEmpty(path.AsSpan()))
		{
			return basePath;
		}
		int length = path.Length;
		string text = ((length >= 1 && PathInternal.IsDirectorySeparator(path[0])) ? Join(GetPathRoot(basePath.AsSpan()), path.AsSpan(1)) : ((length < 2 || !PathInternal.IsValidDriveChar(path[0]) || path[1] != ':') ? JoinInternal(basePath.AsSpan(), path.AsSpan()) : ((!GetVolumeName(path.AsSpan()).EqualsOrdinal(GetVolumeName(basePath.AsSpan()))) ? ((!PathInternal.IsDevice(basePath.AsSpan())) ? path.Insert(2, "\\") : ((length == 2) ? JoinInternal(basePath.AsSpan(0, 4), path.AsSpan(), "\\".AsSpan()) : JoinInternal(basePath.AsSpan(0, 4), path.AsSpan(0, 2), "\\".AsSpan(), path.AsSpan(2)))) : Join(basePath.AsSpan(), path.AsSpan(2)))));
		if (!PathInternal.IsDevice(text.AsSpan()))
		{
			return GetFullPathInternal(text);
		}
		return PathInternal.RemoveRelativeSegments(text, PathInternal.GetRootLength(text.AsSpan()));
	}

	private static string GetFullPathInternal(string path)
	{
		if (PathInternal.IsExtended(path.AsSpan()))
		{
			return path;
		}
		return PathHelper.Normalize(path);
	}

	public static string GetTempPath()
	{
		Span<char> initialBuffer = stackalloc char[260];
		ValueStringBuilder builder = new ValueStringBuilder(initialBuffer);
		GetTempPath(ref builder);
		string result = PathHelper.Normalize(ref builder);
		builder.Dispose();
		return result;
	}

	private static void GetTempPath(ref ValueStringBuilder builder)
	{
		uint tempPathW;
		while ((tempPathW = Interop.Kernel32.GetTempPathW(builder.Capacity, ref builder.GetPinnableReference())) > builder.Capacity)
		{
			builder.EnsureCapacity(checked((int)tempPathW));
		}
		if (tempPathW == 0)
		{
			throw Win32Marshal.GetExceptionForLastWin32Error();
		}
		builder.Length = (int)tempPathW;
	}

	public static string GetTempFileName()
	{
		Span<char> initialBuffer = stackalloc char[260];
		ValueStringBuilder builder = new ValueStringBuilder(initialBuffer);
		GetTempPath(ref builder);
		initialBuffer = stackalloc char[260];
		ValueStringBuilder path = new ValueStringBuilder(initialBuffer);
		uint tempFileNameW = Interop.Kernel32.GetTempFileNameW(ref builder.GetPinnableReference(), "tmp", 0u, ref path.GetPinnableReference());
		builder.Dispose();
		if (tempFileNameW == 0)
		{
			throw Win32Marshal.GetExceptionForLastWin32Error();
		}
		path.Length = path.RawChars.IndexOf('\0');
		string result = PathHelper.Normalize(ref path);
		path.Dispose();
		return result;
	}

	public static bool IsPathRooted([NotNullWhen(true)] string? path)
	{
		if (path != null)
		{
			return IsPathRooted(path.AsSpan());
		}
		return false;
	}

	public static bool IsPathRooted(ReadOnlySpan<char> path)
	{
		int length = path.Length;
		if (length < 1 || !PathInternal.IsDirectorySeparator(path[0]))
		{
			if (length >= 2 && PathInternal.IsValidDriveChar(path[0]))
			{
				return path[1] == ':';
			}
			return false;
		}
		return true;
	}

	public static string? GetPathRoot(string? path)
	{
		if (PathInternal.IsEffectivelyEmpty(path.AsSpan()))
		{
			return null;
		}
		ReadOnlySpan<char> pathRoot = GetPathRoot(path.AsSpan());
		if (path.Length == pathRoot.Length)
		{
			return PathInternal.NormalizeDirectorySeparators(path);
		}
		return PathInternal.NormalizeDirectorySeparators(pathRoot.ToString());
	}

	public static ReadOnlySpan<char> GetPathRoot(ReadOnlySpan<char> path)
	{
		if (PathInternal.IsEffectivelyEmpty(path))
		{
			return ReadOnlySpan<char>.Empty;
		}
		int rootLength = PathInternal.GetRootLength(path);
		if (rootLength > 0)
		{
			return path.Slice(0, rootLength);
		}
		return ReadOnlySpan<char>.Empty;
	}

	internal static ReadOnlySpan<char> GetVolumeName(ReadOnlySpan<char> path)
	{
		ReadOnlySpan<char> pathRoot = GetPathRoot(path);
		if (pathRoot.Length == 0)
		{
			return pathRoot;
		}
		int num = GetUncRootLength(path);
		if (num == -1)
		{
			num = (PathInternal.IsDevice(path) ? 4 : 0);
		}
		ReadOnlySpan<char> readOnlySpan = pathRoot.Slice(num);
		if (!EndsInDirectorySeparator(readOnlySpan))
		{
			return readOnlySpan;
		}
		return readOnlySpan.Slice(0, readOnlySpan.Length - 1);
	}

	internal static int GetUncRootLength(ReadOnlySpan<char> path)
	{
		bool flag = PathInternal.IsDevice(path);
		if (!flag && path.Slice(0, 2).EqualsOrdinal("\\\\".AsSpan()))
		{
			return 2;
		}
		if (flag && path.Length >= 8 && (path.Slice(0, 8).EqualsOrdinal("\\\\?\\UNC\\".AsSpan()) || path.Slice(5, 4).EqualsOrdinal("UNC\\".AsSpan())))
		{
			return 8;
		}
		return -1;
	}
}
