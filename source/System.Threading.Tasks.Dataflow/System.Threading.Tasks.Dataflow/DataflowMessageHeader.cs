using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Threading.Tasks.Dataflow;

[DebuggerDisplay("Id = {Id}")]
public readonly struct DataflowMessageHeader : IEquatable<DataflowMessageHeader>
{
	private readonly long _id;

	public bool IsValid => _id != 0;

	public long Id => _id;

	public DataflowMessageHeader(long id)
	{
		if (id == 0L)
		{
			throw new ArgumentException(System.SR.Argument_InvalidMessageId, "id");
		}
		_id = id;
	}

	public bool Equals(DataflowMessageHeader other)
	{
		return this == other;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is DataflowMessageHeader)
		{
			return this == (DataflowMessageHeader)obj;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (int)Id;
	}

	public static bool operator ==(DataflowMessageHeader left, DataflowMessageHeader right)
	{
		return left.Id == right.Id;
	}

	public static bool operator !=(DataflowMessageHeader left, DataflowMessageHeader right)
	{
		return left.Id != right.Id;
	}
}
