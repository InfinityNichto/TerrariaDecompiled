using System.IO;
using System.Runtime.ConstrainedExecution;
using System.Threading;

namespace System.Runtime;

public sealed class MemoryFailPoint : CriticalFinalizerObject, IDisposable
{
	private static readonly ulong s_topOfMemory = GetTopOfMemory();

	private static long s_hiddenLastKnownFreeAddressSpace;

	private static long s_hiddenLastTimeCheckingAddressSpace;

	private static readonly ulong s_GCSegmentSize = GC.GetSegmentSize();

	private static long s_failPointReservedMemory;

	private readonly ulong _reservedMemory;

	private bool _mustSubtractReservation;

	private static long LastKnownFreeAddressSpace
	{
		get
		{
			return Volatile.Read(ref s_hiddenLastKnownFreeAddressSpace);
		}
		set
		{
			Volatile.Write(ref s_hiddenLastKnownFreeAddressSpace, value);
		}
	}

	private static long LastTimeCheckingAddressSpace
	{
		get
		{
			return Volatile.Read(ref s_hiddenLastTimeCheckingAddressSpace);
		}
		set
		{
			Volatile.Write(ref s_hiddenLastTimeCheckingAddressSpace, value);
		}
	}

	internal static ulong MemoryFailPointReservedMemory => (ulong)Volatile.Read(ref s_failPointReservedMemory);

	private static void AddToLastKnownFreeAddressSpace(long addend)
	{
		Interlocked.Add(ref s_hiddenLastKnownFreeAddressSpace, addend);
	}

