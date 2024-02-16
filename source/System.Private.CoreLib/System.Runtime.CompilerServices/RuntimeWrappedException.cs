using System.Runtime.Serialization;

namespace System.Runtime.CompilerServices;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public sealed class RuntimeWrappedException : Exception
{
	private object _wrappedException;

	public object WrappedException => _wrappedException;

	public RuntimeWrappedException(object thrownObject)
		: base(SR.RuntimeWrappedException)
	{
		base.HResult = -2146233026;
		_wrappedException = thrownObject;
	}

	private RuntimeWrappedException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
		_wrappedException = info.GetValue("WrappedException", typeof(object));
	}

	public override void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		base.GetObjectData(info, context);
		info.AddValue("WrappedException", _wrappedException, typeof(object));
	}
}
