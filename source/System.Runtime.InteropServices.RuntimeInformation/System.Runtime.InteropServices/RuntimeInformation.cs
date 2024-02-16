using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices;

public static class RuntimeInformation
{
	private static string s_frameworkDescription;

	private static string s_runtimeIdentifier;

	private static string s_osDescription;

	private static volatile int s_osArch = -1;

	private static volatile int s_processArch = -1;

	public static string FrameworkDescription
	{
		get
		{
			if (s_frameworkDescription == null)
			{
				ReadOnlySpan<char> readOnlySpan = typeof(object).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
				int num = readOnlySpan.IndexOf('+');
				if (num != -1)
				{
					readOnlySpan = readOnlySpan.Slice(0, num);
				}
				s_frameworkDescription = ((!readOnlySpan.Trim().IsEmpty) ? $"{".NET"} {readOnlySpan}" : ".NET");
			}
			return s_frameworkDescription;
		}
	}

	public static string RuntimeIdentifier
	{
		get
		{
			object obj = s_runtimeIdentifier;
			if (obj == null)
			{
				obj = (AppContext.GetData("RUNTIME_IDENTIFIER") as string) ?? "unknown";
				s_runtimeIdentifier = (string)obj;
			}
			return (string)obj;
		}
	}

	public static string OSDescription
	{
		get
		{
			string text = s_osDescription;
			if (text == null)
			{
				OperatingSystem oSVersion = Environment.OSVersion;
				Version version = oSVersion.Version;
				Span<char> span = stackalloc char[256];
				string text2;
				if (!string.IsNullOrEmpty(oSVersion.ServicePack))
				{
					IFormatProvider formatProvider = null;
					IFormatProvider provider = formatProvider;
					Span<char> span2 = span;
					Span<char> initialBuffer = span2;
					DefaultInterpolatedStringHandler handler = new DefaultInterpolatedStringHandler(4, 5, formatProvider, span2);
					handler.AppendFormatted("Microsoft Windows");
					handler.AppendLiteral(" ");
					handler.AppendFormatted((uint)version.Major);
					handler.AppendLiteral(".");
					handler.AppendFormatted((uint)version.Minor);
					handler.AppendLiteral(".");
					handler.AppendFormatted((uint)version.Build);
					handler.AppendLiteral(" ");
					handler.AppendFormatted(oSVersion.ServicePack);
					text2 = string.Create(provider, initialBuffer, ref handler);
				}
				else
				{
					IFormatProvider formatProvider = null;
					IFormatProvider provider2 = formatProvider;
					Span<char> span2 = span;
					Span<char> initialBuffer2 = span2;
					DefaultInterpolatedStringHandler handler2 = new DefaultInterpolatedStringHandler(3, 4, formatProvider, span2);
					handler2.AppendFormatted("Microsoft Windows");
					handler2.AppendLiteral(" ");
					handler2.AppendFormatted((uint)version.Major);
					handler2.AppendLiteral(".");
					handler2.AppendFormatted((uint)version.Minor);
					handler2.AppendLiteral(".");
					handler2.AppendFormatted((uint)version.Build);
					text2 = string.Create(provider2, initialBuffer2, ref handler2);
				}
				text = text2;
				s_osDescription = text2;
			}
			return text;
		}
	}

	public static Architecture OSArchitecture
	{
		get
		{
			int num = s_osArch;
			if (num == -1)
			{
				global::Interop.Kernel32.GetNativeSystemInfo(out var lpSystemInfo);
				num = (s_osArch = (int)Map((global::Interop.Kernel32.ProcessorArchitecture)lpSystemInfo.wProcessorArchitecture));
			}
			return (Architecture)num;
		}
	}

	public static Architecture ProcessArchitecture
	{
		get
		{
			int num = s_processArch;
			if (num == -1)
			{
				global::Interop.Kernel32.GetSystemInfo(out var lpSystemInfo);
				num = (s_processArch = (int)Map((global::Interop.Kernel32.ProcessorArchitecture)lpSystemInfo.wProcessorArchitecture));
			}
			return (Architecture)num;
		}
	}

	public static bool IsOSPlatform(OSPlatform osPlatform)
	{
		return OperatingSystem.IsOSPlatform(osPlatform.Name);
	}

	private static Architecture Map(global::Interop.Kernel32.ProcessorArchitecture processorArchitecture)
	{
		return processorArchitecture switch
		{
			global::Interop.Kernel32.ProcessorArchitecture.Processor_Architecture_ARM64 => Architecture.Arm64, 
			global::Interop.Kernel32.ProcessorArchitecture.Processor_Architecture_ARM => Architecture.Arm, 
			global::Interop.Kernel32.ProcessorArchitecture.Processor_Architecture_AMD64 => Architecture.X64, 
			_ => Architecture.X86, 
		};
	}
}