	public MemoryFailPoint(int sizeInMegabytes)
	{
		if (sizeInMegabytes <= 0)
		{
			throw new ArgumentOutOfRangeException("sizeInMegabytes", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		ulong num = (_reservedMemory = (ulong)((long)sizeInMegabytes << 20));
		ulong num2 = (ulong)(Math.Ceiling((double)num / (double)s_GCSegmentSize) * (double)s_GCSegmentSize);
		if (num2 >= s_topOfMemory)
		{
			throw new InsufficientMemoryException(SR.InsufficientMemory_MemFailPoint_TooBig);
		}
		ulong num3 = (ulong)(Math.Ceiling((double)sizeInMegabytes / 16.0) * 16.0);
		num3 <<= 20;
		for (int i = 0; i < 3; i++)
		{
			if (!CheckForAvailableMemory(out var availPageFile, out var totalAddressSpaceFree))
			{
				return;
			}
			ulong memoryFailPointReservedMemory = MemoryFailPointReservedMemory;
			ulong num4 = num2 + memoryFailPointReservedMemory;
			bool flag = num4 < num2 || num4 < memoryFailPointReservedMemory;
			bool flag2 = availPageFile < num3 + memoryFailPointReservedMemory + 16777216 || flag;
			bool flag3 = totalAddressSpaceFree < num4 || flag;
			long num5 = Environment.TickCount;
			if (num5 > LastTimeCheckingAddressSpace + 10000 || num5 < LastTimeCheckingAddressSpace || LastKnownFreeAddressSpace < (long)num2)
			{
				CheckForFreeAddressSpace(num2, shouldThrow: false);
			}
			bool flag4 = (ulong)LastKnownFreeAddressSpace < num2;
			if (!flag2 && !flag3 && !flag4)
			{
				break;
			}
			switch (i)
			{
			case 0:
				GC.Collect();
				break;
			case 1:
				if (flag2)
				{
					UIntPtr numBytes = new UIntPtr(num2);
					GrowPageFileIfNecessaryAndPossible(numBytes);
				}
				break;
			case 2:
				if (flag2 || flag3)
				{
					InsufficientMemoryException ex = new InsufficientMemoryException(SR.InsufficientMemory_MemFailPoint);
					throw ex;
				}
				if (flag4)
				{
					InsufficientMemoryException ex2 = new InsufficientMemoryException(SR.InsufficientMemory_MemFailPoint_VAFrag);
					throw ex2;
				}
				break;
			}
		}
		AddToLastKnownFreeAddressSpace((long)(0L - num));
		if (LastKnownFreeAddressSpace < 0)
		{
			CheckForFreeAddressSpace(num2, shouldThrow: true);
		}
		AddMemoryFailPointReservation((long)num);
		_mustSubtractReservation = true;
	}

	~MemoryFailPoint()
	{
		Dispose(disposing: false);
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	private void Dispose(bool disposing)
	{
		if (_mustSubtractReservation)
		{
			AddMemoryFailPointReservation((long)(0L - _reservedMemory));
			_mustSubtractReservation = false;
		}
	}

	internal static long AddMemoryFailPointReservation(long size)
	{
		return Interlocked.Add(ref s_failPointReservedMemory, size);
	}

	private static ulong GetTopOfMemory()
	{
		Interop.Kernel32.GetSystemInfo(out var lpSystemInfo);
		return (ulong)(long)lpSystemInfo.lpMaximumApplicationAddress;
	}

	private unsafe static bool CheckForAvailableMemory(out ulong availPageFile, out ulong totalAddressSpaceFree)
	{
		Interop.Kernel32.MEMORYSTATUSEX mEMORYSTATUSEX = default(Interop.Kernel32.MEMORYSTATUSEX);
		mEMORYSTATUSEX.dwLength = (uint)sizeof(Interop.Kernel32.MEMORYSTATUSEX);
		if (Interop.Kernel32.GlobalMemoryStatusEx(&mEMORYSTATUSEX) == Interop.BOOL.FALSE)
		{
			availPageFile = 0uL;
			totalAddressSpaceFree = 0uL;
			return false;
		}
		availPageFile = mEMORYSTATUSEX.ullAvailPageFile;
		totalAddressSpaceFree = mEMORYSTATUSEX.ullAvailVirtual;
		return true;
	}

	private unsafe static void CheckForFreeAddressSpace(ulong size, bool shouldThrow)
	{
		ulong num2 = (ulong)(LastKnownFreeAddressSpace = (long)MemFreeAfterAddress(null, size));
		LastTimeCheckingAddressSpace = Environment.TickCount;
		if (num2 < size && shouldThrow)
		{
			throw new InsufficientMemoryException(SR.InsufficientMemory_MemFailPoint_VAFrag);
		}
	}

	private unsafe static ulong MemFreeAfterAddress(void* address, ulong size)
	{
		if (size >= s_topOfMemory)
		{
			return 0uL;
		}
		ulong num = 0uL;
		Interop.Kernel32.MEMORY_BASIC_INFORMATION lpBuffer = default(Interop.Kernel32.MEMORY_BASIC_INFORMATION);
		UIntPtr dwLength = (UIntPtr)(ulong)sizeof(Interop.Kernel32.MEMORY_BASIC_INFORMATION);
		while ((ulong)((long)address + (long)size) < s_topOfMemory)
		{
			UIntPtr uIntPtr = Interop.Kernel32.VirtualQuery(address, ref lpBuffer, dwLength);
			if (uIntPtr == UIntPtr.Zero)
			{
				throw Win32Marshal.GetExceptionForLastWin32Error();
			}
			ulong num2 = lpBuffer.RegionSize.ToUInt64();
			if (lpBuffer.State == 65536)
			{
				if (num2 >= size)
				{
					return num2;
				}
				num = Math.Max(num, num2);
			}
			address = (void*)((ulong)address + num2);
		}
		return num;
	}

	private unsafe static void GrowPageFileIfNecessaryAndPossible(UIntPtr numBytes)
	{
		void* ptr = Interop.Kernel32.VirtualAlloc(null, numBytes, 4096, 4);
		if (ptr != null && !Interop.Kernel32.VirtualFree(ptr, UIntPtr.Zero, 32768))
		{
			throw Win32Marshal.GetExceptionForLastWin32Error();
		}
	}
}
