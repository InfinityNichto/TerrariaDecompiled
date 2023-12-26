using System;
using System.Runtime.InteropServices;

namespace ReLogic.Peripherals.RGB.Razer;

internal class NativeMethods
{
	public enum KeyboardEffectType
	{
		None,
		Breathing,
		Custom,
		Reactive,
		Static,
		Spectrumcycling,
		Wave,
		Reserved,
		CustomKey,
		Invalid
	}

	public struct CustomKeyboardEffect
	{
		public const int Rows = 6;

		public const int Columns = 22;

		public const int MaxKeys = 132;

		public const uint KeyFlag = 16777216u;

		public const uint ColorMask = 16777215u;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 132)]
		public readonly uint[] Color;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 132)]
		public readonly uint[] Key;

		public static CustomKeyboardEffect Create()
		{
			return new CustomKeyboardEffect(132);
		}

		private CustomKeyboardEffect(int size)
		{
			Color = new uint[size];
			Key = new uint[size];
		}
	}

	public enum MouseEffectType
	{
		None,
		Blinking,
		Breathing,
		Custom,
		Reactive,
		Spectrumcycling,
		Static,
		Wave,
		Custom2,
		Invalid
	}

	public struct CustomMouseEffect
	{
		public const int Rows = 9;

		public const int Columns = 7;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 63)]
		public readonly uint[] Color;

		public static CustomMouseEffect Create()
		{
			return new CustomMouseEffect(63);
		}

		private CustomMouseEffect(int size)
		{
			Color = new uint[size];
		}
	}

	public enum HeadsetEffectType
	{
		None,
		Static,
		Breathing,
		Spectrumcycling,
		Custom,
		Invalid
	}

	public struct CustomHeadsetEffect
	{
		public const int Leds = 5;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
		public readonly uint[] Color;

		public static CustomHeadsetEffect Create()
		{
			return new CustomHeadsetEffect(5);
		}

		private CustomHeadsetEffect(int size)
		{
			Color = new uint[size];
		}
	}

	public enum MousepadEffectType
	{
		None,
		Breathing,
		Custom,
		Spectrumcycling,
		Static,
		Wave,
		Invalid
	}

	public struct CustomMousepadEffect
	{
		public const int Leds = 15;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
		public readonly uint[] Color;

		public static CustomMousepadEffect Create()
		{
			return new CustomMousepadEffect(15);
		}

		private CustomMousepadEffect(int size)
		{
			Color = new uint[size];
		}
	}

	public enum KeypadEffectType
	{
		None,
		Breathing,
		Custom,
		Reactive,
		Spectrumcycling,
		Static,
		Wave,
		Invalid
	}

	public struct CustomKeypadEffect
	{
		public const int Rows = 4;

		public const int Columns = 5;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
		public readonly uint[] Color;

		public static CustomKeypadEffect Create()
		{
			return new CustomKeypadEffect(20);
		}

		private CustomKeypadEffect(int size)
		{
			Color = new uint[size];
		}
	}

	public enum ChromaLinkEffectType
	{
		None,
		Custom,
		Static,
		Invalid
	}

	public struct CustomChromaLinkEffect
	{
		public const int Leds = 5;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
		public readonly uint[] Color;

		public static CustomChromaLinkEffect Create()
		{
			return new CustomChromaLinkEffect(5);
		}

		private CustomChromaLinkEffect(int size)
		{
			Color = new uint[size];
		}
	}

	public const string DLL_NAME = "RzChromaSDK64.dll";

	[DllImport("RzChromaSDK64.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern RzResult Init();

	[DllImport("RzChromaSDK64.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern RzResult UnInit();

	[DllImport("RzChromaSDK64.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern RzResult CreateKeyboardEffect(KeyboardEffectType effect, ref CustomKeyboardEffect effectData, ref Guid effectId);

	[DllImport("RzChromaSDK64.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern RzResult CreateMouseEffect(MouseEffectType effect, ref CustomMouseEffect effectData, ref Guid effectId);

	[DllImport("RzChromaSDK64.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern RzResult CreateHeadsetEffect(HeadsetEffectType effect, ref CustomHeadsetEffect effectData, ref Guid effectId);

	[DllImport("RzChromaSDK64.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern RzResult CreateMousepadEffect(MousepadEffectType effect, ref CustomMousepadEffect effectData, ref Guid effectId);

	[DllImport("RzChromaSDK64.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern RzResult CreateKeypadEffect(KeypadEffectType effect, ref CustomKeypadEffect effectData, ref Guid effectId);

	[DllImport("RzChromaSDK64.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern RzResult CreateChromaLinkEffect(ChromaLinkEffectType effect, ref CustomChromaLinkEffect effectData, ref Guid effectId);

	[DllImport("RzChromaSDK64.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern RzResult DeleteEffect(Guid effectId);

	[DllImport("RzChromaSDK64.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	public static extern RzResult SetEffect(Guid effectId);
}
