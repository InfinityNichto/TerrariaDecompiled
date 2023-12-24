using System.Runtime.InteropServices;

namespace ReLogic.Peripherals.RGB.Logitech;

internal class NativeMethods
{
	private const int LOGI_DEVICETYPE_MONOCHROME_ORD = 0;

	private const int LOGI_DEVICETYPE_RGB_ORD = 1;

	private const int LOGI_DEVICETYPE_PERKEY_RGB_ORD = 2;

	public const int LOGI_DEVICETYPE_MONOCHROME = 1;

	public const int LOGI_DEVICETYPE_RGB = 2;

	public const int LOGI_DEVICETYPE_PERKEY_RGB = 4;

	public const int LOGI_LED_BITMAP_WIDTH = 21;

	public const int LOGI_LED_BITMAP_HEIGHT = 6;

	public const int LOGI_LED_BITMAP_BYTES_PER_KEY = 4;

	public const int LOGI_LED_BITMAP_SIZE = 504;

	public const int LOGI_LED_DURATION_INFINITE = 0;

	public const string DLL_NAME = "LogitechLedEnginesWrapper ";

	[DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
	public static extern bool LogiLedInit();

	[DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
	public static extern bool LogiLedGetConfigOptionNumber([MarshalAs(UnmanagedType.LPWStr)] string configPath, ref double defaultNumber);

	[DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
	public static extern bool LogiLedGetConfigOptionBool([MarshalAs(UnmanagedType.LPWStr)] string configPath, ref bool defaultRed);

	[DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
	public static extern bool LogiLedGetConfigOptionColor([MarshalAs(UnmanagedType.LPWStr)] string configPath, ref int defaultRed, ref int defaultGreen, ref int defaultBlue);

	[DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
	public static extern bool LogiLedSetTargetDevice(int targetDevice);

	[DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
	public static extern bool LogiLedGetSdkVersion(ref int majorNum, ref int minorNum, ref int buildNum);

	[DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
	public static extern bool LogiLedSaveCurrentLighting();

	[DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
	public static extern bool LogiLedSetLighting(int redPercentage, int greenPercentage, int bluePercentage);

	[DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
	public static extern bool LogiLedRestoreLighting();

	[DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
	public static extern bool LogiLedFlashLighting(int redPercentage, int greenPercentage, int bluePercentage, int milliSecondsDuration, int milliSecondsInterval);

	[DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
	public static extern bool LogiLedPulseLighting(int redPercentage, int greenPercentage, int bluePercentage, int milliSecondsDuration, int milliSecondsInterval);

	[DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
	public static extern bool LogiLedStopEffects();

	[DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
	public static extern bool LogiLedExcludeKeysFromBitmap(KeyName[] keyList, int listCount);

	[DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
	public static extern bool LogiLedSetLightingFromBitmap(byte[] bitmap);

	[DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
	public static extern bool LogiLedSetLightingForKeyWithScanCode(int keyCode, int redPercentage, int greenPercentage, int bluePercentage);

	[DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
	public static extern bool LogiLedSetLightingForKeyWithHidCode(int keyCode, int redPercentage, int greenPercentage, int bluePercentage);

	[DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
	public static extern bool LogiLedSetLightingForKeyWithQuartzCode(int keyCode, int redPercentage, int greenPercentage, int bluePercentage);

	[DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
	public static extern bool LogiLedSetLightingForKeyWithKeyName(KeyName keyCode, int redPercentage, int greenPercentage, int bluePercentage);

	[DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
	public static extern bool LogiLedSaveLightingForKey(KeyName keyName);

	[DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
	public static extern bool LogiLedRestoreLightingForKey(KeyName keyName);

	[DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
	public static extern bool LogiLedFlashSingleKey(KeyName keyName, int redPercentage, int greenPercentage, int bluePercentage, int msDuration, int msInterval);

	[DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
	public static extern bool LogiLedPulseSingleKey(KeyName keyName, int startRedPercentage, int startGreenPercentage, int startBluePercentage, int finishRedPercentage, int finishGreenPercentage, int finishBluePercentage, int msDuration, bool isInfinite);

	[DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
	public static extern bool LogiLedStopEffectsOnKey(KeyName keyName);

	[DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
	public static extern void LogiLedShutdown();
}
