using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Xml;

namespace System.Runtime.Serialization;

internal sealed class EnumDataContract : DataContract
{
	private sealed class EnumDataContractCriticalHelper : DataContractCriticalHelper
	{
		private static readonly Dictionary<Type, XmlQualifiedName> s_typeToName;

		private static readonly Dictionary<XmlQualifiedName, Type> s_nameToType;

		private List<DataMember> _members;

		private List<long> _values;

		private bool _isULong;

		private bool _isFlags;

		private readonly bool _hasDataContract;

		private XmlDictionaryString[] _childElementNames;

		internal List<DataMember> Members
		{
			get
			{
				return _members;
			}
			set
			{
				_members = value;
			}
		}

		internal List<long> Values
		{
			get
			{
				return _values;
			}
			set
			{
				_values = value;
			}
		}

		internal bool IsFlags
		{
			get
			{
				return _isFlags;
			}
			set
			{
				_isFlags = value;
			}
		}

		internal bool IsULong
		{
			get
			{
				return _isULong;
			}
			set
			{
				_isULong = value;
			}
		}

		internal XmlDictionaryString[] ChildElementNames
		{
			get
			{
				return _childElementNames;
			}
			set
			{
				_childElementNames = value;
			}
		}

		static EnumDataContractCriticalHelper()
		{
			s_typeToName = new Dictionary<Type, XmlQualifiedName>();
			s_nameToType = new Dictionary<XmlQualifiedName, Type>();
			Add(typeof(sbyte), "byte");
			Add(typeof(byte), "unsignedByte");
			Add(typeof(short), "short");
			Add(typeof(ushort), "unsignedShort");
			Add(typeof(int), "int");
			Add(typeof(uint), "unsignedInt");
			Add(typeof(long), "long");
			Add(typeof(ulong), "unsignedLong");
		}

		internal static void Add(Type type, string localName)
		{
			XmlQualifiedName xmlQualifiedName = DataContract.CreateQualifiedName(localName, "http://www.w3.org/2001/XMLSchema");
			s_typeToName.Add(type, xmlQualifiedName);
			s_nameToType.Add(xmlQualifiedName, type);
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		internal EnumDataContractCriticalHelper([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
			: base(type)
		{
			base.StableName = DataContract.GetStableName(type, out _hasDataContract);
			Type underlyingType = Enum.GetUnderlyingType(type);
			ImportBaseType(underlyingType);
			IsFlags = type.IsDefined(Globals.TypeOfFlagsAttribute, inherit: false);
			ImportDataMembers();
			XmlDictionary xmlDictionary = new XmlDictionary(2 + Members.Count);
			base.Name = xmlDictionary.Add(base.StableName.Name);
			base.Namespace = xmlDictionary.Add(base.StableName.Namespace);
			_childElementNames = new XmlDictionaryString[Members.Count];
			for (int i = 0; i < Members.Count; i++)
			{
				_childElementNames[i] = xmlDictionary.Add(Members[i].Name);
			}
			if (DataContract.TryGetDCAttribute(type, out var dataContractAttribute) && dataContractAttribute.IsReference)
			{
				DataContract.ThrowInvalidDataContractException(System.SR.Format(System.SR.EnumTypeCannotHaveIsReference, DataContract.GetClrTypeFullName(type), dataContractAttribute.IsReference, false), type);
			}
		}

		private void ImportBaseType(Type baseType)
		{
			_isULong = baseType == Globals.TypeOfULong;
		}

		[MemberNotNull("_members")]
		private void ImportDataMembers()
		{
			Type underlyingType = base.UnderlyingType;
			FieldInfo[] fields = underlyingType.GetFields(BindingFlags.Static | BindingFlags.Public);
			Dictionary<string, DataMember> memberNamesTable = new Dictionary<string, DataMember>();
			List<DataMember> members = new List<DataMember>(fields.Length);
			List<long> list = new List<long>(fields.Length);
			foreach (FieldInfo fieldInfo in fields)
			{
				bool flag = false;
				if (_hasDataContract)
				{
					object[] array = fieldInfo.GetCustomAttributes(Globals.TypeOfEnumMemberAttribute, inherit: false).ToArray();
					if (array != null && array.Length != 0)
					{
						if (array.Length > 1)
						{
							ThrowInvalidDataContractException(System.SR.Format(System.SR.TooManyEnumMembers, DataContract.GetClrTypeFullName(fieldInfo.DeclaringType), fieldInfo.Name));
						}
						EnumMemberAttribute enumMemberAttribute = (EnumMemberAttribute)array[0];
						DataMember dataMember = new DataMember(fieldInfo);
						if (enumMemberAttribute.IsValueSetExplicitly)
						{
							if (enumMemberAttribute.Value == null || enumMemberAttribute.Value.Length == 0)
							{
								ThrowInvalidDataContractException(System.SR.Format(System.SR.InvalidEnumMemberValue, fieldInfo.Name, DataContract.GetClrTypeFullName(underlyingType)));
							}
							dataMember.Name = enumMemberAttribute.Value;
						}
						else
						{
							dataMember.Name = fieldInfo.Name;
						}
						ClassDataContract.CheckAndAddMember(members, dataMember, memberNamesTable);
						flag = true;
					}
					object[] array2 = fieldInfo.GetCustomAttributes(Globals.TypeOfDataMemberAttribute, inherit: false).ToArray();
					if (array2 != null && array2.Length != 0)
					{
						ThrowInvalidDataContractException(System.SR.Format(System.SR.DataMemberOnEnumField, DataContract.GetClrTypeFullName(fieldInfo.DeclaringType), fieldInfo.Name));
					}
				}
				else if (!fieldInfo.IsNotSerialized)
				{
					DataMember dataMember2 = new DataMember(fieldInfo);
					dataMember2.Name = fieldInfo.Name;
					ClassDataContract.CheckAndAddMember(members, dataMember2, memberNamesTable);
					flag = true;
				}
				if (flag)
				{
					object value = fieldInfo.GetValue(null);
					if (_isULong)
					{
						list.Add((long)Convert.ToUInt64(value, null));
					}
					else
					{
						list.Add(Convert.ToInt64(value, null));
					}
				}
			}
			Interlocked.MemoryBarrier();
			_members = members;
			_values = list;
		}
	}

	private readonly EnumDataContractCriticalHelper _helper;

	public XmlQualifiedName BaseContractName { get; set; }

	public List<DataMember> Members
	{
		get
		{
			return _helper.Members;
		}
		set
		{
			_helper.Members = value;
		}
	}

	public List<long> Values
	{
		get
		{
			return _helper.Values;
		}
		set
		{
			_helper.Values = value;
		}
	}

	public bool IsFlags
	{
		get
		{
			return _helper.IsFlags;
		}
		set
		{
			_helper.IsFlags = value;
		}
	}

	public bool IsULong
	{
		get
		{
			return _helper.IsULong;
		}
		set
		{
			_helper.IsULong = value;
		}
	}

	public XmlDictionaryString[] ChildElementNames
	{
		get
		{
			return _helper.ChildElementNames;
		}
		set
		{
			_helper.ChildElementNames = value;
		}
	}

	internal override bool CanContainReferences => false;

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal EnumDataContract(Type type)
		: base(new EnumDataContractCriticalHelper(type))
	{
		_helper = base.Helper as EnumDataContractCriticalHelper;
	}

	internal void WriteEnumValue(XmlWriterDelegator writer, object value)
	{
		long num = (IsULong ? ((long)Convert.ToUInt64(value, null)) : Convert.ToInt64(value, null));
		for (int i = 0; i < Values.Count; i++)
		{
			if (num == Values[i])
			{
				writer.WriteString(ChildElementNames[i].Value);
				return;
			}
		}
		if (IsFlags)
		{
			int num2 = -1;
			bool flag = true;
			for (int j = 0; j < Values.Count; j++)
			{
				long num3 = Values[j];
				if (num3 == 0L)
				{
					num2 = j;
					continue;
				}
				if (num == 0L)
				{
					break;
				}
				if ((num3 & num) == num3)
				{
					if (flag)
					{
						flag = false;
					}
					else
					{
						writer.WriteString(DictionaryGlobals.Space.Value);
					}
					writer.WriteString(ChildElementNames[j].Value);
					num &= ~num3;
				}
			}
			if (num != 0L)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(System.SR.Format(System.SR.InvalidEnumValueOnWrite, value, DataContract.GetClrTypeFullName(base.UnderlyingType))));
			}
			if (flag && num2 >= 0)
			{
				writer.WriteString(ChildElementNames[num2].Value);
			}
			return;
		}
		throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(System.SR.Format(System.SR.InvalidEnumValueOnWrite, value, DataContract.GetClrTypeFullName(base.UnderlyingType))));
	}

