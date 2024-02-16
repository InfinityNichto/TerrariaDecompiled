using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace System.Runtime.Serialization;

internal abstract class DataContract
{
	internal class DataContractCriticalHelper
	{
		private static readonly Dictionary<TypeHandleRef, IntRef> s_typeToIDCache = new Dictionary<TypeHandleRef, IntRef>(new TypeHandleRefEqualityComparer());

		private static DataContract[] s_dataContractCache = new DataContract[32];

		private static int s_dataContractID;

		private static Dictionary<Type, DataContract> s_typeToBuiltInContract;

		private static Dictionary<XmlQualifiedName, DataContract> s_nameToBuiltInContract;

		private static Dictionary<string, string> s_namespaces;

		private static Dictionary<string, XmlDictionaryString> s_clrTypeStrings;

		private static XmlDictionary s_clrTypeStringsDictionary;

		private static readonly TypeHandleRef s_typeHandleRef = new TypeHandleRef();

		private static readonly object s_cacheLock = new object();

		private static readonly object s_createDataContractLock = new object();

		private static readonly object s_initBuiltInContractsLock = new object();

		private static readonly object s_namespacesLock = new object();

		private static readonly object s_clrTypeStringsLock = new object();

		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
		private Type _underlyingType;

		private Type _originalUnderlyingType;

		private bool _isReference;

		private bool _isValueType;

		private XmlQualifiedName _stableName;

		private XmlDictionaryString _name;

		private XmlDictionaryString _ns;

		private MethodInfo _parseMethod;

		private bool _parseMethodSet;

		private Type _typeForInitialization;

		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
		internal Type UnderlyingType
		{
			get
			{
				return _underlyingType;
			}
			set
			{
				_underlyingType = value;
			}
		}

		internal Type OriginalUnderlyingType
		{
			get
			{
				if (_originalUnderlyingType == null)
				{
					_originalUnderlyingType = GetDataContractOriginalType(_underlyingType);
				}
				return _originalUnderlyingType;
			}
			set
			{
				_originalUnderlyingType = value;
			}
		}

		internal virtual bool IsBuiltInDataContract => false;

		internal Type TypeForInitialization => _typeForInitialization;

		internal bool IsReference
		{
			get
			{
				return _isReference;
			}
			set
			{
				_isReference = value;
			}
		}

		internal bool IsValueType
		{
			get
			{
				return _isValueType;
			}
			set
			{
				_isValueType = value;
			}
		}

		internal XmlQualifiedName StableName
		{
			get
			{
				return _stableName;
			}
			set
			{
				_stableName = value;
			}
		}

		internal virtual Dictionary<XmlQualifiedName, DataContract> KnownDataContracts
		{
			[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
			get
			{
				return null;
			}
			set
			{
			}
		}

		internal virtual bool IsISerializable
		{
			get
			{
				return false;
			}
			set
			{
				ThrowInvalidDataContractException(System.SR.RequiresClassDataContractToSetIsISerializable);
			}
		}

		internal XmlDictionaryString Name
		{
			get
			{
				return _name;
			}
			set
			{
				_name = value;
			}
		}

		public XmlDictionaryString Namespace
		{
			get
			{
				return _ns;
			}
			set
			{
				_ns = value;
			}
		}

		internal virtual bool HasRoot
		{
			get
			{
				return true;
			}
			set
			{
			}
		}

		internal virtual XmlDictionaryString TopLevelElementName
		{
			get
			{
				return _name;
			}
			set
			{
				_name = value;
			}
		}

		internal virtual XmlDictionaryString TopLevelElementNamespace
		{
			get
			{
				return _ns;
			}
			set
			{
				_ns = value;
			}
		}

		internal virtual bool CanContainReferences => true;

		internal virtual bool IsPrimitive => false;

		internal MethodInfo ParseMethod
		{
			get
			{
				if (!_parseMethodSet)
				{
					MethodInfo method = UnderlyingType.GetMethod("Parse", BindingFlags.Static | BindingFlags.Public, new Type[1] { typeof(string) });
					if (method != null && method.ReturnType == UnderlyingType)
					{
						_parseMethod = method;
					}
					_parseMethodSet = true;
				}
				return _parseMethod;
			}
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		internal static DataContract GetDataContractSkipValidation(int id, RuntimeTypeHandle typeHandle, Type type)
		{
			DataContract dataContract = s_dataContractCache[id];
			if (dataContract == null)
			{
				dataContract = CreateDataContract(id, typeHandle, type);
				AssignDataContractToId(dataContract, id);
				return dataContract;
			}
			return dataContract.GetValidContract();
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		internal static DataContract GetGetOnlyCollectionDataContractSkipValidation(int id, RuntimeTypeHandle typeHandle, Type type)
		{
			DataContract dataContract = s_dataContractCache[id];
			if (dataContract == null)
			{
				dataContract = CreateGetOnlyCollectionDataContract(id, typeHandle, type);
				s_dataContractCache[id] = dataContract;
			}
			return dataContract;
		}

		internal static DataContract GetDataContractForInitialization(int id)
		{
			DataContract dataContract = s_dataContractCache[id];
			if (dataContract == null)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new SerializationException(System.SR.DataContractCacheOverflow));
			}
			return dataContract;
		}

		internal static int GetIdForInitialization(ClassDataContract classContract)
		{
			int id = DataContract.GetId(classContract.TypeForInitialization.TypeHandle);
			if (id < s_dataContractCache.Length && ContractMatches(classContract, s_dataContractCache[id]))
			{
				return id;
			}
			int num = s_dataContractID;
			for (int i = 0; i < num; i++)
			{
				if (ContractMatches(classContract, s_dataContractCache[i]))
				{
					return i;
				}
			}
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new SerializationException(System.SR.DataContractCacheOverflow));
		}

		private static bool ContractMatches(DataContract contract, DataContract cachedContract)
		{
			if (cachedContract != null)
			{
				return cachedContract.UnderlyingType == contract.UnderlyingType;
			}
			return false;
		}

