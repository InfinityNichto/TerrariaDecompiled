using System.IO;

namespace System.Runtime.Serialization;

[DataContract(Name = "MemoryStream", Namespace = "http://schemas.datacontract.org/2004/07/System.IO")]
internal sealed class MemoryStreamAdapter : MarshalByRefObjectAdapter
{
	[DataMember(Name = "_buffer", Order = 1)]
	public byte[] Buffer { get; set; }

	[DataMember(Name = "_capacity", Order = 2)]
	public int Capacity { get; set; }

	[DataMember(Name = "_expandable", Order = 3)]
	public bool Expandable { get; set; }

	[DataMember(Name = "_exposable", Order = 4)]
	public bool Exposable { get; set; }

	[DataMember(Name = "_isOpen", Order = 5)]
	public bool IsOpen { get; set; }

	[DataMember(Name = "_length", Order = 6)]
	public int Length { get; set; }

	[DataMember(Name = "_origin", Order = 7)]
	public int Origin { get; set; }

	[DataMember(Name = "_position", Order = 8)]
	public int Position { get; set; }

	[DataMember(Name = "_writable", Order = 9)]
	public bool Writable { get; set; }

	public static MemoryStream GetMemoryStream(MemoryStreamAdapter value)
	{
		byte[] array = value.Buffer;
		Span<byte> span = value.Buffer.AsSpan(value.Origin, value.Length - value.Origin);
		if (span.Length < array.Length)
		{
			array = span.ToArray();
		}
		MemoryStream memoryStream = new MemoryStream(array, 0, array.Length, value.Writable, value.Exposable);
		int num = value.Position - value.Origin;
		if (num < 0 || num > memoryStream.Length)
		{
			throw new InvalidOperationException();
		}
		memoryStream.Position = num;
		return memoryStream;
	}

	public static MemoryStreamAdapter GetMemoryStreamAdapter(MemoryStream memoryStream)
	{
		MemoryStreamAdapter memoryStreamAdapter = new MemoryStreamAdapter();
		if (memoryStream.TryGetBuffer(out var buffer))
		{
			memoryStreamAdapter.Exposable = true;
			if (buffer.Count == buffer.Array.Length)
			{
				memoryStreamAdapter.Buffer = buffer.Array;
			}
		}
		MemoryStreamAdapter memoryStreamAdapter2 = memoryStreamAdapter;
		if (memoryStreamAdapter2.Buffer == null)
		{
			byte[] array2 = (memoryStreamAdapter2.Buffer = memoryStream.ToArray());
		}
		checked
		{
			memoryStreamAdapter.Length = (int)memoryStream.Length;
			memoryStreamAdapter.Capacity = memoryStream.Capacity;
			memoryStreamAdapter.Position = (int)memoryStream.Position;
			memoryStreamAdapter.Writable = memoryStream.CanWrite;
			memoryStreamAdapter.Origin = 0;
			memoryStreamAdapter.Expandable = false;
			memoryStreamAdapter.IsOpen = true;
			return memoryStreamAdapter;
		}
	}
}
