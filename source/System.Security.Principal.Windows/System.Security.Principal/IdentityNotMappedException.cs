using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Security.Principal;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public sealed class IdentityNotMappedException : SystemException
{
	private IdentityReferenceCollection _unmappedIdentities;

	public IdentityReferenceCollection UnmappedIdentities
	{
		get
		{
			if (_unmappedIdentities == null)
			{
				_unmappedIdentities = new IdentityReferenceCollection();
			}
			return _unmappedIdentities;
		}
	}

	public IdentityNotMappedException()
		: base(System.SR.IdentityReference_IdentityNotMapped)
	{
	}

	public IdentityNotMappedException(string? message)
		: base(message)
	{
	}

	public IdentityNotMappedException(string? message, Exception? inner)
		: base(message, inner)
	{
	}

	internal IdentityNotMappedException(string message, IdentityReferenceCollection unmappedIdentities)
		: this(message)
	{
		_unmappedIdentities = unmappedIdentities;
	}

	private IdentityNotMappedException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}

	public override void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
	{
		base.GetObjectData(serializationInfo, streamingContext);
	}
}
