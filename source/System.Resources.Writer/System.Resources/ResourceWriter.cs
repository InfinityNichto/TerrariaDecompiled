using System.Collections.Generic;
using System.IO;
using System.Text;

namespace System.Resources;

public sealed class ResourceWriter : IResourceWriter, IDisposable
{
	private sealed class PrecannedResource
	{
		internal readonly string TypeName;

		internal readonly object Data;

		internal PrecannedResource(string typeName, object data)
		{
			TypeName = typeName;
			Data = data;
		}
	}

	private sealed class StreamWrapper
	{
		internal readonly Stream Stream;

		internal readonly bool CloseAfterWrite;

		internal StreamWrapper(Stream s, bool closeAfterWrite)
		{
			Stream = s;
			CloseAfterWrite = closeAfterWrite;
		}
	}

	private SortedDictionary<string, object> _resourceList;

	private Stream _output;

	private Dictionary<string, object> _caseInsensitiveDups;

	private Dictionary<string, PrecannedResource> _preserializedData;

	public Func<Type, string>? TypeNameConverter { get; set; }

	private string ResourceReaderTypeName => "System.Resources.ResourceReader, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";

	private string ResourceSetTypeName => "System.Resources.RuntimeResourceSet";

	public void AddResource(string name, Stream? value)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (_resourceList == null)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_ResourceWriterSaved);
		}
		AddResourceInternal(name, value, closeAfterWrite: false);
	}

	public void AddResourceData(string name, string typeName, byte[] serializedData)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (typeName == null)
		{
			throw new ArgumentNullException("typeName");
		}
		if (serializedData == null)
		{
			throw new ArgumentNullException("serializedData");
		}
		AddResourceData(name, typeName, (object)serializedData);
	}

	private void WriteData(BinaryWriter writer, object dataContext)
	{
		byte[] buffer = dataContext as byte[];
		writer.Write(buffer);
	}

	public ResourceWriter(string fileName)
	{
		if (fileName == null)
		{
			throw new ArgumentNullException("fileName");
		}
		_output = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None);
		_resourceList = new SortedDictionary<string, object>(System.Resources.FastResourceComparer.Default);
		_caseInsensitiveDups = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
	}

	public ResourceWriter(Stream stream)
	{
		if (stream == null)
		{
			throw new ArgumentNullException("stream");
		}
		if (!stream.CanWrite)
		{
			throw new ArgumentException(System.SR.Argument_StreamNotWritable);
		}
		_output = stream;
		_resourceList = new SortedDictionary<string, object>(System.Resources.FastResourceComparer.Default);
		_caseInsensitiveDups = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
	}

	public void AddResource(string name, string? value)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (_resourceList == null)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_ResourceWriterSaved);
		}
		_caseInsensitiveDups.Add(name, null);
		_resourceList.Add(name, value);
	}

	public void AddResource(string name, object? value)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (_resourceList == null)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_ResourceWriterSaved);
		}
		if (value != null && value is Stream)
		{
			AddResourceInternal(name, (Stream)value, closeAfterWrite: false);
			return;
		}
		_caseInsensitiveDups.Add(name, null);
		_resourceList.Add(name, value);
	}

	public void AddResource(string name, Stream? value, bool closeAfterWrite = false)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (_resourceList == null)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_ResourceWriterSaved);
		}
		AddResourceInternal(name, value, closeAfterWrite);
	}

	private void AddResourceInternal(string name, Stream value, bool closeAfterWrite)
	{
		if (value == null)
		{
			_caseInsensitiveDups.Add(name, null);
			_resourceList.Add(name, value);
			return;
		}
		if (!value.CanSeek)
		{
			throw new ArgumentException(System.SR.NotSupported_UnseekableStream);
		}
		_caseInsensitiveDups.Add(name, null);
		_resourceList.Add(name, new StreamWrapper(value, closeAfterWrite));
	}

	public void AddResource(string name, byte[]? value)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (_resourceList == null)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_ResourceWriterSaved);
		}
		_caseInsensitiveDups.Add(name, null);
		_resourceList.Add(name, value);
	}

	private void AddResourceData(string name, string typeName, object data)
	{
		if (_resourceList == null)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_ResourceWriterSaved);
		}
		_caseInsensitiveDups.Add(name, null);
		if (_preserializedData == null)
		{
			_preserializedData = new Dictionary<string, PrecannedResource>(System.Resources.FastResourceComparer.Default);
		}
		_preserializedData.Add(name, new PrecannedResource(typeName, data));
	}

	public void Close()
	{
		Dispose(disposing: true);
	}

	private void Dispose(bool disposing)
	{
		if (disposing)
		{
			if (_resourceList != null)
			{
				Generate();
			}
			if (_output != null)
			{
				_output.Dispose();
			}
		}
		_output = null;
		_caseInsensitiveDups = null;
	}

	public void Dispose()
	{
		Dispose(disposing: true);
	}

	public void Generate()
	{
		if (_resourceList == null)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_ResourceWriterSaved);
		}
		BinaryWriter binaryWriter = new BinaryWriter(_output, Encoding.UTF8);
		List<string> list = new List<string>();
		binaryWriter.Write(ResourceManager.MagicNumber);
		binaryWriter.Write(ResourceManager.HeaderVersionNumber);
		MemoryStream memoryStream = new MemoryStream(240);
		BinaryWriter binaryWriter2 = new BinaryWriter(memoryStream);
		binaryWriter2.Write(ResourceReaderTypeName);
		binaryWriter2.Write(ResourceSetTypeName);
		binaryWriter2.Flush();
		binaryWriter.Write((int)memoryStream.Length);
		memoryStream.Seek(0L, SeekOrigin.Begin);
		memoryStream.CopyTo(binaryWriter.BaseStream, (int)memoryStream.Length);
		binaryWriter.Write(2);
		int num = _resourceList.Count;
		if (_preserializedData != null)
		{
			num += _preserializedData.Count;
		}
		binaryWriter.Write(num);
		int[] array = new int[num];
		int[] array2 = new int[num];
		int num2 = 0;
		MemoryStream memoryStream2 = new MemoryStream(num * 40);
		BinaryWriter binaryWriter3 = new BinaryWriter(memoryStream2, Encoding.Unicode);
		Stream stream = new MemoryStream();
		using (stream)
		{
			BinaryWriter binaryWriter4 = new BinaryWriter(stream, Encoding.UTF8);
			if (_preserializedData != null)
			{
				foreach (KeyValuePair<string, PrecannedResource> preserializedDatum in _preserializedData)
				{
					_resourceList.Add(preserializedDatum.Key, preserializedDatum.Value);
				}
			}
			foreach (KeyValuePair<string, object> resource in _resourceList)
			{
				array[num2] = System.Resources.FastResourceComparer.HashFunction(resource.Key);
				array2[num2++] = (int)binaryWriter3.Seek(0, SeekOrigin.Current);
				binaryWriter3.Write(resource.Key);
				binaryWriter3.Write((int)binaryWriter4.Seek(0, SeekOrigin.Current));
				object value = resource.Value;
				System.Resources.ResourceTypeCode resourceTypeCode = FindTypeCode(value, list);
				binaryWriter4.Write7BitEncodedInt((int)resourceTypeCode);
				if (value is PrecannedResource precannedResource)
				{
					WriteData(binaryWriter4, precannedResource.Data);
				}
				else
				{
					WriteValue(resourceTypeCode, value, binaryWriter4);
				}
			}
			binaryWriter.Write(list.Count);
			foreach (string item in list)
			{
				binaryWriter.Write(item);
			}
			Array.Sort(array, array2);
			binaryWriter.Flush();
			int num3 = (int)binaryWriter.BaseStream.Position & 7;
			if (num3 > 0)
			{
				for (int i = 0; i < 8 - num3; i++)
				{
					binaryWriter.Write("PAD"[i % 3]);
				}
			}
			int[] array3 = array;
			foreach (int value2 in array3)
			{
				binaryWriter.Write(value2);
			}
			int[] array4 = array2;
			foreach (int value3 in array4)
			{
				binaryWriter.Write(value3);
			}
			binaryWriter.Flush();
			binaryWriter3.Flush();
			binaryWriter4.Flush();
			int num4 = (int)(binaryWriter.Seek(0, SeekOrigin.Current) + memoryStream2.Length);
			num4 += 4;
			binaryWriter.Write(num4);
			if (memoryStream2.Length > 0)
			{
				memoryStream2.Seek(0L, SeekOrigin.Begin);
				memoryStream2.CopyTo(binaryWriter.BaseStream, (int)memoryStream2.Length);
			}
			binaryWriter3.Dispose();
			stream.Position = 0L;
			stream.CopyTo(binaryWriter.BaseStream);
			binaryWriter4.Dispose();
		}
		binaryWriter.Flush();
		_resourceList = null;
	}

	private System.Resources.ResourceTypeCode FindTypeCode(object value, List<string> types)
	{
		if (value == null)
		{
			return System.Resources.ResourceTypeCode.Null;
		}
		Type type = value.GetType();
		if (type == typeof(string))
		{
			return System.Resources.ResourceTypeCode.String;
		}
		if (type == typeof(int))
		{
			return System.Resources.ResourceTypeCode.Int32;
		}
		if (type == typeof(bool))
		{
			return System.Resources.ResourceTypeCode.Boolean;
		}
		if (type == typeof(char))
		{
			return System.Resources.ResourceTypeCode.Char;
		}
		if (type == typeof(byte))
		{
			return System.Resources.ResourceTypeCode.Byte;
		}
		if (type == typeof(sbyte))
		{
			return System.Resources.ResourceTypeCode.SByte;
		}
		if (type == typeof(short))
		{
			return System.Resources.ResourceTypeCode.Int16;
		}
		if (type == typeof(long))
		{
			return System.Resources.ResourceTypeCode.Int64;
		}
		if (type == typeof(ushort))
		{
			return System.Resources.ResourceTypeCode.UInt16;
		}
		if (type == typeof(uint))
		{
			return System.Resources.ResourceTypeCode.UInt32;
		}
		if (type == typeof(ulong))
		{
			return System.Resources.ResourceTypeCode.UInt64;
		}
		if (type == typeof(float))
		{
			return System.Resources.ResourceTypeCode.Single;
		}
		if (type == typeof(double))
		{
			return System.Resources.ResourceTypeCode.Double;
		}
		if (type == typeof(decimal))
		{
			return System.Resources.ResourceTypeCode.Decimal;
		}
		if (type == typeof(DateTime))
		{
			return System.Resources.ResourceTypeCode.DateTime;
		}
		if (type == typeof(TimeSpan))
		{
			return System.Resources.ResourceTypeCode.TimeSpan;
		}
		if (type == typeof(byte[]))
		{
			return System.Resources.ResourceTypeCode.ByteArray;
		}
		if (type == typeof(StreamWrapper))
		{
			return System.Resources.ResourceTypeCode.Stream;
		}
		if (type == typeof(PrecannedResource))
		{
			string typeName = ((PrecannedResource)value).TypeName;
			if (typeName.StartsWith("ResourceTypeCode.", StringComparison.Ordinal))
			{
				typeName = typeName.Substring(17);
				return (System.Resources.ResourceTypeCode)Enum.Parse(typeof(System.Resources.ResourceTypeCode), typeName);
			}
			int num = types.IndexOf(typeName);
			if (num == -1)
			{
				num = types.Count;
				types.Add(typeName);
			}
			return (System.Resources.ResourceTypeCode)(num + 64);
		}
		throw new PlatformNotSupportedException(System.SR.NotSupported_BinarySerializedResources);
	}

	private void WriteValue(System.Resources.ResourceTypeCode typeCode, object value, BinaryWriter writer)
	{
		switch (typeCode)
		{
		case System.Resources.ResourceTypeCode.String:
			writer.Write((string)value);
			break;
		case System.Resources.ResourceTypeCode.Boolean:
			writer.Write((bool)value);
			break;
		case System.Resources.ResourceTypeCode.Char:
			writer.Write((ushort)(char)value);
			break;
		case System.Resources.ResourceTypeCode.Byte:
			writer.Write((byte)value);
			break;
		case System.Resources.ResourceTypeCode.SByte:
			writer.Write((sbyte)value);
			break;
		case System.Resources.ResourceTypeCode.Int16:
			writer.Write((short)value);
			break;
		case System.Resources.ResourceTypeCode.UInt16:
			writer.Write((ushort)value);
			break;
		case System.Resources.ResourceTypeCode.Int32:
			writer.Write((int)value);
			break;
		case System.Resources.ResourceTypeCode.UInt32:
			writer.Write((uint)value);
			break;
		case System.Resources.ResourceTypeCode.Int64:
			writer.Write((long)value);
			break;
		case System.Resources.ResourceTypeCode.UInt64:
			writer.Write((ulong)value);
			break;
		case System.Resources.ResourceTypeCode.Single:
			writer.Write((float)value);
			break;
		case System.Resources.ResourceTypeCode.Double:
			writer.Write((double)value);
			break;
		case System.Resources.ResourceTypeCode.Decimal:
			writer.Write((decimal)value);
			break;
		case System.Resources.ResourceTypeCode.DateTime:
		{
			long value2 = ((DateTime)value).ToBinary();
			writer.Write(value2);
			break;
		}
		case System.Resources.ResourceTypeCode.TimeSpan:
			writer.Write(((TimeSpan)value).Ticks);
			break;
		case System.Resources.ResourceTypeCode.ByteArray:
		{
			byte[] array3 = (byte[])value;
			writer.Write(array3.Length);
			writer.Write(array3, 0, array3.Length);
			break;
		}
		case System.Resources.ResourceTypeCode.Stream:
		{
			StreamWrapper streamWrapper = (StreamWrapper)value;
			if (streamWrapper.Stream.GetType() == typeof(MemoryStream))
			{
				MemoryStream memoryStream = (MemoryStream)streamWrapper.Stream;
				if (memoryStream.Length > int.MaxValue)
				{
					throw new ArgumentException(System.SR.ArgumentOutOfRange_StreamLength);
				}
				byte[] array = memoryStream.ToArray();
				writer.Write(array.Length);
				writer.Write(array, 0, array.Length);
				break;
			}
			Stream stream = streamWrapper.Stream;
			if (stream.Length > int.MaxValue)
			{
				throw new ArgumentException(System.SR.ArgumentOutOfRange_StreamLength);
			}
			stream.Position = 0L;
			writer.Write((int)stream.Length);
			byte[] array2 = new byte[4096];
			int num = 0;
			while ((num = stream.Read(array2, 0, array2.Length)) != 0)
			{
				writer.Write(array2, 0, num);
			}
			if (streamWrapper.CloseAfterWrite)
			{
				stream.Close();
			}
			break;
		}
		default:
			throw new PlatformNotSupportedException(System.SR.NotSupported_BinarySerializedResources);
		case System.Resources.ResourceTypeCode.Null:
			break;
		}
	}
}
