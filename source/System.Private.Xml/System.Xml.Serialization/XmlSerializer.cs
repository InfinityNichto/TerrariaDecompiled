using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Xml.Serialization;

public class XmlSerializer
{
	private sealed class XmlSerializerMappingKey
	{
		public XmlMapping Mapping;

		public XmlSerializerMappingKey(XmlMapping mapping)
		{
			Mapping = mapping;
		}

		public override bool Equals([NotNullWhen(true)] object obj)
		{
			if (!(obj is XmlSerializerMappingKey xmlSerializerMappingKey))
			{
				return false;
			}
			if (Mapping.Key != xmlSerializerMappingKey.Mapping.Key)
			{
				return false;
			}
			if (Mapping.ElementName != xmlSerializerMappingKey.Mapping.ElementName)
			{
				return false;
			}
			if (Mapping.Namespace != xmlSerializerMappingKey.Mapping.Namespace)
			{
				return false;
			}
			if (Mapping.IsSoap != xmlSerializerMappingKey.Mapping.IsSoap)
			{
				return false;
			}
			return true;
		}

		public override int GetHashCode()
		{
			int num = ((!Mapping.IsSoap) ? 1 : 0);
			if (Mapping.Key != null)
			{
				num ^= Mapping.Key.GetHashCode();
			}
			if (Mapping.ElementName != null)
			{
				num ^= Mapping.ElementName.GetHashCode();
			}
			if (Mapping.Namespace != null)
			{
				num ^= Mapping.Namespace.GetHashCode();
			}
			return num;
		}
	}

	private static SerializationMode s_mode = SerializationMode.ReflectionAsBackup;

	private TempAssembly _tempAssembly;

	private bool _typedSerializer;

	private readonly Type _primitiveType;

	private XmlMapping _mapping;

	private XmlDeserializationEvents _events;

	internal string DefaultNamespace;

	private Type _rootType;

	private bool _isReflectionBasedSerializer;

	private static readonly TempAssemblyCache s_cache = new TempAssemblyCache();

	private static volatile XmlSerializerNamespaces s_defaultNamespaces;

	internal const string TrimSerializationWarning = "Members from serialized types may be trimmed if not referenced directly";

	private const string TrimDeserializationWarning = "Members from deserialized types may be trimmed if not referenced directly";

	private static readonly Dictionary<Type, Dictionary<XmlSerializerMappingKey, XmlSerializer>> s_xmlSerializerTable = new Dictionary<Type, Dictionary<XmlSerializerMappingKey, XmlSerializer>>();

	internal static SerializationMode Mode
	{
		get
		{
			if (!RuntimeFeature.IsDynamicCodeSupported)
			{
				return SerializationMode.ReflectionOnly;
			}
			return s_mode;
		}
		set
		{
			s_mode = value;
		}
	}

	private static bool ReflectionMethodEnabled
	{
		get
		{
			if (Mode != SerializationMode.ReflectionOnly)
			{
				return Mode == SerializationMode.ReflectionAsBackup;
			}
			return true;
		}
	}

	private static XmlSerializerNamespaces DefaultNamespaces
	{
		get
		{
			if (s_defaultNamespaces == null)
			{
				XmlSerializerNamespaces xmlSerializerNamespaces = new XmlSerializerNamespaces();
				xmlSerializerNamespaces.AddInternal("xsi", "http://www.w3.org/2001/XMLSchema-instance");
				xmlSerializerNamespaces.AddInternal("xsd", "http://www.w3.org/2001/XMLSchema");
				if (s_defaultNamespaces == null)
				{
					s_defaultNamespaces = xmlSerializerNamespaces;
				}
			}
			return s_defaultNamespaces;
		}
	}

	public event XmlNodeEventHandler UnknownNode
	{
		add
		{
			ref XmlDeserializationEvents events = ref _events;
			events.OnUnknownNode = (XmlNodeEventHandler)Delegate.Combine(events.OnUnknownNode, value);
		}
		remove
		{
			ref XmlDeserializationEvents events = ref _events;
			events.OnUnknownNode = (XmlNodeEventHandler)Delegate.Remove(events.OnUnknownNode, value);
		}
	}

