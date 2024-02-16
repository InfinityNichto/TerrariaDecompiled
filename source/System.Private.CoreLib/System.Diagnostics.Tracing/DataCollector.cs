using System.Runtime.InteropServices;

namespace System.Diagnostics.Tracing;

internal struct DataCollector
{
	[ThreadStatic]
	internal static DataCollector ThreadInstance;

	private unsafe byte* scratchEnd;

	private unsafe EventSource.EventData* datasEnd;

	private unsafe GCHandle* pinsEnd;

	private unsafe EventSource.EventData* datasStart;

	private unsafe byte* scratch;

	private unsafe EventSource.EventData* datas;

	private unsafe GCHandle* pins;

	private byte[] buffer;

	private int bufferPos;

	private int bufferNesting;

	private bool writingScalars;

	internal unsafe void Enable(byte* scratch, int scratchSize, EventSource.EventData* datas, int dataCount, GCHandle* pins, int pinCount)
	{
		datasStart = datas;
		scratchEnd = scratch + scratchSize;
		datasEnd = datas + dataCount;
		pinsEnd = pins + pinCount;
		this.scratch = scratch;
		this.datas = datas;
		this.pins = pins;
		writingScalars = false;
	}

	internal void Disable()
	{
		this = default(DataCollector);
	}

	internal unsafe EventSource.EventData* Finish()
	{
		ScalarsEnd();
		return datas;
	}

	internal unsafe void AddScalar(void* value, int size)
	{
		if (bufferNesting == 0)
		{
			byte* ptr = scratch;
			byte* ptr2 = ptr + size;
			if (scratchEnd < ptr2)
			{
				throw new IndexOutOfRangeException(SR.EventSource_AddScalarOutOfRange);
			}
			ScalarsBegin();
			scratch = ptr2;
			for (int i = 0; i != size; i++)
			{
				ptr[i] = ((byte*)value)[i];
			}
		}
		else
		{
			int num = bufferPos;
			int num2;
			checked
			{
				bufferPos += size;
				EnsureBuffer();
				num2 = 0;
			}
			while (num2 != size)
			{
				buffer[num] = ((byte*)value)[num2];
				num2++;
				num++;
			}
		}
	}

	internal unsafe void AddNullTerminatedString(string value)
	{
		if (value == null)
		{
			value = string.Empty;
		}
		int num = value.IndexOf('\0');
		if (num < 0)
		{
			num = value.Length;
		}
		int num2 = (num + 1) * 2;
		if (bufferNesting != 0)
		{
			EnsureBuffer(num2);
		}
		if (bufferNesting == 0)
		{
			ScalarsEnd();
			PinArray(value, num2);
			return;
		}
		int startIndex = bufferPos;
		checked
		{
			bufferPos += num2;
			EnsureBuffer();
			fixed (char* ptr = value)
			{
				void* ptr2 = ptr;
				Marshal.Copy((IntPtr)ptr2, buffer, startIndex, num2);
			}
		}
	}

	internal unsafe void AddArray(Array value, int length, int itemSize)
	{
		if (length > 65535)
		{
			length = 65535;
		}
		int num = length * itemSize;
		if (bufferNesting != 0)
		{
			EnsureBuffer(num + 2);
		}
		AddScalar(&length, 2);
		checked
		{
			if (length != 0)
			{
				if (bufferNesting == 0)
				{
					ScalarsEnd();
					PinArray(value, num);
					return;
				}
				int dstOffset = bufferPos;
				bufferPos += num;
				EnsureBuffer();
				Buffer.BlockCopy(value, 0, buffer, dstOffset, num);
			}
		}
	}

	internal int BeginBufferedArray()
	{
		BeginBuffered();
		bufferPos += 2;
		return bufferPos;
	}

	internal void EndBufferedArray(int bookmark, int count)
	{
		EnsureBuffer();
		buffer[bookmark - 2] = (byte)count;
		buffer[bookmark - 1] = (byte)(count >> 8);
		EndBuffered();
	}

	internal void BeginBuffered()
	{
		ScalarsEnd();
		bufferNesting++;
	}

	internal void EndBuffered()
	{
		bufferNesting--;
		if (bufferNesting == 0)
		{
			EnsureBuffer();
			PinArray(buffer, bufferPos);
			buffer = null;
			bufferPos = 0;
		}
	}

	private void EnsureBuffer()
	{
		int num = bufferPos;
		if (buffer == null || buffer.Length < num)
		{
			GrowBuffer(num);
		}
	}

	private void EnsureBuffer(int additionalSize)
	{
		int num = bufferPos + additionalSize;
		if (buffer == null || buffer.Length < num)
		{
			GrowBuffer(num);
		}
	}

	private void GrowBuffer(int required)
	{
		int num = ((buffer == null) ? 64 : buffer.Length);
		do
		{
			num *= 2;
		}
		while (num < required);
		Array.Resize(ref buffer, num);
	}

	private unsafe void PinArray(object value, int size)
	{
		GCHandle* ptr = pins;
		if (pinsEnd <= ptr)
		{
			throw new IndexOutOfRangeException(SR.EventSource_PinArrayOutOfRange);
		}
		EventSource.EventData* ptr2 = datas;
		if (datasEnd <= ptr2)
		{
			throw new IndexOutOfRangeException(SR.EventSource_DataDescriptorsOutOfRange);
		}
		pins = ptr + 1;
		datas = ptr2 + 1;
		*ptr = GCHandle.Alloc(value, GCHandleType.Pinned);
		ptr2->DataPointer = ptr->AddrOfPinnedObject();
		ptr2->m_Size = size;
	}

	private unsafe void ScalarsBegin()
	{
		if (!writingScalars)
		{
			EventSource.EventData* ptr = datas;
			if (datasEnd <= ptr)
			{
				throw new IndexOutOfRangeException(SR.EventSource_DataDescriptorsOutOfRange);
			}
			ptr->DataPointer = (IntPtr)scratch;
			writingScalars = true;
		}
	}

	private unsafe void ScalarsEnd()
	{
		checked
		{
			if (writingScalars)
			{
				EventSource.EventData* ptr = datas;
				ptr->m_Size = (int)(scratch - unchecked((byte*)checked((nuint)ptr->m_Ptr)));
				datas = ptr + 1;
				writingScalars = false;
			}
		}
	}
}
