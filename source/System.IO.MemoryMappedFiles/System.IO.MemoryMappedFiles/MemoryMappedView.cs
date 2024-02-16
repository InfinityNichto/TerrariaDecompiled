using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace System.IO.MemoryMappedFiles;

internal sealed class MemoryMappedView : IDisposable
{
	private readonly SafeMemoryMappedViewHandle _viewHandle;

	private readonly long _pointerOffset;

	private readonly long _size;

	private readonly MemoryMappedFileAccess _access;

	public SafeMemoryMappedViewHandle ViewHandle => _viewHandle;

	public long PointerOffset => _pointerOffset;

	public long Size => _size;

	public MemoryMappedFileAccess Access => _access;

	public bool IsClosed => _viewHandle.IsClosed;

	private MemoryMappedView(SafeMemoryMappedViewHandle viewHandle, long pointerOffset, long size, MemoryMappedFileAccess access)
	{
		_viewHandle = viewHandle;
		_pointerOffset = pointerOffset;
		_size = size;
		_access = access;
	}

	private void Dispose(bool disposing)
	{
		if (!_viewHandle.IsClosed)
		{
			_viewHandle.Dispose();
		}
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	private static void ValidateSizeAndOffset(long size, long offset, long allocationGranularity, out ulong newSize, out long extraMemNeeded, out long newOffset)
	{
		extraMemNeeded = offset % allocationGranularity;
		newOffset = offset - extraMemNeeded;
		newSize = (ulong)((size != 0L) ? (size + extraMemNeeded) : 0);
		if (IntPtr.Size == 4 && newSize > uint.MaxValue)
		{
			throw new ArgumentOutOfRangeException("size", System.SR.ArgumentOutOfRange_CapacityLargerThanLogicalAddressSpaceNotAllowed);
		}
	}

	public static MemoryMappedView CreateView(SafeMemoryMappedFileHandle memMappedFileHandle, MemoryMappedFileAccess access, long offset, long size)
	{
		ValidateSizeAndOffset(size, offset, GetSystemPageAllocationGranularity(), out var newSize, out var extraMemNeeded, out var newOffset);
		global::Interop.CheckForAvailableVirtualMemory(newSize);
		SafeMemoryMappedViewHandle safeMemoryMappedViewHandle = global::Interop.MapViewOfFile(memMappedFileHandle, MemoryMappedFile.GetFileMapAccess(access), newOffset, new UIntPtr(newSize));
		if (safeMemoryMappedViewHandle.IsInvalid)
		{
			safeMemoryMappedViewHandle.Dispose();
			throw System.IO.Win32Marshal.GetExceptionForLastWin32Error();
		}
		global::Interop.Kernel32.MEMORY_BASIC_INFORMATION lpBuffer = default(global::Interop.Kernel32.MEMORY_BASIC_INFORMATION);
		global::Interop.Kernel32.VirtualQuery(safeMemoryMappedViewHandle, ref lpBuffer, (UIntPtr)(ulong)Marshal.SizeOf(lpBuffer));
		ulong num = (ulong)lpBuffer.RegionSize;
		if ((lpBuffer.State & 0x2000u) != 0 || num < newSize)
		{
			IntPtr intPtr = global::Interop.VirtualAlloc(safeMemoryMappedViewHandle, (UIntPtr)((newSize != 0L) ? newSize : num), 4096, MemoryMappedFile.GetPageAccess(access));
			int lastPInvokeError = Marshal.GetLastPInvokeError();
			if (safeMemoryMappedViewHandle.IsInvalid)
			{
				safeMemoryMappedViewHandle.Dispose();
				throw System.IO.Win32Marshal.GetExceptionForWin32Error(lastPInvokeError);
			}
			lpBuffer = default(global::Interop.Kernel32.MEMORY_BASIC_INFORMATION);
			global::Interop.Kernel32.VirtualQuery(safeMemoryMappedViewHandle, ref lpBuffer, (UIntPtr)(ulong)Marshal.SizeOf(lpBuffer));
			num = (ulong)lpBuffer.RegionSize;
		}
		if (size == 0L)
		{
			size = (long)num - extraMemNeeded;
		}
		safeMemoryMappedViewHandle.Initialize((ulong)(size + extraMemNeeded));
		return new MemoryMappedView(safeMemoryMappedViewHandle, extraMemNeeded, size, access);
	}

	public unsafe void Flush(UIntPtr capacity)
	{
		byte* pointer = null;
		try
		{
			_viewHandle.AcquirePointer(ref pointer);
			if (global::Interop.Kernel32.FlushViewOfFile((IntPtr)pointer, capacity))
			{
				return;
			}
			int lastPInvokeError = Marshal.GetLastPInvokeError();
			if (lastPInvokeError != 33)
			{
				throw System.IO.Win32Marshal.GetExceptionForWin32Error(lastPInvokeError);
			}
			SpinWait spinWait = default(SpinWait);
			for (int i = 0; i < 15; i++)
			{
				int millisecondsTimeout = 1 << i;
				Thread.Sleep(millisecondsTimeout);
				for (int j = 0; j < 20; j++)
				{
					if (global::Interop.Kernel32.FlushViewOfFile((IntPtr)pointer, capacity))
					{
						return;
					}
					lastPInvokeError = Marshal.GetLastPInvokeError();
					if (lastPInvokeError != 33)
					{
						throw System.IO.Win32Marshal.GetExceptionForWin32Error(lastPInvokeError);
					}
					spinWait.SpinOnce();
				}
			}
			throw System.IO.Win32Marshal.GetExceptionForWin32Error(lastPInvokeError);
		}
		finally
		{
			if (pointer != null)
			{
				_viewHandle.ReleasePointer();
			}
		}
	}

	private static int GetSystemPageAllocationGranularity()
	{
		global::Interop.Kernel32.GetSystemInfo(out var lpSystemInfo);
		return lpSystemInfo.dwAllocationGranularity;
	}
}
