using System.Collections.Generic;

namespace System.Diagnostics.Tracing;

internal sealed class TraceLoggingMetadataCollector
{
	private sealed class Impl
	{
		internal readonly List<FieldMetadata> fields = new List<FieldMetadata>();

		internal short scratchSize;

		internal sbyte dataCount;

		internal sbyte pinCount;

		private int bufferNesting;

		private bool scalar;

		public void AddScalar(int size)
		{
			checked
			{
				if (bufferNesting == 0)
				{
					if (!scalar)
					{
						dataCount++;
					}
					scalar = true;
					scratchSize = (short)(scratchSize + size);
				}
			}
		}

		public void AddNonscalar()
		{
			checked
			{
				if (bufferNesting == 0)
				{
					scalar = false;
					pinCount++;
					dataCount++;
				}
			}
		}

		public void BeginBuffered()
		{
			if (bufferNesting == 0)
			{
				AddNonscalar();
			}
			bufferNesting++;
		}

		public void EndBuffered()
		{
			bufferNesting--;
		}

		public int Encode(byte[] metadata)
		{
			int pos = 0;
			foreach (FieldMetadata field in fields)
			{
				field.Encode(ref pos, metadata);
			}
			return pos;
		}
	}

	private readonly Impl impl;

	private readonly FieldMetadata currentGroup;

	private int bufferedArrayFieldCount = int.MinValue;

	internal EventFieldTags Tags { get; set; }

	internal int ScratchSize => impl.scratchSize;

	internal int DataCount => impl.dataCount;

	internal int PinCount => impl.pinCount;

	private bool BeginningBufferedArray => bufferedArrayFieldCount == 0;

	internal TraceLoggingMetadataCollector()
	{
		impl = new Impl();
	}

	private TraceLoggingMetadataCollector(TraceLoggingMetadataCollector other, FieldMetadata group)
	{
		impl = other.impl;
		currentGroup = group;
	}

	public TraceLoggingMetadataCollector AddGroup(string name)
	{
		TraceLoggingMetadataCollector result = this;
		if (name != null || BeginningBufferedArray)
		{
			FieldMetadata fieldMetadata = new FieldMetadata(name, TraceLoggingDataType.Struct, Tags, BeginningBufferedArray);
			AddField(fieldMetadata);
			result = new TraceLoggingMetadataCollector(this, fieldMetadata);
		}
		return result;
	}

	public void AddScalar(string name, TraceLoggingDataType type)
	{
		int size;
		switch (type & (TraceLoggingDataType)31)
		{
		case TraceLoggingDataType.Int8:
		case TraceLoggingDataType.UInt8:
		case TraceLoggingDataType.Char8:
			size = 1;
			break;
		case TraceLoggingDataType.Int16:
		case TraceLoggingDataType.UInt16:
		case TraceLoggingDataType.Char16:
			size = 2;
			break;
		case TraceLoggingDataType.Int32:
		case TraceLoggingDataType.UInt32:
		case TraceLoggingDataType.Float:
		case TraceLoggingDataType.Boolean32:
		case TraceLoggingDataType.HexInt32:
			size = 4;
			break;
		case TraceLoggingDataType.Int64:
		case TraceLoggingDataType.UInt64:
		case TraceLoggingDataType.Double:
		case TraceLoggingDataType.FileTime:
		case TraceLoggingDataType.HexInt64:
			size = 8;
			break;
		case TraceLoggingDataType.Guid:
		case TraceLoggingDataType.SystemTime:
			size = 16;
			break;
		default:
			throw new ArgumentOutOfRangeException("type");
		}
		impl.AddScalar(size);
		AddField(new FieldMetadata(name, type, Tags, BeginningBufferedArray));
	}

	public void AddNullTerminatedString(string name, TraceLoggingDataType type)
	{
		TraceLoggingDataType traceLoggingDataType = type & (TraceLoggingDataType)31;
		if (traceLoggingDataType != TraceLoggingDataType.Utf16String)
		{
			throw new ArgumentOutOfRangeException("type");
		}
		impl.AddNonscalar();
		AddField(new FieldMetadata(name, type, Tags, BeginningBufferedArray));
	}

	public void AddArray(string name, TraceLoggingDataType type)
	{
		switch (type & (TraceLoggingDataType)31)
		{
		default:
			throw new ArgumentOutOfRangeException("type");
		case TraceLoggingDataType.Int8:
		case TraceLoggingDataType.UInt8:
		case TraceLoggingDataType.Int16:
		case TraceLoggingDataType.UInt16:
		case TraceLoggingDataType.Int32:
		case TraceLoggingDataType.UInt32:
		case TraceLoggingDataType.Int64:
		case TraceLoggingDataType.UInt64:
		case TraceLoggingDataType.Float:
		case TraceLoggingDataType.Double:
		case TraceLoggingDataType.Boolean32:
		case TraceLoggingDataType.Guid:
		case TraceLoggingDataType.FileTime:
		case TraceLoggingDataType.HexInt32:
		case TraceLoggingDataType.HexInt64:
		case TraceLoggingDataType.Char8:
		case TraceLoggingDataType.Char16:
			if (BeginningBufferedArray)
			{
				throw new NotSupportedException(SR.EventSource_NotSupportedNestedArraysEnums);
			}
			impl.AddScalar(2);
			impl.AddNonscalar();
			AddField(new FieldMetadata(name, type, Tags, variableCount: true));
			break;
		}
	}

	public void BeginBufferedArray()
	{
		if (bufferedArrayFieldCount >= 0)
		{
			throw new NotSupportedException(SR.EventSource_NotSupportedNestedArraysEnums);
		}
		bufferedArrayFieldCount = 0;
		impl.BeginBuffered();
	}

	public void EndBufferedArray()
	{
		if (bufferedArrayFieldCount != 1)
		{
			throw new InvalidOperationException(SR.EventSource_IncorrentlyAuthoredTypeInfo);
		}
		bufferedArrayFieldCount = int.MinValue;
		impl.EndBuffered();
	}

	internal byte[] GetMetadata()
	{
		int num = impl.Encode(null);
		byte[] array = new byte[num];
		impl.Encode(array);
		return array;
	}

	private void AddField(FieldMetadata fieldMetadata)
	{
		Tags = EventFieldTags.None;
		bufferedArrayFieldCount++;
		impl.fields.Add(fieldMetadata);
		if (currentGroup != null)
		{
			currentGroup.IncrementStructFieldCount();
		}
	}
}
