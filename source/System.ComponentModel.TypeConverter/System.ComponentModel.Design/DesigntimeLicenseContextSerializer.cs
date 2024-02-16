using System.Collections;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace System.ComponentModel.Design;

public class DesigntimeLicenseContextSerializer
{
	private class StreamWrapper : Stream
	{
		private Stream _stream;

		private bool _readFirstByte;

		internal byte _firstByte;

		public override bool CanRead => _stream.CanRead;

		public override bool CanSeek => _stream.CanSeek;

		public override bool CanWrite => _stream.CanWrite;

		public override long Length => _stream.Length;

		public override long Position
		{
			get
			{
				return _stream.Position;
			}
			set
			{
				_stream.Position = value;
			}
		}

		public StreamWrapper(Stream stream)
		{
			_stream = stream;
			_readFirstByte = false;
			_firstByte = 0;
		}

		public override void Flush()
		{
			_stream.Flush();
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			if (_stream.Position == 1)
			{
				buffer[offset] = _firstByte;
				return _stream.Read(buffer, offset + 1, count - 1) + 1;
			}
			return _stream.Read(buffer, offset, count);
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			return _stream.Seek(offset, origin);
		}

		public override void SetLength(long value)
		{
			_stream.SetLength(value);
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			_stream.Write(buffer, offset, count);
		}

		public override int ReadByte()
		{
			byte result = (_firstByte = (byte)_stream.ReadByte());
			_readFirstByte = true;
			return result;
		}
	}

	private static bool EnableUnsafeBinaryFormatterInDesigntimeLicenseContextSerialization { get; } = AppContext.TryGetSwitch("System.ComponentModel.TypeConverter.EnableUnsafeBinaryFormatterInDesigntimeLicenseContextSerialization", out var isEnabled) && isEnabled;


	public static void Serialize(Stream o, string cryptoKey, DesigntimeLicenseContext context)
	{
		if (EnableUnsafeBinaryFormatterInDesigntimeLicenseContextSerialization)
		{
			SerializeWithBinaryFormatter(o, cryptoKey, context);
			return;
		}
		using BinaryWriter binaryWriter = new BinaryWriter(o, Encoding.UTF8, leaveOpen: true);
		binaryWriter.Write(byte.MaxValue);
		binaryWriter.Write(cryptoKey);
		binaryWriter.Write(context._savedLicenseKeys.Count);
		foreach (DictionaryEntry savedLicenseKey in context._savedLicenseKeys)
		{
			binaryWriter.Write(savedLicenseKey.Key.ToString());
			binaryWriter.Write(savedLicenseKey.Value.ToString());
		}
	}

	private static void SerializeWithBinaryFormatter(Stream o, string cryptoKey, DesigntimeLicenseContext context)
	{
		IFormatter formatter = new BinaryFormatter();
		formatter.Serialize(o, new object[2] { cryptoKey, context._savedLicenseKeys });
	}

	private static bool StreamIsBinaryFormatted(StreamWrapper stream)
	{
		if (stream.ReadByte() != 0)
		{
			return false;
		}
		return true;
	}

	private static void DeserializeUsingBinaryFormatter(StreamWrapper wrappedStream, string cryptoKey, RuntimeLicenseContext context)
	{
		if (EnableUnsafeBinaryFormatterInDesigntimeLicenseContextSerialization)
		{
			IFormatter formatter = new BinaryFormatter();
			object obj = formatter.Deserialize(wrappedStream);
			if (obj is object[] array && array[0] is string && (string)array[0] == cryptoKey)
			{
				context._savedLicenseKeys = (Hashtable)array[1];
			}
			return;
		}
		throw new NotSupportedException(System.SR.BinaryFormatterMessage);
	}

	internal static void Deserialize(Stream o, string cryptoKey, RuntimeLicenseContext context)
	{
		StreamWrapper streamWrapper = new StreamWrapper(o);
		if (StreamIsBinaryFormatted(streamWrapper))
		{
			DeserializeUsingBinaryFormatter(streamWrapper, cryptoKey, context);
			return;
		}
		using BinaryReader binaryReader = new BinaryReader(streamWrapper, Encoding.UTF8, leaveOpen: true);
		byte firstByte = streamWrapper._firstByte;
		string text = binaryReader.ReadString();
		int num = binaryReader.ReadInt32();
		if (text == cryptoKey)
		{
			context._savedLicenseKeys.Clear();
			for (int i = 0; i < num; i++)
			{
				string key = binaryReader.ReadString();
				string value = binaryReader.ReadString();
				context._savedLicenseKeys.Add(key, value);
			}
		}
	}
}
