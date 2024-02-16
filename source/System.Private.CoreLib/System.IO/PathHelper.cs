using System.Runtime.InteropServices;
using System.Text;

namespace System.IO;

internal static class PathHelper
{
	internal static string Normalize(string path)
	{
		Span<char> initialBuffer = stackalloc char[260];
		ValueStringBuilder builder = new ValueStringBuilder(initialBuffer);
		GetFullPathName(path.AsSpan(), ref builder);
		string result = ((builder.AsSpan().IndexOf('~') >= 0) ? TryExpandShortFileName(ref builder, path) : (MemoryExtensions.Equals(builder.AsSpan(), path.AsSpan(), StringComparison.Ordinal) ? path : builder.ToString()));
		builder.Dispose();
		return result;
	}

	internal static string Normalize(ref ValueStringBuilder path)
	{
		Span<char> initialBuffer = stackalloc char[260];
		ValueStringBuilder builder = new ValueStringBuilder(initialBuffer);
		GetFullPathName(path.AsSpan(terminate: true), ref builder);
		string result = ((builder.AsSpan().IndexOf('~') >= 0) ? TryExpandShortFileName(ref builder, null) : builder.ToString());
		builder.Dispose();
		return result;
	}

	private static void GetFullPathName(ReadOnlySpan<char> path, ref ValueStringBuilder builder)
	{
		uint fullPathNameW;
		while ((fullPathNameW = Interop.Kernel32.GetFullPathNameW(ref MemoryMarshal.GetReference(path), (uint)builder.Capacity, ref builder.GetPinnableReference(), IntPtr.Zero)) > builder.Capacity)
		{
			builder.EnsureCapacity(checked((int)fullPathNameW));
		}
		if (fullPathNameW == 0)
		{
			int num = Marshal.GetLastWin32Error();
			if (num == 0)
			{
				num = 161;
			}
			throw Win32Marshal.GetExceptionForWin32Error(num, path.ToString());
		}
		builder.Length = (int)fullPathNameW;
	}

	internal static int PrependDevicePathChars(ref ValueStringBuilder content, bool isDosUnc, ref ValueStringBuilder buffer)
	{
		int length = content.Length;
		length += (isDosUnc ? 6 : 4);
		buffer.EnsureCapacity(length + 1);
		buffer.Length = 0;
		if (isDosUnc)
		{
			buffer.Append("\\\\?\\UNC\\");
			buffer.Append(content.AsSpan(2));
			return 6;
		}
		buffer.Append("\\\\?\\");
		buffer.Append(content.AsSpan());
		return 4;
	}

	internal static string TryExpandShortFileName(ref ValueStringBuilder outputBuilder, string originalPath)
	{
		int rootLength = PathInternal.GetRootLength(outputBuilder.AsSpan());
		bool flag = PathInternal.IsDevice(outputBuilder.AsSpan());
		ValueStringBuilder buffer = default(ValueStringBuilder);
		bool flag2 = false;
		int num = 0;
		bool flag3 = false;
		if (flag)
		{
			buffer.Append(outputBuilder.AsSpan());
			if (outputBuilder[2] == '.')
			{
				flag3 = true;
				buffer[2] = '?';
			}
		}
		else
		{
			flag2 = !PathInternal.IsDevice(outputBuilder.AsSpan()) && outputBuilder.Length > 1 && outputBuilder[0] == '\\' && outputBuilder[1] == '\\';
			num = PrependDevicePathChars(ref outputBuilder, flag2, ref buffer);
		}
		rootLength += num;
		int length = buffer.Length;
		bool flag4 = false;
		int num2 = buffer.Length - 1;
		while (!flag4)
		{
			uint longPathNameW = Interop.Kernel32.GetLongPathNameW(ref buffer.GetPinnableReference(terminate: true), ref outputBuilder.GetPinnableReference(), (uint)outputBuilder.Capacity);
			if (buffer[num2] == '\0')
			{
				buffer[num2] = '\\';
			}
			if (longPathNameW == 0)
			{
				int lastWin32Error = Marshal.GetLastWin32Error();
				if (lastWin32Error != 2 && lastWin32Error != 3)
				{
					break;
				}
				num2--;
				while (num2 > rootLength && buffer[num2] != '\\')
				{
					num2--;
				}
				if (num2 == rootLength)
				{
					break;
				}
				buffer[num2] = '\0';
			}
			else if (longPathNameW > outputBuilder.Capacity)
			{
				outputBuilder.EnsureCapacity(checked((int)longPathNameW));
			}
			else
			{
				flag4 = true;
				outputBuilder.Length = checked((int)longPathNameW);
				if (num2 < length - 1)
				{
					outputBuilder.Append(buffer.AsSpan(num2, buffer.Length - num2));
				}
			}
		}
		ref ValueStringBuilder reference = ref flag4 ? ref outputBuilder : ref buffer;
		if (flag3)
		{
			reference[2] = '.';
		}
		if (flag2)
		{
			reference[6] = '\\';
		}
		ReadOnlySpan<char> span = reference.AsSpan(num);
		string result = ((originalPath != null && MemoryExtensions.Equals(span, originalPath.AsSpan(), StringComparison.Ordinal)) ? originalPath : span.ToString());
		buffer.Dispose();
		return result;
	}
}
