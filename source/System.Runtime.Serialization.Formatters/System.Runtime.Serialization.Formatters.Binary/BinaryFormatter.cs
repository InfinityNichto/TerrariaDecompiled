using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace System.Runtime.Serialization.Formatters.Binary;

public sealed class BinaryFormatter : IFormatter
{
	private static readonly ConcurrentDictionary<Type, TypeInformation> s_typeNameCache = new ConcurrentDictionary<Type, TypeInformation>();

	internal ISurrogateSelector _surrogates;

	internal StreamingContext _context;

	internal SerializationBinder _binder;

	internal FormatterTypeStyle _typeFormat = FormatterTypeStyle.TypesAlways;

	internal FormatterAssemblyStyle _assemblyFormat;

	internal TypeFilterLevel _securityLevel = TypeFilterLevel.Full;

	internal object[] _crossAppDomainArray;

	public FormatterTypeStyle TypeFormat
	{
		get
		{
			return _typeFormat;
		}
		set
		{
			_typeFormat = value;
		}
	}

	public FormatterAssemblyStyle AssemblyFormat
	{
		get
		{
			return _assemblyFormat;
		}
		set
		{
			_assemblyFormat = value;
		}
	}

	public TypeFilterLevel FilterLevel
	{
		get
		{
			return _securityLevel;
		}
		set
		{
			_securityLevel = value;
		}
	}

	public ISurrogateSelector? SurrogateSelector
	{
		get
		{
			return _surrogates;
		}
		set
		{
			_surrogates = value;
		}
	}

	public SerializationBinder? Binder
	{
		get
		{
			return _binder;
		}
		set
		{
			_binder = value;
		}
	}

	public StreamingContext Context
	{
		get
		{
			return _context;
		}
		set
		{
			_context = value;
		}
	}

	public BinaryFormatter()
		: this(null, new StreamingContext(StreamingContextStates.All))
	{
	}

	public BinaryFormatter(ISurrogateSelector? selector, StreamingContext context)
	{
		_surrogates = selector;
		_context = context;
	}

	internal static TypeInformation GetTypeInformation(Type type)
	{
		return s_typeNameCache.GetOrAdd(type, delegate(Type t)
		{
			bool hasTypeForwardedFrom;
			string clrAssemblyName = FormatterServices.GetClrAssemblyName(t, out hasTypeForwardedFrom);
			return new TypeInformation(FormatterServices.GetClrTypeFullName(t), clrAssemblyName, hasTypeForwardedFrom);
		});
	}

	[Obsolete("BinaryFormatter serialization is obsolete and should not be used. See https://aka.ms/binaryformatter for more information.", DiagnosticId = "SYSLIB0011", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[RequiresUnreferencedCode("BinaryFormatter serialization is not trim compatible because the Type of objects being processed cannot be statically discovered.")]
	public object Deserialize(Stream serializationStream)
	{
		if (!System.LocalAppContextSwitches.BinaryFormatterEnabled)
		{
			throw new NotSupportedException(System.SR.BinaryFormatter_SerializationDisallowed);
		}
		if (serializationStream == null)
		{
			throw new ArgumentNullException("serializationStream");
		}
		if (serializationStream.CanSeek && serializationStream.Length == 0L)
		{
			throw new SerializationException(System.SR.Serialization_Stream);
		}
		InternalFE formatterEnums = new InternalFE
		{
			_typeFormat = _typeFormat,
			_serializerTypeEnum = InternalSerializerTypeE.Binary,
			_assemblyFormat = _assemblyFormat,
			_securityLevel = _securityLevel
		};
		ObjectReader objectReader = new ObjectReader(serializationStream, _surrogates, _context, formatterEnums, _binder)
		{
			_crossAppDomainArray = _crossAppDomainArray
		};
		try
		{
			BinaryFormatterEventSource.Log.DeserializationStart();
			BinaryParser serParser = new BinaryParser(serializationStream, objectReader);
			return objectReader.Deserialize(serParser);
		}
		catch (SerializationException)
		{
			throw;
		}
		catch (Exception innerException)
		{
			throw new SerializationException(System.SR.Serialization_CorruptedStream, innerException);
		}
		finally
		{
			BinaryFormatterEventSource.Log.DeserializationStop();
		}
	}

	[Obsolete("BinaryFormatter serialization is obsolete and should not be used. See https://aka.ms/binaryformatter for more information.", DiagnosticId = "SYSLIB0011", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[RequiresUnreferencedCode("BinaryFormatter serialization is not trim compatible because the Type of objects being processed cannot be statically discovered.")]
	public void Serialize(Stream serializationStream, object graph)
	{
		if (!System.LocalAppContextSwitches.BinaryFormatterEnabled)
		{
			throw new NotSupportedException(System.SR.BinaryFormatter_SerializationDisallowed);
		}
		if (serializationStream == null)
		{
			throw new ArgumentNullException("serializationStream");
		}
		InternalFE formatterEnums = new InternalFE
		{
			_typeFormat = _typeFormat,
			_serializerTypeEnum = InternalSerializerTypeE.Binary,
			_assemblyFormat = _assemblyFormat
		};
		try
		{
			BinaryFormatterEventSource.Log.SerializationStart();
			ObjectWriter objectWriter = new ObjectWriter(_surrogates, _context, formatterEnums, _binder);
			BinaryFormatterWriter serWriter = new BinaryFormatterWriter(serializationStream, objectWriter, _typeFormat);
			objectWriter.Serialize(graph, serWriter);
			_crossAppDomainArray = objectWriter._crossAppDomainArray;
		}
		finally
		{
			BinaryFormatterEventSource.Log.SerializationStop();
		}
	}
}
