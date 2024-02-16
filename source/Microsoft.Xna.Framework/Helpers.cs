using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.Audio;

namespace Microsoft.Xna.Framework;

internal static class Helpers
{
	public const uint InvalidHandle = uint.MaxValue;

	public const int MaximumStringLength = 260;

	public const int Guide_MessageBox_MaxButtons = 3;

	public const int MaxNumberOfSignedInPlayers = 1;

	internal const int PlayerAnyIndex = 255;

	private static object KeepKernelReturnCode = typeof(KernelReturnCode);

	public static bool Succeeded(int error)
	{
		return error >= 0;
	}

	public static bool Succeeded(ErrorCodes error)
	{
		return (int)error >= 0;
	}

	public static bool Failed(int error)
	{
		return error < 0;
	}

	public static bool Failed(ErrorCodes error)
	{
		return (int)error < 0;
	}

	public unsafe static int SmartGetHashCode(object obj)
	{
		GCHandle gCHandle = GCHandle.Alloc(obj, GCHandleType.Pinned);
		try
		{
			int num = Marshal.SizeOf(obj);
			int num2 = 0;
			int num3 = 0;
			int* ptr = (int*)gCHandle.AddrOfPinnedObject().ToPointer();
			while (num2 + 4 <= num)
			{
				num3 ^= *ptr;
				num2 += 4;
				ptr++;
			}
			return (num3 == 0) ? int.MaxValue : num3;
		}
		finally
		{
			gCHandle.Free();
		}
	}

	public static void ValidateCopyParameters(int dataLength, int dataIndex, int elementCount)
	{
		if (dataIndex < 0 || dataIndex > dataLength)
		{
			throw new ArgumentOutOfRangeException("dataIndex", FrameworkResources.MustBeValidIndex);
		}
		if (elementCount + dataIndex > dataLength)
		{
			throw new ArgumentOutOfRangeException("elementCount", FrameworkResources.MustBeValidIndex);
		}
		if (elementCount <= 0)
		{
			throw new ArgumentOutOfRangeException("elementCount", FrameworkResources.MustBeValidIndex);
		}
	}

	public static uint GetSizeOf<T>() where T : struct
	{
		return (uint)Marshal.SizeOf(typeof(T));
	}

	public static void ThrowExceptionFromErrorCode(ErrorCodes error)
	{
		if (Failed(error))
		{
			throw GetExceptionFromResult((uint)error);
		}
	}

	public static void ThrowExceptionFromErrorCode(int error)
	{
		if (Failed(error))
		{
			throw GetExceptionFromResult((uint)error);
		}
	}

	public static void ThrowExceptionFromResult(uint result)
	{
		if (result == 0)
		{
			return;
		}
		throw GetExceptionFromResult(result);
	}

	[SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes")]
	[SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly")]
	[SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
	public static Exception GetExceptionFromResult(uint result)
	{
		if (result == 0)
		{
			return null;
		}
		switch (result)
		{
		case 2289436696u:
			return new ArgumentException(FrameworkResources.WrongTextureFormat);
		case 2289436774u:
			return new ArgumentException(FrameworkResources.NotFound);
		case 2289436775u:
			return new ArgumentException(FrameworkResources.MoreData);
		case 2289436701u:
			return new InvalidOperationException(FrameworkResources.TooManyOperations);
		case 2289436786u:
			return new InvalidOperationException(FrameworkResources.InvalidCall);
		case 2328297475u:
			return new InvalidOperationException(FrameworkResources.Expired);
		case 2328297478u:
			return new InvalidOperationException(FrameworkResources.InvalidUsage);
		case 2328297494u:
			return new InvalidOperationException(FrameworkResources.WaveBankNotPrepared);
		case 2328297482u:
			return new IndexOutOfRangeException(FrameworkResources.InvalidVariableIndex);
		case 2328297485u:
			return new IndexOutOfRangeException(FrameworkResources.InvalidWaveIndex);
		case 2328297486u:
			return new IndexOutOfRangeException(FrameworkResources.InvalidTrackIndex);
		case 2328297487u:
			return new IndexOutOfRangeException(FrameworkResources.InvalidSoundOffsetOrIndex);
		case 2328297488u:
			return new IOException(FrameworkResources.XactReadFile);
		case 2328297479u:
			return new ArgumentException(FrameworkResources.InvalidContentVersion);
		case 2328297483u:
			return new ArgumentException(FrameworkResources.InvalidCategory);
		case 2328297484u:
			return new IndexOutOfRangeException(FrameworkResources.InvalidCue);
		case 2328297490u:
			return new InvalidOperationException(FrameworkResources.InCallback);
		case 2328297491u:
			return new InvalidOperationException(FrameworkResources.NoWaveBank);
		case 2328297492u:
			return new InvalidOperationException(FrameworkResources.SelectVariation);
		case 2328297495u:
			return new NoAudioHardwareException();
		case 2328297496u:
			return new ArgumentException(FrameworkResources.InvalidEntryCount);
		case 2328297480u:
		case 2343370753u:
			return new InstancePlayLimitException(FrameworkResources.InstancePlayFailedDueToLimit);
		case 2147746390u:
			return new InvalidOperationException(FrameworkResources.NoAudioPlaybackDevicesFound);
		case 2364407809u:
			return new NoMicrophoneConnectedException();
		case 2147500033u:
			return new NotImplementedException();
		case 2147942487u:
			return new ArgumentException();
		case 2147942405u:
			return new UnauthorizedAccessException();
		case 2147942414u:
			throw new OutOfMemoryException();
		case 2147500036u:
			throw new InvalidOperationException(FrameworkResources.ResourceInUse);
		default:
			return new InvalidOperationException(FrameworkResources.UnexpectedError);
		}
	}

	[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
	public static void CheckDisposed(object obj, IntPtr pComPtr)
	{
		if (pComPtr == IntPtr.Zero)
		{
			throw new ObjectDisposedException(obj.GetType().Name);
		}
	}

	public static void ValidateOrientation(DisplayOrientation orientation)
	{
		if (orientation != 0 && orientation != DisplayOrientation.LandscapeLeft && orientation != DisplayOrientation.LandscapeRight && orientation != DisplayOrientation.Portrait)
		{
			throw new ArgumentException(FrameworkResources.InvalidDisplayOrientation);
		}
	}

	public static bool IsLandscape(DisplayOrientation orientation)
	{
		return (orientation & (DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight)) != 0;
	}

	public static DisplayOrientation ChooseOrientation(DisplayOrientation orientation, int width, int height, bool allowLandscapeLeftAndRight)
	{
		if ((orientation & (DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight | DisplayOrientation.Portrait)) != 0)
		{
			return orientation;
		}
		if (width > height)
		{
			if (allowLandscapeLeftAndRight)
			{
				return DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight;
			}
			return DisplayOrientation.LandscapeLeft;
		}
		return DisplayOrientation.Portrait;
	}
}