		internal static int GetId(RuntimeTypeHandle typeHandle)
		{
			lock (s_cacheLock)
			{
				typeHandle = GetDataContractAdapterTypeHandle(typeHandle);
				s_typeHandleRef.Value = typeHandle;
				if (!s_typeToIDCache.TryGetValue(s_typeHandleRef, out var value))
				{
					int num = s_dataContractID++;
					if (num >= s_dataContractCache.Length)
					{
						int num2 = ((num < 1073741823) ? (num * 2) : int.MaxValue);
						if (num2 <= num)
						{
							throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new SerializationException(System.SR.DataContractCacheOverflow));
						}
						Array.Resize(ref s_dataContractCache, num2);
					}
					value = new IntRef(num);
					try
					{
						s_typeToIDCache.Add(new TypeHandleRef(typeHandle), value);
					}
					catch (Exception ex)
					{
						throw DiagnosticUtility.ExceptionUtility.ThrowHelperFatal(ex.Message, ex);
					}
				}
				return value.Value;
			}
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		private static DataContract CreateDataContract(int id, RuntimeTypeHandle typeHandle, Type type)
		{
			DataContract dataContract = s_dataContractCache[id];
			if (dataContract == null)
			{
				lock (s_createDataContractLock)
				{
					dataContract = s_dataContractCache[id];
					if (dataContract == null)
					{
						if (type == null)
						{
							type = Type.GetTypeFromHandle(typeHandle);
						}
						type = UnwrapNullableType(type);
						dataContract = GetDataContractFromGeneratedAssembly(type);
						if (dataContract != null)
						{
							AssignDataContractToId(dataContract, id);
							return dataContract;
						}
						dataContract = CreateDataContract(type);
					}
				}
			}
			return dataContract;
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		private static DataContract CreateDataContract(Type type)
		{
			type = UnwrapNullableType(type);
			Type type2 = type;
			type = GetDataContractAdapterType(type);
			DataContract dataContract = GetBuiltInDataContract(type);
			if (dataContract == null)
			{
				if (type.IsArray)
				{
					dataContract = new CollectionDataContract(type);
				}
				else if (type.IsEnum)
				{
					dataContract = new EnumDataContract(type);
				}
				else if (Globals.TypeOfIXmlSerializable.IsAssignableFrom(type))
				{
					dataContract = new XmlDataContract(type);
				}
				else if (Globals.TypeOfScriptObject_IsAssignableFrom(type))
				{
					dataContract = Globals.CreateScriptObjectClassDataContract();
				}
				else if (!CollectionDataContract.TryCreate(type, out dataContract))
				{
					if (!type.IsSerializable && !type.IsDefined(Globals.TypeOfDataContractAttribute, inherit: false) && !ClassDataContract.IsNonAttributedTypeValidForSerialization(type) && !ClassDataContract.IsKnownSerializableType(type))
					{
						ThrowInvalidDataContractException(System.SR.Format(System.SR.TypeNotSerializable, type), type);
					}
					dataContract = new ClassDataContract(type);
					if (type != type2)
					{
						ClassDataContract classDataContract = new ClassDataContract(type2);
						if (dataContract.StableName != classDataContract.StableName)
						{
							dataContract.StableName = classDataContract.StableName;
						}
					}
				}
			}
			return dataContract;
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static void AssignDataContractToId(DataContract dataContract, int id)
		{
			lock (s_cacheLock)
			{
				s_dataContractCache[id] = dataContract;
			}
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		private static DataContract CreateGetOnlyCollectionDataContract(int id, RuntimeTypeHandle typeHandle, Type type)
		{
			DataContract dataContract = null;
			lock (s_createDataContractLock)
			{
				dataContract = s_dataContractCache[id];
				if (dataContract == null)
				{
					if (type == null)
					{
						type = Type.GetTypeFromHandle(typeHandle);
					}
					type = UnwrapNullableType(type);
					type = GetDataContractAdapterType(type);
					if (!CollectionDataContract.TryCreateGetOnlyCollectionDataContract(type, out dataContract))
					{
						ThrowInvalidDataContractException(System.SR.Format(System.SR.TypeNotSerializable, type), type);
					}
				}
			}
			return dataContract;
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		internal static Type GetDataContractAdapterType(Type type)
		{
			if (type == Globals.TypeOfDateTimeOffset)
			{
				return Globals.TypeOfDateTimeOffsetAdapter;
			}
			if (type == Globals.TypeOfMemoryStream)
			{
				return Globals.TypeOfMemoryStreamAdapter;
			}
			if (type.IsGenericType && type.GetGenericTypeDefinition() == Globals.TypeOfKeyValuePair)
			{
				return Globals.TypeOfKeyValuePairAdapter.MakeGenericType(type.GetGenericArguments());
			}
			return type;
		}

		internal static Type GetDataContractOriginalType(Type type)
		{
			if (type == Globals.TypeOfDateTimeOffsetAdapter)
			{
				return Globals.TypeOfDateTimeOffset;
			}
			if (type == Globals.TypeOfMemoryStreamAdapter)
			{
				return Globals.TypeOfMemoryStream;
			}
			return type;
		}

		private static RuntimeTypeHandle GetDataContractAdapterTypeHandle(RuntimeTypeHandle typeHandle)
		{
			if (Globals.TypeOfDateTimeOffset.TypeHandle.Equals(typeHandle))
			{
				return Globals.TypeOfDateTimeOffsetAdapter.TypeHandle;
			}
			if (Globals.TypeOfMemoryStream.TypeHandle.Equals(typeHandle))
			{
				return Globals.TypeOfMemoryStreamAdapter.TypeHandle;
			}
			return typeHandle;
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		public static DataContract GetBuiltInDataContract(Type type)
		{
			if (type.IsInterface && !CollectionDataContract.IsCollectionInterface(type))
			{
				type = Globals.TypeOfObject;
			}
			lock (s_initBuiltInContractsLock)
			{
				if (s_typeToBuiltInContract == null)
				{
					s_typeToBuiltInContract = new Dictionary<Type, DataContract>();
				}
				if (!s_typeToBuiltInContract.TryGetValue(type, out var value))
				{
					TryCreateBuiltInDataContract(type, out value);
					s_typeToBuiltInContract.Add(type, value);
				}
				return value;
			}
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		public static DataContract GetBuiltInDataContract(string name, string ns)
		{
			lock (s_initBuiltInContractsLock)
			{
				if (s_nameToBuiltInContract == null)
				{
					s_nameToBuiltInContract = new Dictionary<XmlQualifiedName, DataContract>();
				}
				XmlQualifiedName key = new XmlQualifiedName(name, ns);
				if (!s_nameToBuiltInContract.TryGetValue(key, out var value))
				{
					TryCreateBuiltInDataContract(name, ns, out value);
					s_nameToBuiltInContract.Add(key, value);
				}
				return value;
			}
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		public static DataContract GetBuiltInDataContract(string typeName)
		{
			if (!typeName.StartsWith("System.", StringComparison.Ordinal))
			{
				return null;
			}
			lock (s_initBuiltInContractsLock)
			{
				if (s_nameToBuiltInContract == null)
				{
					s_nameToBuiltInContract = new Dictionary<XmlQualifiedName, DataContract>();
				}
				XmlQualifiedName key = new XmlQualifiedName(typeName);
				if (!s_nameToBuiltInContract.TryGetValue(key, out var value))
				{
					Type type = null;
					switch (typeName.Substring(7))
					{
					case "Char":
						type = typeof(char);
						break;
					case "Boolean":
						type = typeof(bool);
						break;
					case "SByte":
						type = typeof(sbyte);
						break;
					case "Byte":
						type = typeof(byte);
						break;
					case "Int16":
						type = typeof(short);
						break;
					case "UInt16":
						type = typeof(ushort);
						break;
					case "Int32":
						type = typeof(int);
						break;
					case "UInt32":
						type = typeof(uint);
						break;
					case "Int64":
						type = typeof(long);
						break;
					case "UInt64":
						type = typeof(ulong);
						break;
					case "Single":
						type = typeof(float);
						break;
					case "Double":
						type = typeof(double);
						break;
					case "Decimal":
						type = typeof(decimal);
						break;
					case "DateTime":
						type = typeof(DateTime);
						break;
					case "String":
						type = typeof(string);
						break;
					case "Byte[]":
						type = typeof(byte[]);
						break;
					case "Object":
						type = typeof(object);
						break;
					case "TimeSpan":
						type = typeof(TimeSpan);
						break;
					case "Guid":
						type = typeof(Guid);
						break;
					case "Uri":
						type = typeof(Uri);
						break;
					case "Xml.XmlQualifiedName":
						type = typeof(XmlQualifiedName);
						break;
					case "Enum":
						type = typeof(Enum);
						break;
					case "ValueType":
						type = typeof(ValueType);
						break;
					case "Array":
						type = typeof(Array);
						break;
					case "Xml.XmlElement":
						type = typeof(XmlElement);
						break;
					case "Xml.XmlNode[]":
						type = typeof(XmlNode[]);
						break;
					}
					if (type != null)
					{
						TryCreateBuiltInDataContract(type, out value);
					}
					s_nameToBuiltInContract.Add(key, value);
				}
				return value;
			}
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		public static bool TryCreateBuiltInDataContract(Type type, [NotNullWhen(true)] out DataContract dataContract)
		{
			if (type.IsEnum)
			{
				dataContract = null;
				return false;
			}
			dataContract = null;
			switch (type.GetTypeCode())
			{
			case TypeCode.Boolean:
				dataContract = new BooleanDataContract();
				break;
			case TypeCode.Byte:
				dataContract = new UnsignedByteDataContract();
				break;
			case TypeCode.Char:
				dataContract = new CharDataContract();
				break;
			case TypeCode.DateTime:
				dataContract = new DateTimeDataContract();
				break;
			case TypeCode.Decimal:
				dataContract = new DecimalDataContract();
				break;
			case TypeCode.Double:
				dataContract = new DoubleDataContract();
				break;
			case TypeCode.Int16:
				dataContract = new ShortDataContract();
				break;
			case TypeCode.Int32:
				dataContract = new IntDataContract();
				break;
			case TypeCode.Int64:
				dataContract = new LongDataContract();
				break;
			case TypeCode.SByte:
				dataContract = new SignedByteDataContract();
				break;
			case TypeCode.Single:
				dataContract = new FloatDataContract();
				break;
			case TypeCode.String:
				dataContract = new StringDataContract();
				break;
			case TypeCode.UInt16:
				dataContract = new UnsignedShortDataContract();
				break;
			case TypeCode.UInt32:
				dataContract = new UnsignedIntDataContract();
				break;
			case TypeCode.UInt64:
				dataContract = new UnsignedLongDataContract();
				break;
			default:
				if (type == typeof(byte[]))
				{
					dataContract = new ByteArrayDataContract();
				}
				else if (type == typeof(object))
				{
					dataContract = new ObjectDataContract();
				}
				else if (type == typeof(Uri))
				{
					dataContract = new UriDataContract();
				}
				else if (type == typeof(XmlQualifiedName))
				{
					dataContract = new QNameDataContract();
				}
				else if (type == typeof(TimeSpan))
				{
					dataContract = new TimeSpanDataContract();
				}
				else if (type == typeof(Guid))
				{
					dataContract = new GuidDataContract();
				}
				else if (type == typeof(Enum) || type == typeof(ValueType))
				{
					dataContract = new SpecialTypeDataContract(type, DictionaryGlobals.ObjectLocalName, DictionaryGlobals.SchemaNamespace);
				}
				else if (type == typeof(Array))
				{
					dataContract = new CollectionDataContract(type);
				}
				else if (type == typeof(XmlElement) || type == typeof(XmlNode[]))
				{
					dataContract = new XmlDataContract(type);
				}
				break;
			}
			return dataContract != null;
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		public static bool TryCreateBuiltInDataContract(string name, string ns, [NotNullWhen(true)] out DataContract dataContract)
		{
			dataContract = null;
			if (ns == DictionaryGlobals.SchemaNamespace.Value)
			{
				if (DictionaryGlobals.BooleanLocalName.Value == name)
				{
					dataContract = new BooleanDataContract();
				}
				else if (DictionaryGlobals.SignedByteLocalName.Value == name)
				{
					dataContract = new SignedByteDataContract();
				}
				else if (DictionaryGlobals.UnsignedByteLocalName.Value == name)
				{
					dataContract = new UnsignedByteDataContract();
				}
				else if (DictionaryGlobals.ShortLocalName.Value == name)
				{
					dataContract = new ShortDataContract();
				}
				else if (DictionaryGlobals.UnsignedShortLocalName.Value == name)
				{
					dataContract = new UnsignedShortDataContract();
				}
				else if (DictionaryGlobals.IntLocalName.Value == name)
				{
					dataContract = new IntDataContract();
				}
				else if (DictionaryGlobals.UnsignedIntLocalName.Value == name)
				{
					dataContract = new UnsignedIntDataContract();
				}
				else if (DictionaryGlobals.LongLocalName.Value == name)
				{
					dataContract = new LongDataContract();
				}
				else if (DictionaryGlobals.integerLocalName.Value == name)
				{
					dataContract = new IntegerDataContract();
				}
				else if (DictionaryGlobals.positiveIntegerLocalName.Value == name)
				{
					dataContract = new PositiveIntegerDataContract();
				}
				else if (DictionaryGlobals.negativeIntegerLocalName.Value == name)
				{
					dataContract = new NegativeIntegerDataContract();
				}
				else if (DictionaryGlobals.nonPositiveIntegerLocalName.Value == name)
				{
					dataContract = new NonPositiveIntegerDataContract();
				}
				else if (DictionaryGlobals.nonNegativeIntegerLocalName.Value == name)
				{
					dataContract = new NonNegativeIntegerDataContract();
				}
				else if (DictionaryGlobals.UnsignedLongLocalName.Value == name)
				{
					dataContract = new UnsignedLongDataContract();
				}
				else if (DictionaryGlobals.FloatLocalName.Value == name)
				{
					dataContract = new FloatDataContract();
				}
				else if (DictionaryGlobals.DoubleLocalName.Value == name)
				{
					dataContract = new DoubleDataContract();
				}
				else if (DictionaryGlobals.DecimalLocalName.Value == name)
				{
					dataContract = new DecimalDataContract();
				}
				else if (DictionaryGlobals.DateTimeLocalName.Value == name)
				{
					dataContract = new DateTimeDataContract();
				}
				else if (DictionaryGlobals.StringLocalName.Value == name)
				{
					dataContract = new StringDataContract();
				}
				else if (DictionaryGlobals.timeLocalName.Value == name)
				{
					dataContract = new TimeDataContract();
				}
				else if (DictionaryGlobals.dateLocalName.Value == name)
				{
					dataContract = new DateDataContract();
				}
				else if (DictionaryGlobals.hexBinaryLocalName.Value == name)
				{
					dataContract = new HexBinaryDataContract();
				}
				else if (DictionaryGlobals.gYearMonthLocalName.Value == name)
				{
					dataContract = new GYearMonthDataContract();
				}
				else if (DictionaryGlobals.gYearLocalName.Value == name)
				{
					dataContract = new GYearDataContract();
				}
				else if (DictionaryGlobals.gMonthDayLocalName.Value == name)
				{
					dataContract = new GMonthDayDataContract();
				}
				else if (DictionaryGlobals.gDayLocalName.Value == name)
				{
					dataContract = new GDayDataContract();
				}
				else if (DictionaryGlobals.gMonthLocalName.Value == name)
				{
					dataContract = new GMonthDataContract();
				}
				else if (DictionaryGlobals.normalizedStringLocalName.Value == name)
				{
					dataContract = new NormalizedStringDataContract();
				}
				else if (DictionaryGlobals.tokenLocalName.Value == name)
				{
					dataContract = new TokenDataContract();
				}
				else if (DictionaryGlobals.languageLocalName.Value == name)
				{
					dataContract = new LanguageDataContract();
				}
				else if (DictionaryGlobals.NameLocalName.Value == name)
				{
					dataContract = new NameDataContract();
				}
				else if (DictionaryGlobals.NCNameLocalName.Value == name)
				{
					dataContract = new NCNameDataContract();
				}
				else if (DictionaryGlobals.XSDIDLocalName.Value == name)
				{
					dataContract = new IDDataContract();
				}
				else if (DictionaryGlobals.IDREFLocalName.Value == name)
				{
					dataContract = new IDREFDataContract();
				}
				else if (DictionaryGlobals.IDREFSLocalName.Value == name)
				{
					dataContract = new IDREFSDataContract();
				}
				else if (DictionaryGlobals.ENTITYLocalName.Value == name)
				{
					dataContract = new ENTITYDataContract();
				}
				else if (DictionaryGlobals.ENTITIESLocalName.Value == name)
				{
					dataContract = new ENTITIESDataContract();
				}
				else if (DictionaryGlobals.NMTOKENLocalName.Value == name)
				{
					dataContract = new NMTOKENDataContract();
				}
				else if (DictionaryGlobals.NMTOKENSLocalName.Value == name)
				{
					dataContract = new NMTOKENDataContract();
				}
				else if (DictionaryGlobals.ByteArrayLocalName.Value == name)
				{
					dataContract = new ByteArrayDataContract();
				}
				else if (DictionaryGlobals.ObjectLocalName.Value == name)
				{
					dataContract = new ObjectDataContract();
				}
				else if (DictionaryGlobals.TimeSpanLocalName.Value == name)
				{
					dataContract = new XsDurationDataContract();
				}
				else if (DictionaryGlobals.UriLocalName.Value == name)
				{
					dataContract = new UriDataContract();
				}
				else if (DictionaryGlobals.QNameLocalName.Value == name)
				{
					dataContract = new QNameDataContract();
				}
			}
			else if (ns == DictionaryGlobals.SerializationNamespace.Value)
			{
				if (DictionaryGlobals.TimeSpanLocalName.Value == name)
				{
					dataContract = new TimeSpanDataContract();
				}
				else if (DictionaryGlobals.GuidLocalName.Value == name)
				{
					dataContract = new GuidDataContract();
				}
				else if (DictionaryGlobals.CharLocalName.Value == name)
				{
					dataContract = new CharDataContract();
				}
				else if ("ArrayOfanyType" == name)
				{
					dataContract = new CollectionDataContract(typeof(Array));
				}
			}
			else if (ns == DictionaryGlobals.AsmxTypesNamespace.Value)
			{
				if (DictionaryGlobals.CharLocalName.Value == name)
				{
					dataContract = new AsmxCharDataContract();
				}
				else if (DictionaryGlobals.GuidLocalName.Value == name)
				{
					dataContract = new AsmxGuidDataContract();
				}
			}
			else if (ns == "http://schemas.datacontract.org/2004/07/System.Xml")
			{
				if (name == "XmlElement")
				{
					dataContract = new XmlDataContract(typeof(XmlElement));
				}
				else if (name == "ArrayOfXmlNode")
				{
					dataContract = new XmlDataContract(typeof(XmlNode[]));
				}
			}
			return dataContract != null;
		}

		internal static string GetNamespace(string key)
		{
			lock (s_namespacesLock)
			{
				if (s_namespaces == null)
				{
					s_namespaces = new Dictionary<string, string>();
				}
				if (s_namespaces.TryGetValue(key, out var value))
				{
					return value;
				}
				try
				{
					s_namespaces.Add(key, key);
				}
				catch (Exception ex)
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperFatal(ex.Message, ex);
				}
				return key;
			}
		}

		internal static XmlDictionaryString GetClrTypeString(string key)
		{
			lock (s_clrTypeStringsLock)
			{
				if (s_clrTypeStrings == null)
				{
					s_clrTypeStringsDictionary = new XmlDictionary();
					s_clrTypeStrings = new Dictionary<string, XmlDictionaryString>();
					try
					{
						s_clrTypeStrings.Add(Globals.TypeOfInt.Assembly.FullName, s_clrTypeStringsDictionary.Add("0"));
					}
					catch (Exception ex)
					{
						throw DiagnosticUtility.ExceptionUtility.ThrowHelperFatal(ex.Message, ex);
					}
				}
				if (s_clrTypeStrings.TryGetValue(key, out var value))
				{
					return value;
				}
				value = s_clrTypeStringsDictionary.Add(key);
				try
				{
					s_clrTypeStrings.Add(key, value);
				}
				catch (Exception ex2)
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperFatal(ex2.Message, ex2);
				}
				return value;
			}
		}

		[DoesNotReturn]
		internal static void ThrowInvalidDataContractException(string message, Type type)
		{
			if (type != null)
			{
				lock (s_cacheLock)
				{
					s_typeHandleRef.Value = GetDataContractAdapterTypeHandle(type.TypeHandle);
					try
					{
						s_typeToIDCache.Remove(s_typeHandleRef);
					}
					catch (Exception ex)
					{
						throw DiagnosticUtility.ExceptionUtility.ThrowHelperFatal(ex.Message, ex);
					}
				}
			}
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(message));
		}

		internal DataContractCriticalHelper([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
		{
			_underlyingType = type;
			SetTypeForInitialization(type);
			_isValueType = type.IsValueType;
		}

		[MemberNotNull("_typeForInitialization")]
		private void SetTypeForInitialization(Type classType)
		{
			_typeForInitialization = classType;
		}

		internal void SetDataContractName(XmlQualifiedName stableName)
		{
			XmlDictionary xmlDictionary = new XmlDictionary(2);
			Name = xmlDictionary.Add(stableName.Name);
			Namespace = xmlDictionary.Add(stableName.Namespace);
			StableName = stableName;
		}

		internal void SetDataContractName(XmlDictionaryString name, XmlDictionaryString ns)
		{
			Name = name;
			Namespace = ns;
			StableName = CreateQualifiedName(name.Value, ns.Value);
		}

		[DoesNotReturn]
		internal void ThrowInvalidDataContractException(string message)
		{
			ThrowInvalidDataContractException(message, UnderlyingType);
		}
	}

	private XmlDictionaryString _name;

	private XmlDictionaryString _ns;

	private static readonly Dictionary<Type, DataContract> s_dataContracts = new Dictionary<Type, DataContract>();

	internal const string SerializerTrimmerWarning = "Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.";

	private readonly DataContractCriticalHelper _helper;

	internal MethodInfo ParseMethod => _helper.ParseMethod;

	protected DataContractCriticalHelper Helper => _helper;

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
	public Type UnderlyingType
	{
		get
		{
			return _helper.UnderlyingType;
		}
		set
		{
			_helper.UnderlyingType = value;
		}
	}

	public Type OriginalUnderlyingType
	{
		get
		{
			return _helper.OriginalUnderlyingType;
		}
		set
		{
			_helper.OriginalUnderlyingType = value;
		}
	}

	public virtual bool IsBuiltInDataContract
	{
		get
		{
			return _helper.IsBuiltInDataContract;
		}
		set
		{
		}
	}

	internal Type TypeForInitialization => _helper.TypeForInitialization;

	public bool IsValueType
	{
		get
		{
			return _helper.IsValueType;
		}
		set
		{
			_helper.IsValueType = value;
		}
	}

	public bool IsReference
	{
		get
		{
			return _helper.IsReference;
		}
		set
		{
			_helper.IsReference = value;
		}
	}

	public XmlQualifiedName StableName
	{
		get
		{
			return _helper.StableName;
		}
		set
		{
			_helper.StableName = value;
		}
	}

	public virtual Dictionary<XmlQualifiedName, DataContract> KnownDataContracts
	{
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		get
		{
			return _helper.KnownDataContracts;
		}
		set
		{
			_helper.KnownDataContracts = value;
		}
	}

	public virtual bool IsISerializable
	{
		get
		{
			return _helper.IsISerializable;
		}
		set
		{
			_helper.IsISerializable = value;
		}
	}

	public XmlDictionaryString Name
	{
		get
		{
			return _name;
		}
		set
		{
			_name = value;
		}
	}

	public virtual XmlDictionaryString Namespace
	{
		get
		{
			return _ns;
		}
		set
		{
			_ns = value;
		}
	}

	public virtual bool HasRoot
	{
		get
		{
			return true;
		}
		set
		{
		}
	}

	public virtual XmlDictionaryString TopLevelElementName
	{
		get
		{
			return _helper.TopLevelElementName;
		}
		set
		{
			_helper.TopLevelElementName = value;
		}
	}

	public virtual XmlDictionaryString TopLevelElementNamespace
	{
		get
		{
			return _helper.TopLevelElementNamespace;
		}
		set
		{
			_helper.TopLevelElementNamespace = value;
		}
	}

	internal virtual bool CanContainReferences => true;

	internal virtual bool IsPrimitive => false;

	public static Dictionary<Type, DataContract> GetDataContracts()
	{
		return s_dataContracts;
	}

	internal DataContract(DataContractCriticalHelper helper)
	{
		_helper = helper;
		_name = helper.Name;
		_ns = helper.Namespace;
	}

	private static DataContract GetGeneratedDataContract(Type type)
	{
		if (!s_dataContracts.TryGetValue(type, out var value))
		{
			return null;
		}
		return value;
	}

	internal static bool TryGetDataContractFromGeneratedAssembly(Type type, out DataContract dataContract)
	{
		dataContract = GetGeneratedDataContract(type);
		return dataContract != null;
	}

	internal static DataContract GetDataContractFromGeneratedAssembly(Type type)
	{
		return null;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static DataContract GetDataContract(Type type)
	{
		return GetDataContract(type.TypeHandle, type);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static DataContract GetDataContract(RuntimeTypeHandle typeHandle, Type type)
	{
		return GetDataContract(typeHandle, type, SerializationMode.SharedContract);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static DataContract GetDataContract(RuntimeTypeHandle typeHandle, Type type, SerializationMode mode)
	{
		int id = GetId(typeHandle);
		DataContract dataContractSkipValidation = GetDataContractSkipValidation(id, typeHandle, null);
		return dataContractSkipValidation.GetValidContract(mode);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static DataContract GetDataContract(int id, RuntimeTypeHandle typeHandle, SerializationMode mode)
	{
		DataContract dataContractSkipValidation = GetDataContractSkipValidation(id, typeHandle, null);
		return dataContractSkipValidation.GetValidContract(mode);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static DataContract GetDataContractSkipValidation(int id, RuntimeTypeHandle typeHandle, Type type)
	{
		return DataContractCriticalHelper.GetDataContractSkipValidation(id, typeHandle, type);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static DataContract GetGetOnlyCollectionDataContract(int id, RuntimeTypeHandle typeHandle, Type type, SerializationMode mode)
	{
		DataContract getOnlyCollectionDataContractSkipValidation = GetGetOnlyCollectionDataContractSkipValidation(id, typeHandle, type);
		getOnlyCollectionDataContractSkipValidation = getOnlyCollectionDataContractSkipValidation.GetValidContract(mode);
		if (getOnlyCollectionDataContractSkipValidation is ClassDataContract)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new SerializationException(System.SR.Format(System.SR.ErrorDeserializing, System.SR.Format(System.SR.ErrorTypeInfo, GetClrTypeFullName(getOnlyCollectionDataContractSkipValidation.UnderlyingType)), System.SR.Format(System.SR.NoSetMethodForProperty, string.Empty, string.Empty))));
		}
		return getOnlyCollectionDataContractSkipValidation;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static DataContract GetGetOnlyCollectionDataContractSkipValidation(int id, RuntimeTypeHandle typeHandle, Type type)
	{
		return DataContractCriticalHelper.GetGetOnlyCollectionDataContractSkipValidation(id, typeHandle, type);
	}

	internal static DataContract GetDataContractForInitialization(int id)
	{
		return DataContractCriticalHelper.GetDataContractForInitialization(id);
	}

	internal static int GetIdForInitialization(ClassDataContract classContract)
	{
		return DataContractCriticalHelper.GetIdForInitialization(classContract);
	}

	internal static int GetId(RuntimeTypeHandle typeHandle)
	{
		return DataContractCriticalHelper.GetId(typeHandle);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public static DataContract GetBuiltInDataContract(Type type)
	{
		return DataContractCriticalHelper.GetBuiltInDataContract(type);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public static DataContract GetBuiltInDataContract(string name, string ns)
	{
		return DataContractCriticalHelper.GetBuiltInDataContract(name, ns);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public static DataContract GetBuiltInDataContract(string typeName)
	{
		return DataContractCriticalHelper.GetBuiltInDataContract(typeName);
	}

	internal static string GetNamespace(string key)
	{
		return DataContractCriticalHelper.GetNamespace(key);
	}

	internal static XmlDictionaryString GetClrTypeString(string key)
	{
		return DataContractCriticalHelper.GetClrTypeString(key);
	}

	[DoesNotReturn]
	internal static void ThrowInvalidDataContractException(string message, Type type)
	{
		DataContractCriticalHelper.ThrowInvalidDataContractException(message, type);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public virtual void WriteXmlValue(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context)
	{
		throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(System.SR.Format(System.SR.UnexpectedContractType, GetClrTypeFullName(GetType()), GetClrTypeFullName(UnderlyingType))));
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public virtual object ReadXmlValue(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context)
	{
		throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(System.SR.Format(System.SR.UnexpectedContractType, GetClrTypeFullName(GetType()), GetClrTypeFullName(UnderlyingType))));
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public virtual void WriteXmlElement(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString ns)
	{
		throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(System.SR.Format(System.SR.UnexpectedContractType, GetClrTypeFullName(GetType()), GetClrTypeFullName(UnderlyingType))));
	}

	public virtual object ReadXmlElement(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context)
	{
		throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(System.SR.Format(System.SR.UnexpectedContractType, GetClrTypeFullName(GetType()), GetClrTypeFullName(UnderlyingType))));
	}

	internal virtual void WriteRootElement(XmlWriterDelegator writer, XmlDictionaryString name, XmlDictionaryString ns)
	{
		if (ns == DictionaryGlobals.SerializationNamespace && !IsPrimitive)
		{
			writer.WriteStartElement("z", name, ns);
		}
		else
		{
			writer.WriteStartElement(name, ns);
		}
	}

	internal virtual DataContract GetValidContract(SerializationMode mode)
	{
		return this;
	}

	internal virtual DataContract GetValidContract()
	{
		return this;
	}

	internal virtual bool IsValidContract(SerializationMode mode)
	{
		return true;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static bool IsTypeSerializable(Type type)
	{
		return IsTypeSerializable(type, new HashSet<Type>());
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private static bool IsTypeSerializable(Type type, HashSet<Type> previousCollectionTypes)
	{
		if (type.IsSerializable || type.IsEnum || type.IsDefined(Globals.TypeOfDataContractAttribute, inherit: false) || type.IsInterface || type.IsPointer || type == Globals.TypeOfDBNull || Globals.TypeOfIXmlSerializable.IsAssignableFrom(type))
		{
			return true;
		}
		if (CollectionDataContract.IsCollection(type, out var itemType))
		{
			ValidatePreviousCollectionTypes(type, itemType, previousCollectionTypes);
			if (IsTypeSerializable(itemType, previousCollectionTypes))
			{
				return true;
			}
		}
		if (GetBuiltInDataContract(type) == null)
		{
			return ClassDataContract.IsNonAttributedTypeValidForSerialization(type);
		}
		return true;
	}

	private static void ValidatePreviousCollectionTypes(Type collectionType, Type itemType, HashSet<Type> previousCollectionTypes)
	{
		previousCollectionTypes.Add(collectionType);
		while (itemType.IsArray)
		{
			itemType = itemType.GetElementType();
		}
		if (previousCollectionTypes.Contains(itemType))
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(System.SR.Format(System.SR.RecursiveCollectionType, GetClrTypeFullName(itemType))));
		}
	}

	internal static Type UnwrapRedundantNullableType(Type type)
	{
		Type result = type;
		while (type.IsGenericType && type.GetGenericTypeDefinition() == Globals.TypeOfNullable)
		{
			result = type;
			type = type.GetGenericArguments()[0];
		}
		return result;
	}

	internal static Type UnwrapNullableType(Type type)
	{
		while (type.IsGenericType && type.GetGenericTypeDefinition() == Globals.TypeOfNullable)
		{
			type = type.GetGenericArguments()[0];
		}
		return type;
	}

	private static bool IsAlpha(char ch)
	{
		if (ch < 'A' || ch > 'Z')
		{
			if (ch >= 'a')
			{
				return ch <= 'z';
			}
			return false;
		}
		return true;
	}

	private static bool IsDigit(char ch)
	{
		if (ch >= '0')
		{
			return ch <= '9';
		}
		return false;
	}

	private static bool IsAsciiLocalName(string localName)
	{
		if (localName.Length == 0)
		{
			return false;
		}
		if (!IsAlpha(localName[0]))
		{
			return false;
		}
		for (int i = 1; i < localName.Length; i++)
		{
			char ch = localName[i];
			if (!IsAlpha(ch) && !IsDigit(ch))
			{
				return false;
			}
		}
		return true;
	}

	internal static string EncodeLocalName(string localName)
	{
		if (IsAsciiLocalName(localName))
		{
			return localName;
		}
		if (IsValidNCName(localName))
		{
			return localName;
		}
		return XmlConvert.EncodeLocalName(localName);
	}

	internal static bool IsValidNCName(string name)
	{
		try
		{
			XmlConvert.VerifyNCName(name);
			return true;
		}
		catch (XmlException)
		{
			return false;
		}
		catch (Exception)
		{
			return false;
		}
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static XmlQualifiedName GetStableName(Type type)
	{
		bool hasDataContract;
		return GetStableName(type, out hasDataContract);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static XmlQualifiedName GetStableName(Type type, out bool hasDataContract)
	{
		type = UnwrapRedundantNullableType(type);
		DataContractAttribute dataContractAttribute;
		if (TryGetBuiltInXmlAndArrayTypeStableName(type, out var stableName))
		{
			hasDataContract = false;
		}
		else if (TryGetDCAttribute(type, out dataContractAttribute))
		{
			stableName = GetDCTypeStableName(type, dataContractAttribute);
			hasDataContract = true;
		}
		else
		{
			stableName = GetNonDCTypeStableName(type);
			hasDataContract = false;
		}
		return stableName;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private static XmlQualifiedName GetDCTypeStableName(Type type, DataContractAttribute dataContractAttribute)
	{
		string text = null;
		string text2 = null;
		if (dataContractAttribute.IsNameSetExplicitly)
		{
			text = dataContractAttribute.Name;
			if (text == null || text.Length == 0)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(System.SR.Format(System.SR.InvalidDataContractName, GetClrTypeFullName(type))));
			}
			if (type.IsGenericType && !type.IsGenericTypeDefinition)
			{
				text = ExpandGenericParameters(text, type);
			}
			text = EncodeLocalName(text);
		}
		else
		{
			text = GetDefaultStableLocalName(type);
		}
		if (dataContractAttribute.IsNamespaceSetExplicitly)
		{
			text2 = dataContractAttribute.Namespace;
			if (text2 == null)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(System.SR.Format(System.SR.InvalidDataContractNamespace, GetClrTypeFullName(type))));
			}
			CheckExplicitDataContractNamespaceUri(text2, type);
		}
		else
		{
			text2 = GetDefaultDataContractNamespace(type);
		}
		return CreateQualifiedName(text, text2);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private static XmlQualifiedName GetNonDCTypeStableName(Type type)
	{
		string text = null;
		string text2 = null;
		CollectionDataContractAttribute collectionContractAttribute;
		if (CollectionDataContract.IsCollection(type, out var itemType))
		{
			return GetCollectionStableName(type, itemType, out collectionContractAttribute);
		}
		text = GetDefaultStableLocalName(type);
		text2 = ((!ClassDataContract.IsNonAttributedTypeValidForSerialization(type)) ? GetDefaultStableNamespace(type) : GetDefaultDataContractNamespace(type));
		return CreateQualifiedName(text, text2);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private static bool TryGetBuiltInXmlAndArrayTypeStableName(Type type, [NotNullWhen(true)] out XmlQualifiedName stableName)
	{
		stableName = null;
		DataContract builtInDataContract = GetBuiltInDataContract(type);
		if (builtInDataContract != null)
		{
			stableName = builtInDataContract.StableName;
		}
		else if (Globals.TypeOfIXmlSerializable.IsAssignableFrom(type))
		{
			SchemaExporter.GetXmlTypeInfo(type, out var stableName2, out var _, out var _);
			stableName = stableName2;
		}
		else if (type.IsArray)
		{
			stableName = GetCollectionStableName(type, type.GetElementType(), out var _);
		}
		return stableName != null;
	}

	internal static bool TryGetDCAttribute(Type type, [NotNullWhen(true)] out DataContractAttribute dataContractAttribute)
	{
		dataContractAttribute = null;
		object[] array = type.GetCustomAttributes(Globals.TypeOfDataContractAttribute, inherit: false).ToArray();
		if (array != null && array.Length != 0)
		{
			dataContractAttribute = (DataContractAttribute)array[0];
		}
		return dataContractAttribute != null;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static XmlQualifiedName GetCollectionStableName(Type type, Type itemType, out CollectionDataContractAttribute collectionContractAttribute)
	{
		object[] array = type.GetCustomAttributes(Globals.TypeOfCollectionDataContractAttribute, inherit: false).ToArray();
		string text;
		string text2;
		if (array != null && array.Length != 0)
		{
			collectionContractAttribute = (CollectionDataContractAttribute)array[0];
			if (collectionContractAttribute.IsNameSetExplicitly)
			{
				text = collectionContractAttribute.Name;
				if (text == null || text.Length == 0)
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(System.SR.Format(System.SR.InvalidCollectionContractName, GetClrTypeFullName(type))));
				}
				if (type.IsGenericType && !type.IsGenericTypeDefinition)
				{
					text = ExpandGenericParameters(text, type);
				}
				text = EncodeLocalName(text);
			}
			else
			{
				text = GetDefaultStableLocalName(type);
			}
			if (collectionContractAttribute.IsNamespaceSetExplicitly)
			{
				text2 = collectionContractAttribute.Namespace;
				if (text2 == null)
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(System.SR.Format(System.SR.InvalidCollectionContractNamespace, GetClrTypeFullName(type))));
				}
				CheckExplicitDataContractNamespaceUri(text2, type);
			}
			else
			{
				text2 = GetDefaultDataContractNamespace(type);
			}
		}
		else
		{
			collectionContractAttribute = null;
			string text3 = "ArrayOf" + GetArrayPrefix(ref itemType);
			XmlQualifiedName stableName = GetStableName(itemType);
			text = text3 + stableName.Name;
			text2 = GetCollectionNamespace(stableName.Namespace);
		}
		return CreateQualifiedName(text, text2);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private static string GetArrayPrefix(ref Type itemType)
	{
		string text = string.Empty;
		while (itemType.IsArray && GetBuiltInDataContract(itemType) == null)
		{
			text += "ArrayOf";
			itemType = itemType.GetElementType();
		}
		return text;
	}

	internal static string GetCollectionNamespace(string elementNs)
	{
		if (!IsBuiltInNamespace(elementNs))
		{
			return elementNs;
		}
		return "http://schemas.microsoft.com/2003/10/Serialization/Arrays";
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static XmlQualifiedName GetDefaultStableName(Type type)
	{
		return CreateQualifiedName(GetDefaultStableLocalName(type), GetDefaultStableNamespace(type));
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private static string GetDefaultStableLocalName(Type type)
	{
		if (type.IsGenericParameter)
		{
			return "{" + type.GenericParameterPosition + "}";
		}
		string text = null;
		if (type.IsArray)
		{
			text = GetArrayPrefix(ref type);
		}
		string text2;
		if (type.DeclaringType == null)
		{
			text2 = type.Name;
		}
		else
		{
			int num = ((type.Namespace != null) ? type.Namespace.Length : 0);
			if (num > 0)
			{
				num++;
			}
			text2 = GetClrTypeFullName(type).Substring(num).Replace('+', '.');
		}
		if (text != null)
		{
			text2 = text + text2;
		}
		if (type.IsGenericType)
		{
			StringBuilder stringBuilder = new StringBuilder();
			StringBuilder stringBuilder2 = new StringBuilder();
			bool flag = true;
			int num2 = text2.IndexOf('[');
			if (num2 >= 0)
			{
				text2 = text2.Substring(0, num2);
			}
			IList<int> dataContractNameForGenericName = GetDataContractNameForGenericName(text2, stringBuilder);
			bool isGenericTypeDefinition = type.IsGenericTypeDefinition;
			Type[] genericArguments = type.GetGenericArguments();
			for (int i = 0; i < genericArguments.Length; i++)
			{
				Type type2 = genericArguments[i];
				if (isGenericTypeDefinition)
				{
					stringBuilder.Append('{').Append(i).Append('}');
					continue;
				}
				XmlQualifiedName stableName = GetStableName(type2);
				stringBuilder.Append(stableName.Name);
				stringBuilder2.Append(' ').Append(stableName.Namespace);
				if (flag)
				{
					flag = IsBuiltInNamespace(stableName.Namespace);
				}
			}
			if (isGenericTypeDefinition)
			{
				stringBuilder.Append("{#}");
			}
			else if (dataContractNameForGenericName.Count > 1 || !flag)
			{
				foreach (int item in dataContractNameForGenericName)
				{
					stringBuilder2.Insert(0, item.ToString(CultureInfo.InvariantCulture)).Insert(0, " ");
				}
				stringBuilder.Append(GetNamespacesDigest(stringBuilder2.ToString()));
			}
			text2 = stringBuilder.ToString();
		}
		return EncodeLocalName(text2);
	}

	private static string GetDefaultDataContractNamespace(Type type)
	{
		string text = type.Namespace;
		if (text == null)
		{
			text = string.Empty;
		}
		string clrNs = text;
		object[] nsAttributes = type.Module.GetCustomAttributes(typeof(ContractNamespaceAttribute)).ToArray();
		string text2 = GetGlobalDataContractNamespace(clrNs, nsAttributes);
		if (text2 == null)
		{
			string clrNs2 = text;
			nsAttributes = type.Assembly.GetCustomAttributes(typeof(ContractNamespaceAttribute)).ToArray();
			text2 = GetGlobalDataContractNamespace(clrNs2, nsAttributes);
		}
		if (text2 == null)
		{
			text2 = GetDefaultStableNamespace(type);
		}
		else
		{
			CheckExplicitDataContractNamespaceUri(text2, type);
		}
		return text2;
	}

	internal static List<int> GetDataContractNameForGenericName(string typeName, StringBuilder localName)
	{
		List<int> list = new List<int>();
		int num = 0;
		while (true)
		{
			int num2 = typeName.IndexOf('`', num);
			if (num2 < 0)
			{
				localName?.Append(typeName.AsSpan(num));
				list.Add(0);
				break;
			}
			if (localName != null)
			{
				string text = typeName.Substring(num, num2 - num);
				localName.Append(text.Equals("KeyValuePairAdapter") ? "KeyValuePair" : text);
			}
			while ((num = typeName.IndexOf('.', num + 1, num2 - num - 1)) >= 0)
			{
				list.Add(0);
			}
			num = typeName.IndexOf('.', num2);
			if (num < 0)
			{
				list.Add(int.Parse(typeName.AsSpan(num2 + 1), NumberStyles.Integer, CultureInfo.InvariantCulture));
				break;
			}
			list.Add(int.Parse(typeName.AsSpan(num2 + 1, num - num2 - 1), NumberStyles.Integer, CultureInfo.InvariantCulture));
		}
		localName?.Append("Of");
		return list;
	}

	internal static bool IsBuiltInNamespace(string ns)
	{
		if (!(ns == "http://www.w3.org/2001/XMLSchema"))
		{
			return ns == "http://schemas.microsoft.com/2003/10/Serialization/";
		}
		return true;
	}

	internal static string GetDefaultStableNamespace(Type type)
	{
		if (type.IsGenericParameter)
		{
			return "{ns}";
		}
		return GetDefaultStableNamespace(type.Namespace);
	}

	internal static XmlQualifiedName CreateQualifiedName(string localName, string ns)
	{
		return new XmlQualifiedName(localName, GetNamespace(ns));
	}

	internal static string GetDefaultStableNamespace(string clrNs)
	{
		if (clrNs == null)
		{
			clrNs = string.Empty;
		}
		return new Uri(Globals.DataContractXsdBaseNamespaceUri, clrNs).AbsoluteUri;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static void GetDefaultStableName(string fullTypeName, out string localName, out string ns)
	{
		CodeTypeReference typeReference = new CodeTypeReference(fullTypeName);
		GetDefaultStableName(typeReference, out localName, out ns);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private static void GetDefaultStableName(CodeTypeReference typeReference, out string localName, out string ns)
	{
		string baseType = typeReference.BaseType;
		DataContract builtInDataContract = GetBuiltInDataContract(baseType);
		if (builtInDataContract != null)
		{
			localName = builtInDataContract.StableName.Name;
			ns = builtInDataContract.StableName.Namespace;
			return;
		}
		GetClrNameAndNamespace(baseType, out localName, out ns);
		if (typeReference.TypeArguments.Count > 0)
		{
			StringBuilder stringBuilder = new StringBuilder();
			StringBuilder stringBuilder2 = new StringBuilder();
			bool flag = true;
			List<int> dataContractNameForGenericName = GetDataContractNameForGenericName(localName, stringBuilder);
			foreach (CodeTypeReference typeArgument in typeReference.TypeArguments)
			{
				GetDefaultStableName(typeArgument, out var localName2, out var ns2);
				stringBuilder.Append(localName2);
				stringBuilder2.Append(' ').Append(ns2);
				if (flag)
				{
					flag = IsBuiltInNamespace(ns2);
				}
			}
			if (dataContractNameForGenericName.Count > 1 || !flag)
			{
				foreach (int item in dataContractNameForGenericName)
				{
					stringBuilder2.Insert(0, item).Insert(0, ' ');
				}
				stringBuilder.Append(GetNamespacesDigest(stringBuilder2.ToString()));
			}
			localName = stringBuilder.ToString();
		}
		localName = EncodeLocalName(localName);
		ns = GetDefaultStableNamespace(ns);
	}

	private static void CheckExplicitDataContractNamespaceUri(string dataContractNs, Type type)
	{
		if (dataContractNs.Length > 0)
		{
			string text = dataContractNs.Trim();
			if (text.Length == 0 || text.IndexOf("##", StringComparison.Ordinal) != -1)
			{
				ThrowInvalidDataContractException(System.SR.Format(System.SR.DataContractNamespaceIsNotValid, dataContractNs), type);
			}
			dataContractNs = text;
		}
		if (Uri.TryCreate(dataContractNs, UriKind.RelativeOrAbsolute, out Uri result))
		{
			if (result.ToString() == "http://schemas.microsoft.com/2003/10/Serialization/")
			{
				ThrowInvalidDataContractException(System.SR.Format(System.SR.DataContractNamespaceReserved, "http://schemas.microsoft.com/2003/10/Serialization/"), type);
			}
		}
		else
		{
			ThrowInvalidDataContractException(System.SR.Format(System.SR.DataContractNamespaceIsNotValid, dataContractNs), type);
		}
	}

	internal static string GetClrTypeFullName(Type type)
	{
		if (type.IsGenericTypeDefinition || !type.ContainsGenericParameters)
		{
			return type.FullName;
		}
		return type.Namespace + "." + type.Name;
	}

	internal static void GetClrNameAndNamespace(string fullTypeName, out string localName, out string ns)
	{
		int num = fullTypeName.LastIndexOf('.');
		if (num < 0)
		{
			ns = string.Empty;
			localName = fullTypeName.Replace('+', '.');
		}
		else
		{
			ns = fullTypeName.Substring(0, num);
			localName = fullTypeName.Substring(num + 1).Replace('+', '.');
		}
		int num2 = localName.IndexOf('[');
		if (num2 >= 0)
		{
			localName = localName.Substring(0, num2);
		}
	}

	internal static string GetDataContractNamespaceFromUri(string uriString)
	{
		if (!uriString.StartsWith("http://schemas.datacontract.org/2004/07/", StringComparison.Ordinal))
		{
			return uriString;
		}
		return uriString.Substring("http://schemas.datacontract.org/2004/07/".Length);
	}

	private static string GetGlobalDataContractNamespace(string clrNs, object[] nsAttributes)
	{
		string text = null;
		for (int i = 0; i < nsAttributes.Length; i++)
		{
			ContractNamespaceAttribute contractNamespaceAttribute = (ContractNamespaceAttribute)nsAttributes[i];
			string text2 = contractNamespaceAttribute.ClrNamespace;
			if (text2 == null)
			{
				text2 = string.Empty;
			}
			if (text2 == clrNs)
			{
				if (contractNamespaceAttribute.ContractNamespace == null)
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(System.SR.Format(System.SR.InvalidGlobalDataContractNamespace, clrNs)));
				}
				if (text != null)
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(System.SR.Format(System.SR.DataContractNamespaceAlreadySet, text, contractNamespaceAttribute.ContractNamespace, clrNs)));
				}
				text = contractNamespaceAttribute.ContractNamespace;
			}
		}
		return text;
	}

	private static string GetNamespacesDigest(string namespaces)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(namespaces);
		byte[] inArray = ComputeHash(bytes);
		char[] array = new char[24];
		int num = Convert.ToBase64CharArray(inArray, 0, 6, array, 0);
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < num; i++)
		{
			char c = array[i];
			switch (c)
			{
			case '/':
				stringBuilder.Append("_S");
				break;
			case '+':
				stringBuilder.Append("_P");
				break;
			default:
				stringBuilder.Append(c);
				break;
			case '=':
				break;
			}
		}
		return stringBuilder.ToString();
	}

	private static byte[] ComputeHash(byte[] namespaces)
	{
		int[] array = new int[16]
		{
			7, 12, 17, 22, 5, 9, 14, 20, 4, 11,
			16, 23, 6, 10, 15, 21
		};
		uint[] array2 = new uint[64]
		{
			3614090360u, 3905402710u, 606105819u, 3250441966u, 4118548399u, 1200080426u, 2821735955u, 4249261313u, 1770035416u, 2336552879u,
			4294925233u, 2304563134u, 1804603682u, 4254626195u, 2792965006u, 1236535329u, 4129170786u, 3225465664u, 643717713u, 3921069994u,
			3593408605u, 38016083u, 3634488961u, 3889429448u, 568446438u, 3275163606u, 4107603335u, 1163531501u, 2850285829u, 4243563512u,
			1735328473u, 2368359562u, 4294588738u, 2272392833u, 1839030562u, 4259657740u, 2763975236u, 1272893353u, 4139469664u, 3200236656u,
			681279174u, 3936430074u, 3572445317u, 76029189u, 3654602809u, 3873151461u, 530742520u, 3299628645u, 4096336452u, 1126891415u,
			2878612391u, 4237533241u, 1700485571u, 2399980690u, 4293915773u, 2240044497u, 1873313359u, 4264355552u, 2734768916u, 1309151649u,
			4149444226u, 3174756917u, 718787259u, 3951481745u
		};
		int num = (namespaces.Length + 8) / 64 + 1;
		uint num2 = 1732584193u;
		uint num3 = 4023233417u;
		uint num4 = 2562383102u;
		uint num5 = 271733878u;
		for (int i = 0; i < num; i++)
		{
			byte[] array3 = namespaces;
			int num6 = i * 64;
			if (num6 + 64 > namespaces.Length)
			{
				array3 = new byte[64];
				for (int j = num6; j < namespaces.Length; j++)
				{
					array3[j - num6] = namespaces[j];
				}
				if (num6 <= namespaces.Length)
				{
					array3[namespaces.Length - num6] = 128;
				}
				if (i == num - 1)
				{
					array3[56] = (byte)(namespaces.Length << 3);
					array3[57] = (byte)(namespaces.Length >> 5);
					array3[58] = (byte)(namespaces.Length >> 13);
					array3[59] = (byte)(namespaces.Length >> 21);
				}
				num6 = 0;
			}
			uint num7 = num2;
			uint num8 = num3;
			uint num9 = num4;
			uint num10 = num5;
			for (int k = 0; k < 64; k++)
			{
				uint num11;
				int num12;
				if (k < 16)
				{
					num11 = (num8 & num9) | (~num8 & num10);
					num12 = k;
				}
				else if (k < 32)
				{
					num11 = (num8 & num10) | (num9 & ~num10);
					num12 = 5 * k + 1;
				}
				else if (k < 48)
				{
					num11 = num8 ^ num9 ^ num10;
					num12 = 3 * k + 5;
				}
				else
				{
					num11 = num9 ^ (num8 | ~num10);
					num12 = 7 * k;
				}
				num12 = (num12 & 0xF) * 4 + num6;
				uint num13 = num10;
				num10 = num9;
				num9 = num8;
				num8 = num7 + num11 + array2[k] + BinaryPrimitives.ReadUInt32LittleEndian(array3.AsSpan(num12));
				num8 = (num8 << array[(k & 3) | ((k >> 2) & -4)]) | (num8 >> 32 - array[(k & 3) | ((k >> 2) & -4)]);
				num8 += num9;
				num7 = num13;
			}
			num2 += num7;
			num3 += num8;
			if (i < num - 1)
			{
				num4 += num9;
				num5 += num10;
			}
		}
		return new byte[6]
		{
			(byte)num2,
			(byte)(num2 >> 8),
			(byte)(num2 >> 16),
			(byte)(num2 >> 24),
			(byte)num3,
			(byte)(num3 >> 8)
		};
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private static string ExpandGenericParameters(string format, Type type)
	{
		GenericNameProvider genericNameProvider = new GenericNameProvider(type);
		return ExpandGenericParameters(format, genericNameProvider);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static string ExpandGenericParameters(string format, IGenericNameProvider genericNameProvider)
	{
		string text = null;
		StringBuilder stringBuilder = new StringBuilder();
		IList<int> nestedParameterCounts = genericNameProvider.GetNestedParameterCounts();
		for (int i = 0; i < format.Length; i++)
		{
			char c = format[i];
			if (c == '{')
			{
				i++;
				int num = i;
				for (; i < format.Length && format[i] != '}'; i++)
				{
				}
				if (i == format.Length)
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(System.SR.Format(System.SR.GenericNameBraceMismatch, format, genericNameProvider.GetGenericTypeName())));
				}
				if (format[num] == '#' && i == num + 1)
				{
					if (nestedParameterCounts.Count <= 1 && genericNameProvider.ParametersFromBuiltInNamespaces)
					{
						continue;
					}
					if (text == null)
					{
						StringBuilder stringBuilder2 = new StringBuilder(genericNameProvider.GetNamespaces());
						foreach (int item in nestedParameterCounts)
						{
							stringBuilder2.Insert(0, item.ToString(CultureInfo.InvariantCulture)).Insert(0, " ");
						}
						text = GetNamespacesDigest(stringBuilder2.ToString());
					}
					stringBuilder.Append(text);
				}
				else
				{
					if (!int.TryParse(format.AsSpan(num, i - num), out var result) || result < 0 || result >= genericNameProvider.GetParameterCount())
					{
						throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(System.SR.Format(System.SR.GenericParameterNotValid, format.Substring(num, i - num), genericNameProvider.GetGenericTypeName(), genericNameProvider.GetParameterCount() - 1)));
					}
					stringBuilder.Append(genericNameProvider.GetParameterName(result));
				}
			}
			else
			{
				stringBuilder.Append(c);
			}
		}
		return stringBuilder.ToString();
	}

	internal static bool IsTypeNullable(Type type)
	{
		if (type.IsValueType)
		{
			if (type.IsGenericType)
			{
				return type.GetGenericTypeDefinition() == Globals.TypeOfNullable;
			}
			return false;
		}
		return true;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static Dictionary<XmlQualifiedName, DataContract> ImportKnownTypeAttributes(Type type)
	{
		Dictionary<XmlQualifiedName, DataContract> knownDataContracts = null;
		Dictionary<Type, Type> typesChecked = new Dictionary<Type, Type>();
		ImportKnownTypeAttributes(type, typesChecked, ref knownDataContracts);
		return knownDataContracts;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private static void ImportKnownTypeAttributes(Type type, Dictionary<Type, Type> typesChecked, ref Dictionary<XmlQualifiedName, DataContract> knownDataContracts)
	{
		while (type != null && IsTypeSerializable(type) && !typesChecked.ContainsKey(type))
		{
			typesChecked.Add(type, type);
			object[] array = type.GetCustomAttributes(Globals.TypeOfKnownTypeAttribute, inherit: false).ToArray();
			if (array != null)
			{
				bool flag = false;
				bool flag2 = false;
				for (int i = 0; i < array.Length; i++)
				{
					KnownTypeAttribute knownTypeAttribute = (KnownTypeAttribute)array[i];
					if (knownTypeAttribute.Type != null)
					{
						if (flag)
						{
							ThrowInvalidDataContractException(System.SR.Format(System.SR.KnownTypeAttributeOneScheme, GetClrTypeFullName(type)), type);
						}
						CheckAndAdd(knownTypeAttribute.Type, typesChecked, ref knownDataContracts);
						flag2 = true;
						continue;
					}
					if (flag || flag2)
					{
						ThrowInvalidDataContractException(System.SR.Format(System.SR.KnownTypeAttributeOneScheme, GetClrTypeFullName(type)), type);
					}
					string methodName = knownTypeAttribute.MethodName;
					if (methodName == null)
					{
						ThrowInvalidDataContractException(System.SR.Format(System.SR.KnownTypeAttributeNoData, GetClrTypeFullName(type)), type);
					}
					if (methodName.Length == 0)
					{
						ThrowInvalidDataContractException(System.SR.Format(System.SR.KnownTypeAttributeEmptyString, GetClrTypeFullName(type)), type);
					}
					MethodInfo method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
					if (method == null)
					{
						ThrowInvalidDataContractException(System.SR.Format(System.SR.KnownTypeAttributeUnknownMethod, methodName, GetClrTypeFullName(type)), type);
					}
					if (!Globals.TypeOfTypeEnumerable.IsAssignableFrom(method.ReturnType))
					{
						ThrowInvalidDataContractException(System.SR.Format(System.SR.KnownTypeAttributeReturnType, GetClrTypeFullName(type), methodName), type);
					}
					object obj = method.Invoke(null, Array.Empty<object>());
					if (obj == null)
					{
						ThrowInvalidDataContractException(System.SR.Format(System.SR.KnownTypeAttributeMethodNull, GetClrTypeFullName(type)), type);
					}
					foreach (Type item in (IEnumerable<Type>)obj)
					{
						if (item == null)
						{
							ThrowInvalidDataContractException(System.SR.Format(System.SR.KnownTypeAttributeValidMethodTypes, GetClrTypeFullName(type)), type);
						}
						CheckAndAdd(item, typesChecked, ref knownDataContracts);
					}
					flag = true;
				}
			}
			try
			{
				if (GetDataContract(type) is CollectionDataContract { IsDictionary: not false } collectionDataContract && collectionDataContract.ItemType.GetGenericTypeDefinition() == Globals.TypeOfKeyValue)
				{
					DataContract dataContract = GetDataContract(Globals.TypeOfKeyValuePair.MakeGenericType(collectionDataContract.ItemType.GetGenericArguments()));
					if (knownDataContracts == null)
					{
						knownDataContracts = new Dictionary<XmlQualifiedName, DataContract>();
					}
					knownDataContracts.TryAdd(dataContract.StableName, dataContract);
				}
			}
			catch (InvalidDataContractException)
			{
			}
			type = type.BaseType;
		}
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static void CheckAndAdd(Type type, Dictionary<Type, Type> typesChecked, [NotNullIfNotNull("nameToDataContractTable")] ref Dictionary<XmlQualifiedName, DataContract> nameToDataContractTable)
	{
		type = UnwrapNullableType(type);
		DataContract dataContract = GetDataContract(type);
		DataContract value;
		if (nameToDataContractTable == null)
		{
			nameToDataContractTable = new Dictionary<XmlQualifiedName, DataContract>();
		}
		else if (nameToDataContractTable.TryGetValue(dataContract.StableName, out value))
		{
			if (DataContractCriticalHelper.GetDataContractAdapterType(value.UnderlyingType) != DataContractCriticalHelper.GetDataContractAdapterType(type) && (!(value is ClassDataContract) || !((ClassDataContract)value).IsKeyValuePairAdapter))
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(System.SR.Format(System.SR.DupContractInKnownTypes, type, value.UnderlyingType, dataContract.StableName.Namespace, dataContract.StableName.Name)));
			}
			return;
		}
		nameToDataContractTable.Add(dataContract.StableName, dataContract);
		ImportKnownTypeAttributes(type, typesChecked, ref nameToDataContractTable);
	}

	internal static bool IsTypeVisible(Type t)
	{
		if (!t.IsVisible && !IsTypeVisibleInSerializationModule(t))
		{
			return false;
		}
		Type[] genericArguments = t.GetGenericArguments();
		foreach (Type type in genericArguments)
		{
			if (!type.IsGenericParameter && !IsTypeVisible(type))
			{
				return false;
			}
		}
		return true;
	}

	internal static bool ConstructorRequiresMemberAccess(ConstructorInfo ctor)
	{
		if (ctor != null && !ctor.IsPublic)
		{
			return !IsMemberVisibleInSerializationModule(ctor);
		}
		return false;
	}

	internal static bool MethodRequiresMemberAccess(MethodInfo method)
	{
		if (method != null && !method.IsPublic)
		{
			return !IsMemberVisibleInSerializationModule(method);
		}
		return false;
	}

	internal static bool FieldRequiresMemberAccess(FieldInfo field)
	{
		if (field != null && !field.IsPublic)
		{
			return !IsMemberVisibleInSerializationModule(field);
		}
		return false;
	}

	private static bool IsTypeVisibleInSerializationModule(Type type)
	{
		if (type.Module.Equals(typeof(DataContract).Module) || IsAssemblyFriendOfSerialization(type.Assembly))
		{
			return !type.IsNestedPrivate;
		}
		return false;
	}

	private static bool IsMemberVisibleInSerializationModule(MemberInfo member)
	{
		if (!IsTypeVisibleInSerializationModule(member.DeclaringType))
		{
			return false;
		}
		if (member is MethodInfo)
		{
			MethodInfo methodInfo = (MethodInfo)member;
			if (!methodInfo.IsAssembly)
			{
				return methodInfo.IsFamilyOrAssembly;
			}
			return true;
		}
		if (member is FieldInfo)
		{
			FieldInfo fieldInfo = (FieldInfo)member;
			if (fieldInfo.IsAssembly || fieldInfo.IsFamilyOrAssembly)
			{
				return IsTypeVisible(fieldInfo.FieldType);
			}
			return false;
		}
		if (member is ConstructorInfo)
		{
			ConstructorInfo constructorInfo = (ConstructorInfo)member;
			if (!constructorInfo.IsAssembly)
			{
				return constructorInfo.IsFamilyOrAssembly;
			}
			return true;
		}
		return false;
	}

	internal static bool IsAssemblyFriendOfSerialization(Assembly assembly)
	{
		InternalsVisibleToAttribute[] array = (InternalsVisibleToAttribute[])assembly.GetCustomAttributes(typeof(InternalsVisibleToAttribute));
		InternalsVisibleToAttribute[] array2 = array;
		foreach (InternalsVisibleToAttribute internalsVisibleToAttribute in array2)
		{
			string assemblyName = internalsVisibleToAttribute.AssemblyName;
			if (assemblyName.Trim().Equals("System.Runtime.Serialization") || Regex.IsMatch(assemblyName, "^[\\s]*System\\.Runtime\\.Serialization[\\s]*,[\\s]*PublicKey[\\s]*=[\\s]*(?i:00240000048000009400000006020000002400005253413100040000010001008d56c76f9e8649383049f383c44be0ec204181822a6c31cf5eb7ef486944d032188ea1d3920763712ccb12d75fb77e9811149e6148e5d32fbaab37611c1878ddc19e20ef135d0cb2cff2bfec3d115810c3d9069638fe4be215dbf795861920e5ab6f7db2e2ceef136ac23d5dd2bf031700aec232f6c6b1c785b4305c123b37ab)[\\s]*$"))
			{
				return true;
			}
		}
		return false;
	}

	internal static string SanitizeTypeName(string typeName)
	{
		return typeName.Replace('.', '_');
	}
}