	public event XmlAttributeEventHandler UnknownAttribute
	{
		add
		{
			ref XmlDeserializationEvents events = ref _events;
			events.OnUnknownAttribute = (XmlAttributeEventHandler)Delegate.Combine(events.OnUnknownAttribute, value);
		}
		remove
		{
			ref XmlDeserializationEvents events = ref _events;
			events.OnUnknownAttribute = (XmlAttributeEventHandler)Delegate.Remove(events.OnUnknownAttribute, value);
		}
	}

	public event XmlElementEventHandler UnknownElement
	{
		add
		{
			ref XmlDeserializationEvents events = ref _events;
			events.OnUnknownElement = (XmlElementEventHandler)Delegate.Combine(events.OnUnknownElement, value);
		}
		remove
		{
			ref XmlDeserializationEvents events = ref _events;
			events.OnUnknownElement = (XmlElementEventHandler)Delegate.Remove(events.OnUnknownElement, value);
		}
	}

	public event UnreferencedObjectEventHandler UnreferencedObject
	{
		add
		{
			ref XmlDeserializationEvents events = ref _events;
			events.OnUnreferencedObject = (UnreferencedObjectEventHandler)Delegate.Combine(events.OnUnreferencedObject, value);
		}
		remove
		{
			ref XmlDeserializationEvents events = ref _events;
			events.OnUnreferencedObject = (UnreferencedObjectEventHandler)Delegate.Remove(events.OnUnreferencedObject, value);
		}
	}

