using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;

namespace System.Runtime.Serialization;

[CLSCompliant(false)]
public abstract class Formatter : IFormatter
{
	protected ObjectIDGenerator m_idGenerator;

	protected Queue m_objectQueue;

	public abstract ISurrogateSelector? SurrogateSelector { get; set; }

	public abstract SerializationBinder? Binder { get; set; }

	public abstract StreamingContext Context { get; set; }

	protected Formatter()
	{
		m_objectQueue = new Queue();
		m_idGenerator = new ObjectIDGenerator();
	}

	[Obsolete("BinaryFormatter serialization is obsolete and should not be used. See https://aka.ms/binaryformatter for more information.", DiagnosticId = "SYSLIB0011", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[RequiresUnreferencedCode("BinaryFormatter serialization is not trim compatible because the Type of objects being processed cannot be statically discovered.")]
	public abstract object Deserialize(Stream serializationStream);

	protected virtual object? GetNext(out long objID)
	{
		if (m_objectQueue.Count == 0)
		{
			objID = 0L;
			return null;
		}
		object obj = m_objectQueue.Dequeue();
		objID = m_idGenerator.HasId(obj, out var firstTime);
		if (firstTime)
		{
			throw new SerializationException(System.SR.Serialization_NoID);
		}
		return obj;
	}

	protected virtual long Schedule(object? obj)
	{
		if (obj == null)
		{
			return 0L;
		}
		bool firstTime;
		long id = m_idGenerator.GetId(obj, out firstTime);
		if (firstTime)
		{
			m_objectQueue.Enqueue(obj);
		}
		return id;
	}

	[Obsolete("BinaryFormatter serialization is obsolete and should not be used. See https://aka.ms/binaryformatter for more information.", DiagnosticId = "SYSLIB0011", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[RequiresUnreferencedCode("BinaryFormatter serialization is not trim compatible because the Type of objects being processed cannot be statically discovered.")]
	public abstract void Serialize(Stream serializationStream, object graph);

	protected abstract void WriteArray(object obj, string name, Type memberType);

	protected abstract void WriteBoolean(bool val, string name);

	protected abstract void WriteByte(byte val, string name);

	protected abstract void WriteChar(char val, string name);

	protected abstract void WriteDateTime(DateTime val, string name);

	protected abstract void WriteDecimal(decimal val, string name);

	protected abstract void WriteDouble(double val, string name);

	protected abstract void WriteInt16(short val, string name);

	protected abstract void WriteInt32(int val, string name);

	protected abstract void WriteInt64(long val, string name);

	protected abstract void WriteObjectRef(object? obj, string name, Type memberType);

	protected virtual void WriteMember(string memberName, object? data)
	{
		if (data == null)
		{
			WriteObjectRef(data, memberName, typeof(object));
			return;
		}
		Type type = data.GetType();
		if (type == typeof(bool))
		{
			WriteBoolean(Convert.ToBoolean(data, CultureInfo.InvariantCulture), memberName);
		}
		else if (type == typeof(char))
		{
			WriteChar(Convert.ToChar(data, CultureInfo.InvariantCulture), memberName);
		}
		else if (type == typeof(sbyte))
		{
			WriteSByte(Convert.ToSByte(data, CultureInfo.InvariantCulture), memberName);
		}
		else if (type == typeof(byte))
		{
			WriteByte(Convert.ToByte(data, CultureInfo.InvariantCulture), memberName);
		}
		else if (type == typeof(short))
		{
			WriteInt16(Convert.ToInt16(data, CultureInfo.InvariantCulture), memberName);
		}
		else if (type == typeof(int))
		{
			WriteInt32(Convert.ToInt32(data, CultureInfo.InvariantCulture), memberName);
		}
		else if (type == typeof(long))
		{
			WriteInt64(Convert.ToInt64(data, CultureInfo.InvariantCulture), memberName);
		}
		else if (type == typeof(float))
		{
			WriteSingle(Convert.ToSingle(data, CultureInfo.InvariantCulture), memberName);
		}
		else if (type == typeof(double))
		{
			WriteDouble(Convert.ToDouble(data, CultureInfo.InvariantCulture), memberName);
		}
		else if (type == typeof(DateTime))
		{
			WriteDateTime(Convert.ToDateTime(data, CultureInfo.InvariantCulture), memberName);
		}
		else if (type == typeof(decimal))
		{
			WriteDecimal(Convert.ToDecimal(data, CultureInfo.InvariantCulture), memberName);
		}
		else if (type == typeof(ushort))
		{
			WriteUInt16(Convert.ToUInt16(data, CultureInfo.InvariantCulture), memberName);
		}
		else if (type == typeof(uint))
		{
			WriteUInt32(Convert.ToUInt32(data, CultureInfo.InvariantCulture), memberName);
		}
		else if (type == typeof(ulong))
		{
			WriteUInt64(Convert.ToUInt64(data, CultureInfo.InvariantCulture), memberName);
		}
		else if (type.IsArray)
		{
			WriteArray(data, memberName, type);
		}
		else if (type.IsValueType)
		{
			WriteValueType(data, memberName, type);
		}
		else
		{
			WriteObjectRef(data, memberName, type);
		}
	}

	[CLSCompliant(false)]
	protected abstract void WriteSByte(sbyte val, string name);

	protected abstract void WriteSingle(float val, string name);

	protected abstract void WriteTimeSpan(TimeSpan val, string name);

	[CLSCompliant(false)]
	protected abstract void WriteUInt16(ushort val, string name);

	[CLSCompliant(false)]
	protected abstract void WriteUInt32(uint val, string name);

	[CLSCompliant(false)]
	protected abstract void WriteUInt64(ulong val, string name);

	protected abstract void WriteValueType(object obj, string name, Type memberType);
}
