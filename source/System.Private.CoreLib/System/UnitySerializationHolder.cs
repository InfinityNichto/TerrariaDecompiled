using System.Runtime.Serialization;

namespace System;

[Serializable]
public sealed class UnitySerializationHolder : ISerializable, IObjectReference
{
	private readonly int _unityType;

	private readonly string _data;

	internal static void GetUnitySerializationInfo(SerializationInfo info, int unityType)
	{
		info.SetType(typeof(UnitySerializationHolder));
		info.AddValue("Data", null, typeof(string));
		info.AddValue("UnityType", unityType);
		info.AddValue("AssemblyName", string.Empty);
	}

	public UnitySerializationHolder(SerializationInfo info, StreamingContext context)
	{
		if (info == null)
		{
			throw new ArgumentNullException("info");
		}
		_unityType = info.GetInt32("UnityType");
		_data = info.GetString("Data");
	}

	public void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		throw new NotSupportedException(SR.NotSupported_UnitySerHolder);
	}

	public object GetRealObject(StreamingContext context)
	{
		if (_unityType != 2)
		{
			throw new ArgumentException(SR.Format(SR.Argument_InvalidUnity, _data ?? "UnityType"));
		}
		return DBNull.Value;
	}
}