	protected XmlSerializer()
	{
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public XmlSerializer(Type type, XmlAttributeOverrides? overrides, Type[]? extraTypes, XmlRootAttribute? root, string? defaultNamespace)
		: this(type, overrides, extraTypes, root, defaultNamespace, null)
	{
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public XmlSerializer(Type type, XmlRootAttribute? root)
		: this(type, null, Type.EmptyTypes, root, null, null)
	{
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public XmlSerializer(Type type, Type[]? extraTypes)
		: this(type, null, extraTypes, null, null, null)
	{
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public XmlSerializer(Type type, XmlAttributeOverrides? overrides)
		: this(type, overrides, Type.EmptyTypes, null, null, null)
	{
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public XmlSerializer(XmlTypeMapping xmlTypeMapping)
	{
		if (xmlTypeMapping == null)
		{
			throw new ArgumentNullException("xmlTypeMapping");
		}
		if (Mode != SerializationMode.ReflectionOnly)
		{
			_tempAssembly = GenerateTempAssembly(xmlTypeMapping);
		}
		_mapping = xmlTypeMapping;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public XmlSerializer(Type type)
		: this(type, (string?)null)
	{
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public XmlSerializer(Type type, string? defaultNamespace)
	{
		if (type == null)
		{
			throw new ArgumentNullException("type");
		}
		DefaultNamespace = defaultNamespace;
		_rootType = type;
		_mapping = GetKnownMapping(type, defaultNamespace);
		if (_mapping != null)
		{
			_primitiveType = type;
		}
		else
		{
			if (Mode == SerializationMode.ReflectionOnly)
			{
				return;
			}
			_tempAssembly = s_cache[defaultNamespace, type];
			if (_tempAssembly == null)
			{
				lock (s_cache)
				{
					_tempAssembly = s_cache[defaultNamespace, type];
					if (_tempAssembly == null)
					{
						XmlSerializerImplementation contract = null;
						Assembly assembly = TempAssembly.LoadGeneratedAssembly(type, defaultNamespace, out contract);
						if (assembly == null)
						{
							if (Mode == SerializationMode.PreGenOnly)
							{
								AssemblyName name = type.Assembly.GetName();
								string tempAssemblyName = Compiler.GetTempAssemblyName(name, defaultNamespace);
								throw new FileLoadException(System.SR.Format(System.SR.FailLoadAssemblyUnderPregenMode, tempAssemblyName));
							}
							XmlReflectionImporter xmlReflectionImporter = new XmlReflectionImporter(defaultNamespace);
							_mapping = xmlReflectionImporter.ImportTypeMapping(type, null, defaultNamespace);
							_tempAssembly = GenerateTempAssembly(_mapping, type, defaultNamespace);
						}
						else
						{
							_mapping = XmlReflectionImporter.GetTopLevelMapping(type, defaultNamespace);
							_tempAssembly = new TempAssembly(new XmlMapping[1] { _mapping }, assembly, contract);
						}
					}
					s_cache.Add(defaultNamespace, type, _tempAssembly);
				}
			}
			if (_mapping == null)
			{
				_mapping = XmlReflectionImporter.GetTopLevelMapping(type, defaultNamespace);
			}
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public XmlSerializer(Type type, XmlAttributeOverrides? overrides, Type[]? extraTypes, XmlRootAttribute? root, string? defaultNamespace, string? location)
	{
		if (type == null)
		{
			throw new ArgumentNullException("type");
		}
		DefaultNamespace = defaultNamespace;
		_rootType = type;
		_mapping = GenerateXmlTypeMapping(type, overrides, extraTypes, root, defaultNamespace);
		if (Mode != SerializationMode.ReflectionOnly)
		{
			_tempAssembly = GenerateTempAssembly(_mapping, type, defaultNamespace, location);
		}
	}

	[RequiresUnreferencedCode("calls ImportTypeMapping")]
	private XmlTypeMapping GenerateXmlTypeMapping(Type type, XmlAttributeOverrides overrides, Type[] extraTypes, XmlRootAttribute root, string defaultNamespace)
	{
		XmlReflectionImporter xmlReflectionImporter = new XmlReflectionImporter(overrides, defaultNamespace);
		if (extraTypes != null)
		{
			for (int i = 0; i < extraTypes.Length; i++)
			{
				xmlReflectionImporter.IncludeType(extraTypes[i]);
			}
		}
		return xmlReflectionImporter.ImportTypeMapping(type, root, defaultNamespace);
	}

	[RequiresUnreferencedCode("creates TempAssembly")]
	internal static TempAssembly GenerateTempAssembly(XmlMapping xmlMapping)
	{
		return GenerateTempAssembly(xmlMapping, null, null);
	}

	[RequiresUnreferencedCode("creates TempAssembly")]
	internal static TempAssembly GenerateTempAssembly(XmlMapping xmlMapping, Type type, string defaultNamespace)
	{
		return GenerateTempAssembly(xmlMapping, type, defaultNamespace, null);
	}

	[RequiresUnreferencedCode("creates TempAssembly")]
	internal static TempAssembly GenerateTempAssembly(XmlMapping xmlMapping, Type type, string defaultNamespace, string location)
	{
		if (xmlMapping == null)
		{
			throw new ArgumentNullException("xmlMapping");
		}
		xmlMapping.CheckShallow();
		if (xmlMapping.IsSoap)
		{
			return null;
		}
		return new TempAssembly(new XmlMapping[1] { xmlMapping }, new Type[1] { type }, defaultNamespace, location);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public void Serialize(TextWriter textWriter, object? o)
	{
		Serialize(textWriter, o, null);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public void Serialize(TextWriter textWriter, object? o, XmlSerializerNamespaces? namespaces)
	{
		XmlWriter xmlWriter = XmlWriter.Create(textWriter);
		Serialize(xmlWriter, o, namespaces);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public void Serialize(Stream stream, object? o)
	{
		Serialize(stream, o, null);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public void Serialize(Stream stream, object? o, XmlSerializerNamespaces? namespaces)
	{
		XmlWriter xmlWriter = XmlWriter.Create(stream);
		Serialize(xmlWriter, o, namespaces);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public void Serialize(XmlWriter xmlWriter, object? o)
	{
		Serialize(xmlWriter, o, null);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public void Serialize(XmlWriter xmlWriter, object? o, XmlSerializerNamespaces? namespaces)
	{
		Serialize(xmlWriter, o, namespaces, null);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public void Serialize(XmlWriter xmlWriter, object? o, XmlSerializerNamespaces? namespaces, string? encodingStyle)
	{
		Serialize(xmlWriter, o, namespaces, encodingStyle, null);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public void Serialize(XmlWriter xmlWriter, object? o, XmlSerializerNamespaces? namespaces, string? encodingStyle, string? id)
	{
		try
		{
			if (_primitiveType != null)
			{
				if (encodingStyle != null && encodingStyle.Length > 0)
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.XmlInvalidEncodingNotEncoded1, encodingStyle));
				}
				SerializePrimitive(xmlWriter, o, namespaces);
			}
			else if (ShouldUseReflectionBasedSerialization(_mapping) || _isReflectionBasedSerializer)
			{
				SerializeUsingReflection(xmlWriter, o, namespaces, encodingStyle, id);
			}
			else if (_tempAssembly == null || _typedSerializer)
			{
				XmlSerializationWriter xmlSerializationWriter = CreateWriter();
				xmlSerializationWriter.Init(xmlWriter, (namespaces == null || namespaces.Count == 0) ? DefaultNamespaces : namespaces, encodingStyle, id, _tempAssembly);
				try
				{
					Serialize(o, xmlSerializationWriter);
				}
				finally
				{
					xmlSerializationWriter.Dispose();
				}
			}
			else
			{
				_tempAssembly.InvokeWriter(_mapping, xmlWriter, o, (namespaces == null || namespaces.Count == 0) ? DefaultNamespaces : namespaces, encodingStyle, id);
			}
		}
		catch (Exception innerException)
		{
			if (innerException is TargetInvocationException)
			{
				innerException = innerException.InnerException;
			}
			throw new InvalidOperationException(System.SR.XmlGenError, innerException);
		}
		xmlWriter.Flush();
	}

	[RequiresUnreferencedCode("calls GetMapping")]
	private void SerializeUsingReflection(XmlWriter xmlWriter, object o, XmlSerializerNamespaces namespaces, string encodingStyle, string id)
	{
		XmlMapping mapping = GetMapping();
		ReflectionXmlSerializationWriter reflectionXmlSerializationWriter = new ReflectionXmlSerializationWriter(mapping, xmlWriter, (namespaces == null || namespaces.Count == 0) ? DefaultNamespaces : namespaces, encodingStyle, id);
		reflectionXmlSerializationWriter.WriteObject(o);
	}

	[RequiresUnreferencedCode("calls GenerateXmlTypeMapping")]
	private XmlMapping GetMapping()
	{
		if (_mapping == null || !_mapping.GenerateSerializer)
		{
			_mapping = GenerateXmlTypeMapping(_rootType, null, null, null, DefaultNamespace);
		}
		return _mapping;
	}

	[RequiresUnreferencedCode("Members from deserialized types may be trimmed if not referenced directly")]
	public object? Deserialize(Stream stream)
	{
		XmlReader xmlReader = XmlReader.Create(stream, new XmlReaderSettings
		{
			IgnoreWhitespace = true
		});
		return Deserialize(xmlReader, null);
	}

	[RequiresUnreferencedCode("Members from deserialized types may be trimmed if not referenced directly")]
	public object? Deserialize(TextReader textReader)
	{
		XmlTextReader xmlTextReader = new XmlTextReader(textReader);
		xmlTextReader.WhitespaceHandling = WhitespaceHandling.Significant;
		xmlTextReader.Normalization = true;
		xmlTextReader.XmlResolver = null;
		return Deserialize(xmlTextReader, null);
	}

	[RequiresUnreferencedCode("Members from deserialized types may be trimmed if not referenced directly")]
	public object? Deserialize(XmlReader xmlReader)
	{
		return Deserialize(xmlReader, null);
	}

	[RequiresUnreferencedCode("Members from deserialized types may be trimmed if not referenced directly")]
	public object? Deserialize(XmlReader xmlReader, XmlDeserializationEvents events)
	{
		return Deserialize(xmlReader, null, events);
	}

	[RequiresUnreferencedCode("Members from deserialized types may be trimmed if not referenced directly")]
	public object? Deserialize(XmlReader xmlReader, string? encodingStyle)
	{
		return Deserialize(xmlReader, encodingStyle, _events);
	}

	[RequiresUnreferencedCode("Members from deserialized types may be trimmed if not referenced directly")]
	public object? Deserialize(XmlReader xmlReader, string? encodingStyle, XmlDeserializationEvents events)
	{
		events.sender = this;
		try
		{
			if (_primitiveType != null)
			{
				if (encodingStyle != null && encodingStyle.Length > 0)
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.XmlInvalidEncodingNotEncoded1, encodingStyle));
				}
				return DeserializePrimitive(xmlReader, events);
			}
			if (ShouldUseReflectionBasedSerialization(_mapping) || _isReflectionBasedSerializer)
			{
				return DeserializeUsingReflection(xmlReader, encodingStyle, events);
			}
			if (_tempAssembly == null || _typedSerializer)
			{
				XmlSerializationReader xmlSerializationReader = CreateReader();
				xmlSerializationReader.Init(xmlReader, events, encodingStyle, _tempAssembly);
				try
				{
					return Deserialize(xmlSerializationReader);
				}
				finally
				{
					xmlSerializationReader.Dispose();
				}
			}
			return _tempAssembly.InvokeReader(_mapping, xmlReader, events, encodingStyle);
		}
		catch (Exception innerException)
		{
			if (innerException is TargetInvocationException)
			{
				innerException = innerException.InnerException;
			}
			if (xmlReader is IXmlLineInfo)
			{
				IXmlLineInfo xmlLineInfo = (IXmlLineInfo)xmlReader;
				throw new InvalidOperationException(System.SR.Format(System.SR.XmlSerializeErrorDetails, xmlLineInfo.LineNumber.ToString(CultureInfo.InvariantCulture), xmlLineInfo.LinePosition.ToString(CultureInfo.InvariantCulture)), innerException);
			}
			throw new InvalidOperationException(System.SR.XmlSerializeError, innerException);
		}
	}

	[RequiresUnreferencedCode("calls GetMapping")]
	private object DeserializeUsingReflection(XmlReader xmlReader, string encodingStyle, XmlDeserializationEvents events)
	{
		XmlMapping mapping = GetMapping();
		ReflectionXmlSerializationReader reflectionXmlSerializationReader = new ReflectionXmlSerializationReader(mapping, xmlReader, events, encodingStyle);
		return reflectionXmlSerializationReader.ReadObject();
	}

	private static bool ShouldUseReflectionBasedSerialization(XmlMapping mapping)
	{
		if (Mode != SerializationMode.ReflectionOnly)
		{
			return mapping?.IsSoap ?? false;
		}
		return true;
	}

	public virtual bool CanDeserialize(XmlReader xmlReader)
	{
		if (_primitiveType != null)
		{
			TypeDesc typeDesc = (TypeDesc)TypeScope.PrimtiveTypes[_primitiveType];
			return xmlReader.IsStartElement(typeDesc.DataType.Name, string.Empty);
		}
		if (ShouldUseReflectionBasedSerialization(_mapping) || _isReflectionBasedSerializer)
		{
			return true;
		}
		if (_tempAssembly != null)
		{
			return _tempAssembly.CanRead(_mapping, xmlReader);
		}
		return false;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public static XmlSerializer[] FromMappings(XmlMapping[]? mappings)
	{
		return FromMappings(mappings, null);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public static XmlSerializer[] FromMappings(XmlMapping[]? mappings, Type? type)
	{
		if (mappings == null || mappings.Length == 0)
		{
			return Array.Empty<XmlSerializer>();
		}
		bool flag = false;
		foreach (XmlMapping xmlMapping in mappings)
		{
			if (xmlMapping.IsSoap)
			{
				flag = true;
			}
		}
		if ((flag && ReflectionMethodEnabled) || Mode == SerializationMode.ReflectionOnly)
		{
			return GetReflectionBasedSerializers(mappings, type);
		}
		XmlSerializerImplementation contract = null;
		Assembly assembly = ((type == null) ? null : TempAssembly.LoadGeneratedAssembly(type, null, out contract));
		TempAssembly tempAssembly = null;
		if (assembly == null)
		{
			if (Mode == SerializationMode.PreGenOnly)
			{
				AssemblyName name = type.Assembly.GetName();
				string tempAssemblyName = Compiler.GetTempAssemblyName(name, null);
				throw new FileLoadException(System.SR.Format(System.SR.FailLoadAssemblyUnderPregenMode, tempAssemblyName));
			}
			if (XmlMapping.IsShallow(mappings))
			{
				return Array.Empty<XmlSerializer>();
			}
			if (type == null)
			{
				tempAssembly = new TempAssembly(mappings, new Type[1] { type }, null, null);
				XmlSerializer[] array = new XmlSerializer[mappings.Length];
				contract = tempAssembly.Contract;
				for (int j = 0; j < array.Length; j++)
				{
					array[j] = (XmlSerializer)contract.TypedSerializers[mappings[j].Key];
					array[j].SetTempAssembly(tempAssembly, mappings[j]);
				}
				return array;
			}
			return GetSerializersFromCache(mappings, type);
		}
		XmlSerializer[] array2 = new XmlSerializer[mappings.Length];
		for (int k = 0; k < array2.Length; k++)
		{
			array2[k] = (XmlSerializer)contract.TypedSerializers[mappings[k].Key];
		}
		return array2;
	}

	private static XmlSerializer[] GetReflectionBasedSerializers(XmlMapping[] mappings, Type type)
	{
		XmlSerializer[] array = new XmlSerializer[mappings.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = new XmlSerializer();
			array[i]._rootType = type;
			array[i]._mapping = mappings[i];
			array[i]._isReflectionBasedSerializer = true;
		}
		return array;
	}

	[RequiresUnreferencedCode("calls GenerateSerializerToStream")]
	[UnconditionalSuppressMessage("SingleFile", "IL3000: Avoid accessing Assembly file path when publishing as a single file", Justification = "Code is used on diagnostics so we fallback to print assembly.FullName if assembly.Location is empty")]
	internal static bool GenerateSerializer(Type[] types, XmlMapping[] mappings, Stream stream)
	{
		if (types == null || types.Length == 0)
		{
			return false;
		}
		if (mappings == null)
		{
			throw new ArgumentNullException("mappings");
		}
		if (stream == null)
		{
			throw new ArgumentNullException("stream");
		}
		if (XmlMapping.IsShallow(mappings))
		{
			throw new InvalidOperationException(System.SR.XmlMelformMapping);
		}
		Assembly assembly = null;
		foreach (Type type in types)
		{
			if (DynamicAssemblies.IsTypeDynamic(type))
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.XmlPregenTypeDynamic, type.FullName));
			}
			if (assembly == null)
			{
				assembly = type.Assembly;
			}
			else if (type.Assembly != assembly)
			{
				string text = assembly.Location;
				if (text == string.Empty)
				{
					text = assembly.FullName;
				}
				throw new ArgumentException(System.SR.Format(System.SR.XmlPregenOrphanType, type.FullName, text), "types");
			}
		}
		return TempAssembly.GenerateSerializerToStream(mappings, types, null, assembly, new Hashtable(), stream);
	}

	[RequiresUnreferencedCode("calls Contract")]
	private static XmlSerializer[] GetSerializersFromCache(XmlMapping[] mappings, Type type)
	{
		XmlSerializer[] array = new XmlSerializer[mappings.Length];
		Dictionary<XmlSerializerMappingKey, XmlSerializer> value = null;
		lock (s_xmlSerializerTable)
		{
			if (!s_xmlSerializerTable.TryGetValue(type, out value))
			{
				value = new Dictionary<XmlSerializerMappingKey, XmlSerializer>();
				s_xmlSerializerTable[type] = value;
			}
		}
		lock (value)
		{
			Dictionary<XmlSerializerMappingKey, int> dictionary = new Dictionary<XmlSerializerMappingKey, int>();
			for (int i = 0; i < mappings.Length; i++)
			{
				XmlSerializerMappingKey key = new XmlSerializerMappingKey(mappings[i]);
				if (!value.TryGetValue(key, out array[i]))
				{
					dictionary.Add(key, i);
				}
			}
			if (dictionary.Count > 0)
			{
				XmlMapping[] array2 = new XmlMapping[dictionary.Count];
				int num = 0;
				foreach (XmlSerializerMappingKey key2 in dictionary.Keys)
				{
					array2[num++] = key2.Mapping;
				}
				TempAssembly tempAssembly = new TempAssembly(array2, new Type[1] { type }, null, null);
				XmlSerializerImplementation contract = tempAssembly.Contract;
				foreach (XmlSerializerMappingKey key3 in dictionary.Keys)
				{
					num = dictionary[key3];
					array[num] = (XmlSerializer)contract.TypedSerializers[key3.Mapping.Key];
					array[num].SetTempAssembly(tempAssembly, key3.Mapping);
					value[key3] = array[num];
				}
			}
		}
		return array;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public static XmlSerializer[] FromTypes(Type[]? types)
	{
		if (types == null)
		{
			return Array.Empty<XmlSerializer>();
		}
		XmlReflectionImporter xmlReflectionImporter = new XmlReflectionImporter();
		XmlTypeMapping[] array = new XmlTypeMapping[types.Length];
		for (int i = 0; i < types.Length; i++)
		{
			array[i] = xmlReflectionImporter.ImportTypeMapping(types[i]);
		}
		XmlMapping[] mappings = array;
		return FromMappings(mappings);
	}

	public static string GetXmlSerializerAssemblyName(Type type)
	{
		return GetXmlSerializerAssemblyName(type, null);
	}

	public static string GetXmlSerializerAssemblyName(Type type, string? defaultNamespace)
	{
		if (type == null)
		{
			throw new ArgumentNullException("type");
		}
		return Compiler.GetTempAssemblyName(type.Assembly.GetName(), defaultNamespace);
	}

	protected virtual XmlSerializationReader CreateReader()
	{
		throw new NotImplementedException();
	}

	protected virtual object Deserialize(XmlSerializationReader reader)
	{
		throw new NotImplementedException();
	}

	protected virtual XmlSerializationWriter CreateWriter()
	{
		throw new NotImplementedException();
	}

	protected virtual void Serialize(object? o, XmlSerializationWriter writer)
	{
		throw new NotImplementedException();
	}

	internal void SetTempAssembly(TempAssembly tempAssembly, XmlMapping mapping)
	{
		_tempAssembly = tempAssembly;
		_mapping = mapping;
		_typedSerializer = true;
	}

	private static XmlTypeMapping GetKnownMapping(Type type, string ns)
	{
		if (ns != null && ns != string.Empty)
		{
			return null;
		}
		TypeDesc typeDesc = (TypeDesc)TypeScope.PrimtiveTypes[type];
		if (typeDesc == null)
		{
			return null;
		}
		ElementAccessor elementAccessor = new ElementAccessor();
		elementAccessor.Name = typeDesc.DataType.Name;
		XmlTypeMapping xmlTypeMapping = new XmlTypeMapping(null, elementAccessor);
		xmlTypeMapping.SetKeyInternal(XmlMapping.GenerateKey(type, null, null));
		return xmlTypeMapping;
	}

	private void SerializePrimitive(XmlWriter xmlWriter, object o, XmlSerializerNamespaces namespaces)
	{
		XmlSerializationPrimitiveWriter xmlSerializationPrimitiveWriter = new XmlSerializationPrimitiveWriter();
		xmlSerializationPrimitiveWriter.Init(xmlWriter, namespaces, null, null, null);
		switch (Type.GetTypeCode(_primitiveType))
		{
		case TypeCode.String:
			xmlSerializationPrimitiveWriter.Write_string(o);
			return;
		case TypeCode.Int32:
			xmlSerializationPrimitiveWriter.Write_int(o);
			return;
		case TypeCode.Boolean:
			xmlSerializationPrimitiveWriter.Write_boolean(o);
			return;
		case TypeCode.Int16:
			xmlSerializationPrimitiveWriter.Write_short(o);
			return;
		case TypeCode.Int64:
			xmlSerializationPrimitiveWriter.Write_long(o);
			return;
		case TypeCode.Single:
			xmlSerializationPrimitiveWriter.Write_float(o);
			return;
		case TypeCode.Double:
			xmlSerializationPrimitiveWriter.Write_double(o);
			return;
		case TypeCode.Decimal:
			xmlSerializationPrimitiveWriter.Write_decimal(o);
			return;
		case TypeCode.DateTime:
			xmlSerializationPrimitiveWriter.Write_dateTime(o);
			return;
		case TypeCode.Char:
			xmlSerializationPrimitiveWriter.Write_char(o);
			return;
		case TypeCode.Byte:
			xmlSerializationPrimitiveWriter.Write_unsignedByte(o);
			return;
		case TypeCode.SByte:
			xmlSerializationPrimitiveWriter.Write_byte(o);
			return;
		case TypeCode.UInt16:
			xmlSerializationPrimitiveWriter.Write_unsignedShort(o);
			return;
		case TypeCode.UInt32:
			xmlSerializationPrimitiveWriter.Write_unsignedInt(o);
			return;
		case TypeCode.UInt64:
			xmlSerializationPrimitiveWriter.Write_unsignedLong(o);
			return;
		}
		if (_primitiveType == typeof(XmlQualifiedName))
		{
			xmlSerializationPrimitiveWriter.Write_QName(o);
			return;
		}
		if (_primitiveType == typeof(byte[]))
		{
			xmlSerializationPrimitiveWriter.Write_base64Binary(o);
			return;
		}
		if (_primitiveType == typeof(Guid))
		{
			xmlSerializationPrimitiveWriter.Write_guid(o);
			return;
		}
		if (_primitiveType == typeof(TimeSpan))
		{
			xmlSerializationPrimitiveWriter.Write_TimeSpan(o);
			return;
		}
		if (_primitiveType == typeof(DateTimeOffset))
		{
			xmlSerializationPrimitiveWriter.Write_dateTimeOffset(o);
			return;
		}
		throw new InvalidOperationException(System.SR.Format(System.SR.XmlUnxpectedType, _primitiveType.FullName));
	}

	private object DeserializePrimitive(XmlReader xmlReader, XmlDeserializationEvents events)
	{
		XmlSerializationPrimitiveReader xmlSerializationPrimitiveReader = new XmlSerializationPrimitiveReader();
		xmlSerializationPrimitiveReader.Init(xmlReader, events, null, null);
		switch (Type.GetTypeCode(_primitiveType))
		{
		case TypeCode.String:
			return xmlSerializationPrimitiveReader.Read_string();
		case TypeCode.Int32:
			return xmlSerializationPrimitiveReader.Read_int();
		case TypeCode.Boolean:
			return xmlSerializationPrimitiveReader.Read_boolean();
		case TypeCode.Int16:
			return xmlSerializationPrimitiveReader.Read_short();
		case TypeCode.Int64:
			return xmlSerializationPrimitiveReader.Read_long();
		case TypeCode.Single:
			return xmlSerializationPrimitiveReader.Read_float();
		case TypeCode.Double:
			return xmlSerializationPrimitiveReader.Read_double();
		case TypeCode.Decimal:
			return xmlSerializationPrimitiveReader.Read_decimal();
		case TypeCode.DateTime:
			return xmlSerializationPrimitiveReader.Read_dateTime();
		case TypeCode.Char:
			return xmlSerializationPrimitiveReader.Read_char();
		case TypeCode.Byte:
			return xmlSerializationPrimitiveReader.Read_unsignedByte();
		case TypeCode.SByte:
			return xmlSerializationPrimitiveReader.Read_byte();
		case TypeCode.UInt16:
			return xmlSerializationPrimitiveReader.Read_unsignedShort();
		case TypeCode.UInt32:
			return xmlSerializationPrimitiveReader.Read_unsignedInt();
		case TypeCode.UInt64:
			return xmlSerializationPrimitiveReader.Read_unsignedLong();
		default:
			if (_primitiveType == typeof(XmlQualifiedName))
			{
				return xmlSerializationPrimitiveReader.Read_QName();
			}
			if (_primitiveType == typeof(byte[]))
			{
				return xmlSerializationPrimitiveReader.Read_base64Binary();
			}
			if (_primitiveType == typeof(Guid))
			{
				return xmlSerializationPrimitiveReader.Read_guid();
			}
			if (_primitiveType == typeof(TimeSpan))
			{
				return xmlSerializationPrimitiveReader.Read_TimeSpan();
			}
			if (_primitiveType == typeof(DateTimeOffset))
			{
				return xmlSerializationPrimitiveReader.Read_dateTimeOffset();
			}
			throw new InvalidOperationException(System.SR.Format(System.SR.XmlUnxpectedType, _primitiveType.FullName));
		}
	}
}
