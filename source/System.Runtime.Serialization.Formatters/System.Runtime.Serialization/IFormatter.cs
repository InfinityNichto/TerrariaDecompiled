using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace System.Runtime.Serialization;

public interface IFormatter
{
	ISurrogateSelector? SurrogateSelector { get; set; }

	SerializationBinder? Binder { get; set; }

	StreamingContext Context { get; set; }

	[Obsolete("BinaryFormatter serialization is obsolete and should not be used. See https://aka.ms/binaryformatter for more information.", DiagnosticId = "SYSLIB0011", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[RequiresUnreferencedCode("BinaryFormatter serialization is not trim compatible because the Type of objects being processed cannot be statically discovered.")]
	object Deserialize(Stream serializationStream);

	[Obsolete("BinaryFormatter serialization is obsolete and should not be used. See https://aka.ms/binaryformatter for more information.", DiagnosticId = "SYSLIB0011", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[RequiresUnreferencedCode("BinaryFormatter serialization is not trim compatible because the Type of objects being processed cannot be statically discovered.")]
	void Serialize(Stream serializationStream, object graph);
}
