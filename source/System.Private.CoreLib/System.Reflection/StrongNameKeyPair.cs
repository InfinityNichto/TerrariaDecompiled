using System.IO;
using System.Runtime.Serialization;

namespace System.Reflection;

[Obsolete("Strong name signing is not supported and throws PlatformNotSupportedException.", DiagnosticId = "SYSLIB0017", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
public class StrongNameKeyPair : IDeserializationCallback, ISerializable
{
	public byte[] PublicKey
	{
		get
		{
			throw new PlatformNotSupportedException(SR.PlatformNotSupported_StrongNameSigning);
		}
	}

	public StrongNameKeyPair(FileStream keyPairFile)
	{
		throw new PlatformNotSupportedException(SR.PlatformNotSupported_StrongNameSigning);
	}

	public StrongNameKeyPair(byte[] keyPairArray)
	{
		throw new PlatformNotSupportedException(SR.PlatformNotSupported_StrongNameSigning);
	}

	protected StrongNameKeyPair(SerializationInfo info, StreamingContext context)
	{
		throw new PlatformNotSupportedException();
	}

	public StrongNameKeyPair(string keyPairContainer)
	{
		throw new PlatformNotSupportedException(SR.PlatformNotSupported_StrongNameSigning);
	}

	void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
	{
		throw new PlatformNotSupportedException();
	}

	void IDeserializationCallback.OnDeserialization(object sender)
	{
		throw new PlatformNotSupportedException();
	}
}
