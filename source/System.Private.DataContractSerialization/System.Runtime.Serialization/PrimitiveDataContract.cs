using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Xml;

namespace System.Runtime.Serialization;

internal abstract class PrimitiveDataContract : DataContract
{
	private sealed class PrimitiveDataContractCriticalHelper : DataContractCriticalHelper
	{
		private MethodInfo _xmlFormatWriterMethod;

		private MethodInfo _xmlFormatContentWriterMethod;

		private MethodInfo _xmlFormatReaderMethod;

		internal MethodInfo XmlFormatWriterMethod
		{
			get
			{
				return _xmlFormatWriterMethod;
			}
			set
			{
				_xmlFormatWriterMethod = value;
			}
		}

		internal MethodInfo XmlFormatContentWriterMethod
		{
			get
			{
				return _xmlFormatContentWriterMethod;
			}
			set
			{
				_xmlFormatContentWriterMethod = value;
			}
		}

		internal MethodInfo XmlFormatReaderMethod
		{
			get
			{
				return _xmlFormatReaderMethod;
			}
			set
			{
				_xmlFormatReaderMethod = value;
			}
		}

		internal PrimitiveDataContractCriticalHelper([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] Type type, XmlDictionaryString name, XmlDictionaryString ns)
			: base(type)
		{
			SetDataContractName(name, ns);
		}
	}

	internal static readonly PrimitiveDataContract NullContract = new NullPrimitiveDataContract();

	private readonly PrimitiveDataContractCriticalHelper _helper;

	internal abstract string WriteMethodName { get; }

	internal abstract string ReadMethodName { get; }

	public override XmlDictionaryString TopLevelElementNamespace
	{
		get
		{
			return DictionaryGlobals.SerializationNamespace;
		}
		set
		{
		}
	}

	internal override bool CanContainReferences => false;

	internal override bool IsPrimitive => true;

	public override bool IsBuiltInDataContract => true;

	internal MethodInfo XmlFormatWriterMethod
	{
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		get
		{
			if (_helper.XmlFormatWriterMethod == null)
			{
				if (base.UnderlyingType.IsValueType)
				{
					_helper.XmlFormatWriterMethod = typeof(XmlWriterDelegator).GetMethod(WriteMethodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, new Type[3]
					{
						base.UnderlyingType,
						typeof(XmlDictionaryString),
						typeof(XmlDictionaryString)
					});
				}
				else
				{
					_helper.XmlFormatWriterMethod = typeof(XmlObjectSerializerWriteContext).GetMethod(WriteMethodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, new Type[4]
					{
						typeof(XmlWriterDelegator),
						base.UnderlyingType,
						typeof(XmlDictionaryString),
						typeof(XmlDictionaryString)
					});
				}
			}
			return _helper.XmlFormatWriterMethod;
		}
	}

	internal MethodInfo XmlFormatContentWriterMethod
	{
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		get
		{
			if (_helper.XmlFormatContentWriterMethod == null)
			{
				if (base.UnderlyingType.IsValueType)
				{
					_helper.XmlFormatContentWriterMethod = typeof(XmlWriterDelegator).GetMethod(WriteMethodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, new Type[1] { base.UnderlyingType });
				}
				else
				{
					_helper.XmlFormatContentWriterMethod = typeof(XmlObjectSerializerWriteContext).GetMethod(WriteMethodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, new Type[2]
					{
						typeof(XmlWriterDelegator),
						base.UnderlyingType
					});
				}
			}
			return _helper.XmlFormatContentWriterMethod;
		}
	}

	internal MethodInfo XmlFormatReaderMethod
	{
		get
		{
			if (_helper.XmlFormatReaderMethod == null)
			{
				_helper.XmlFormatReaderMethod = typeof(XmlReaderDelegator).GetMethod(ReadMethodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
			return _helper.XmlFormatReaderMethod;
		}
	}

	protected PrimitiveDataContract([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] Type type, XmlDictionaryString name, XmlDictionaryString ns)
		: base(new PrimitiveDataContractCriticalHelper(type, name, ns))
	{
		_helper = base.Helper as PrimitiveDataContractCriticalHelper;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static PrimitiveDataContract GetPrimitiveDataContract(Type type)
	{
		return DataContract.GetBuiltInDataContract(type) as PrimitiveDataContract;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static PrimitiveDataContract GetPrimitiveDataContract(string name, string ns)
	{
		return DataContract.GetBuiltInDataContract(name, ns) as PrimitiveDataContract;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public override void WriteXmlValue(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context)
	{
		xmlWriter.WriteAnyType(obj);
	}

	protected object HandleReadValue(object obj, XmlObjectSerializerReadContext context)
	{
		context.AddNewObject(obj);
		return obj;
	}

	protected bool TryReadNullAtTopLevel(XmlReaderDelegator reader)
	{
		Attributes attributes = new Attributes();
		attributes.Read(reader);
		if (attributes.Ref != Globals.NewObjectId)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(System.SR.Format(System.SR.CannotDeserializeRefAtTopLevel, attributes.Ref)));
		}
		if (attributes.XsiNil)
		{
			reader.Skip();
			return true;
		}
		return false;
	}
}