	internal object ReadEnumValue(XmlReaderDelegator reader)
	{
		string text = reader.ReadElementContentAsString();
		long num = 0L;
		int i = 0;
		if (IsFlags)
		{
			for (; i < text.Length && text[i] == ' '; i++)
			{
			}
			int num2 = i;
			int num3 = 0;
			for (; i < text.Length; i++)
			{
				if (text[i] == ' ')
				{
					num3 = i - num2;
					if (num3 > 0)
					{
						num |= ReadEnumValue(text, num2, num3);
					}
					for (i++; i < text.Length && text[i] == ' '; i++)
					{
					}
					num2 = i;
					if (i == text.Length)
					{
						break;
					}
				}
			}
			num3 = i - num2;
			if (num3 > 0)
			{
				num |= ReadEnumValue(text, num2, num3);
			}
		}
		else
		{
			if (text.Length == 0)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(System.SR.Format(System.SR.InvalidEnumValueOnRead, text, DataContract.GetClrTypeFullName(base.UnderlyingType))));
			}
			num = ReadEnumValue(text, 0, text.Length);
		}
		if (IsULong)
		{
			return Enum.ToObject(base.UnderlyingType, (object)(ulong)num);
		}
		return Enum.ToObject(base.UnderlyingType, (object)num);
	}

	private long ReadEnumValue(string value, int index, int count)
	{
		for (int i = 0; i < Members.Count; i++)
		{
			string name = Members[i].Name;
			if (name.Length == count && string.CompareOrdinal(value, index, name, 0, count) == 0)
			{
				return Values[i];
			}
		}
		throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(System.SR.Format(System.SR.InvalidEnumValueOnRead, value.Substring(index, count), DataContract.GetClrTypeFullName(base.UnderlyingType))));
	}

	internal string GetStringFromEnumValue(long value)
	{
		if (IsULong)
		{
			return XmlConvert.ToString((ulong)value);
		}
		return XmlConvert.ToString(value);
	}

	internal long GetEnumValueFromString(string value)
	{
		if (IsULong)
		{
			return (long)XmlConverter.ToUInt64(value);
		}
		return XmlConverter.ToInt64(value);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public override void WriteXmlValue(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context)
	{
		WriteEnumValue(xmlWriter, obj);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public override object ReadXmlValue(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context)
	{
		object obj = ReadEnumValue(xmlReader);
		context?.AddNewObject(obj);
		return obj;
	}
}
