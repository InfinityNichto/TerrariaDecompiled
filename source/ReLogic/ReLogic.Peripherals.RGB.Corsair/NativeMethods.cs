using System;
using System.Runtime.InteropServices;

namespace ReLogic.Peripherals.RGB.Corsair;

internal class NativeMethods
{
	public const string DLL_NAME = "CUESDK.x64_2019.dll";

	[DllImport("CUESDK.x64_2019.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
	[return: MarshalAs(UnmanagedType.I1)]
	public static extern bool CorsairSetLedsColors(int size, [In][Out] CorsairLedColor[] ledsColors);

	[DllImport("CUESDK.x64_2019.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
	[return: MarshalAs(UnmanagedType.I1)]
	public static extern bool CorsairSetLedsColorsAsync(int size, [In][Out] CorsairLedColor[] ledsColors, IntPtr callback, IntPtr context);

	[DllImport("CUESDK.x64_2019.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
	public static extern int CorsairGetDeviceCount();

	[DllImport("CUESDK.x64_2019.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
	public static extern IntPtr CorsairGetDeviceInfo(int deviceIndex);

	[DllImport("CUESDK.x64_2019.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
	public static extern IntPtr CorsairGetLedPositions();

	[DllImport("CUESDK.x64_2019.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
	public static extern IntPtr CorsairGetLedPositionsByDeviceIndex(int deviceIndex);

	[DllImport("CUESDK.x64_2019.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
	public static extern bool CorsairRequestControl(CorsairAccessMode accessMode);

	[DllImport("CUESDK.x64_2019.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
	public static extern CorsairProtocolDetails CorsairPerformProtocolHandshake();

	[DllImport("CUESDK.x64_2019.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
	public static extern CorsairError CorsairGetLastError();

	[DllImport("CUESDK.x64_2019.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
	public static extern bool CorsairReleaseControl(CorsairAccessMode accessMode);
}
