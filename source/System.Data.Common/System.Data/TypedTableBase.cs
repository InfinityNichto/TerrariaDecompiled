using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;

namespace System.Data;

[Serializable]
public abstract class TypedTableBase<T> : DataTable, IEnumerable<T>, IEnumerable where T : DataRow
{
	protected TypedTableBase()
	{
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2112:ReflectionToRequiresUnreferencedCode", Justification = "DataTable.CreateInstance's use of GetType uses only the parameterless constructor, not this serialization related constructor.")]
	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	protected TypedTableBase(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}

	public IEnumerator<T> GetEnumerator()
	{
		return base.Rows.Cast<T>().GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public EnumerableRowCollection<TResult> Cast<TResult>()
	{
		EnumerableRowCollection<T> source = new EnumerableRowCollection<T>(this);
		return source.Cast<TResult>();
	}
}
