using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Security;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace System.Runtime.Serialization;

internal sealed class XmlDataContract : DataContract
{
	private sealed class XmlDataContractCriticalHelper : DataContractCriticalHelper
	{
		private Dictionary<XmlQualifiedName, DataContract> _knownDataContracts;

		private bool _isKnownTypeAttributeChecked;

		private XmlDictionaryString _topLevelElementName;

		private XmlDictionaryString _topLevelElementNamespace;

		private bool _isTopLevelElementNullable;

		private bool _hasRoot;

		private CreateXmlSerializableDelegate _createXmlSerializable;

		private XmlSchemaType _xsdType;

		internal override Dictionary<XmlQualifiedName, DataContract> KnownDataContracts
		{
			[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
			get
			{
				if (!_isKnownTypeAttributeChecked && base.UnderlyingType != null)
				{
					lock (this)
					{
						if (!_isKnownTypeAttributeChecked)
						{
							_knownDataContracts = DataContract.ImportKnownTypeAttributes(base.UnderlyingType);
							Interlocked.MemoryBarrier();
							_isKnownTypeAttributeChecked = true;
						}
					}
				}
				return _knownDataContracts;
			}
			set
			{
				_knownDataContracts = value;
			}
		}

		internal XmlSchemaType XsdType
		{
			get
			{
				return _xsdType;
			}
			set
			{
				_xsdType = value;
			}
		}

		internal bool IsAnonymous => _xsdType != null;

		internal override bool HasRoot
		{
			get
			{
				return _hasRoot;
			}
			set
			{
				_hasRoot = value;
			}
		}

		internal override XmlDictionaryString TopLevelElementName
		{
			get
			{
				return _topLevelElementName;
			}
			set
			{
				_topLevelElementName = value;
			}
		}

		internal override XmlDictionaryString TopLevelElementNamespace
		{
			get
			{
				return _topLevelElementNamespace;
			}
			set
			{
				_topLevelElementNamespace = value;
			}
		}

		internal bool IsTopLevelElementNullable
		{
			get
			{
				return _isTopLevelElementNullable;
			}
			set
			{
				_isTopLevelElementNullable = value;
			}
		}

		internal CreateXmlSerializableDelegate CreateXmlSerializableDelegate
		{
			get
			{
				return _createXmlSerializable;
			}
			set
			{
				_createXmlSerializable = value;
			}
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		internal XmlDataContractCriticalHelper([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
			: base(type)
		{
			if (type.IsDefined(Globals.TypeOfDataContractAttribute, inherit: false))
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(System.SR.Format(System.SR.IXmlSerializableCannotHaveDataContract, DataContract.GetClrTypeFullName(type))));
			}
			if (type.IsDefined(Globals.TypeOfCollectionDataContractAttribute, inherit: false))
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(System.SR.Format(System.SR.IXmlSerializableCannotHaveCollectionDataContract, DataContract.GetClrTypeFullName(type))));
			}
			SchemaExporter.GetXmlTypeInfo(type, out var stableName, out var _, out var hasRoot);
			base.StableName = stableName;
			HasRoot = hasRoot;
			XmlDictionary xmlDictionary = new XmlDictionary();
			base.Name = xmlDictionary.Add(base.StableName.Name);
			base.Namespace = xmlDictionary.Add(base.StableName.Namespace);
			object[] array = ((base.UnderlyingType == null) ? null : base.UnderlyingType.GetCustomAttributes(Globals.TypeOfXmlRootAttribute, inherit: false).ToArray());
			if (array == null || array.Length == 0)
			{
				if (hasRoot)
				{
					_topLevelElementName = base.Name;
					_topLevelElementNamespace = ((base.StableName.Namespace == "http://www.w3.org/2001/XMLSchema") ? DictionaryGlobals.EmptyString : base.Namespace);
					_isTopLevelElementNullable = true;
				}
				return;
			}
			if (hasRoot)
			{
				XmlRootAttribute xmlRootAttribute = (XmlRootAttribute)array[0];
				_isTopLevelElementNullable = xmlRootAttribute.IsNullable;
				string elementName = xmlRootAttribute.ElementName;
				_topLevelElementName = ((elementName == null || elementName.Length == 0) ? base.Name : xmlDictionary.Add(DataContract.EncodeLocalName(elementName)));
				string @namespace = xmlRootAttribute.Namespace;
				_topLevelElementNamespace = ((@namespace == null || @namespace.Length == 0) ? DictionaryGlobals.EmptyString : xmlDictionary.Add(@namespace));
				return;
			}
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(System.SR.Format(System.SR.IsAnyCannotHaveXmlRoot, DataContract.GetClrTypeFullName(base.UnderlyingType))));
		}
	}

	private readonly XmlDataContractCriticalHelper _helper;

	public override Dictionary<XmlQualifiedName, DataContract> KnownDataContracts
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

	internal XmlSchemaType XsdType
	{
		get
		{
			return _helper.XsdType;
		}
		set
		{
			_helper.XsdType = value;
		}
	}

	internal bool IsAnonymous => _helper.IsAnonymous;

	public override bool HasRoot
	{
		get
		{
			return _helper.HasRoot;
		}
		set
		{
			_helper.HasRoot = value;
		}
	}

	public override XmlDictionaryString TopLevelElementName
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

	public override XmlDictionaryString TopLevelElementNamespace
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

	internal bool IsTopLevelElementNullable
	{
		get
		{
			return _helper.IsTopLevelElementNullable;
		}
		set
		{
			_helper.IsTopLevelElementNullable = value;
		}
	}

	internal CreateXmlSerializableDelegate CreateXmlSerializableDelegate
	{
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		get
		{
			if (DataContractSerializer.Option == SerializationOption.CodeGenOnly || DataContractSerializer.Option == SerializationOption.ReflectionAsBackup)
			{
				if (_helper.CreateXmlSerializableDelegate == null)
				{
					lock (this)
					{
						if (_helper.CreateXmlSerializableDelegate == null)
						{
							CreateXmlSerializableDelegate createXmlSerializableDelegate = GenerateCreateXmlSerializableDelegate();
							Interlocked.MemoryBarrier();
							_helper.CreateXmlSerializableDelegate = createXmlSerializableDelegate;
						}
					}
				}
				return _helper.CreateXmlSerializableDelegate;
			}
			return () => ReflectionCreateXmlSerializable(base.UnderlyingType);
		}
	}

	internal override bool CanContainReferences => false;

	public override bool IsBuiltInDataContract
	{
		get
		{
			if (!(base.UnderlyingType == Globals.TypeOfXmlElement))
			{
				return base.UnderlyingType == Globals.TypeOfXmlNodeArray;
			}
			return true;
		}
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal XmlDataContract(Type type)
		: base(new XmlDataContractCriticalHelper(type))
	{
		_helper = base.Helper as XmlDataContractCriticalHelper;
	}

	private ConstructorInfo GetConstructor()
	{
		if (base.UnderlyingType.IsValueType)
		{
			return null;
		}
		ConstructorInfo constructor = base.UnderlyingType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
		if (constructor == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(System.SR.Format(System.SR.IXmlSerializableMustHaveDefaultConstructor, DataContract.GetClrTypeFullName(base.UnderlyingType))));
		}
		return constructor;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal CreateXmlSerializableDelegate GenerateCreateXmlSerializableDelegate()
	{
		Type underlyingType = base.UnderlyingType;
		CodeGenerator codeGenerator = new CodeGenerator();
		bool flag = RequiresMemberAccessForCreate(null) && !(underlyingType.FullName == "System.Xml.Linq.XElement");
		try
		{
			codeGenerator.BeginMethod("Create" + DataContract.GetClrTypeFullName(underlyingType), typeof(CreateXmlSerializableDelegate), flag);
		}
		catch (SecurityException securityException)
		{
			if (!flag)
			{
				throw;
			}
			RequiresMemberAccessForCreate(securityException);
		}
		if (underlyingType.IsValueType)
		{
			LocalBuilder localBuilder = codeGenerator.DeclareLocal(underlyingType, underlyingType.Name + "Value");
			codeGenerator.Ldloca(localBuilder);
			codeGenerator.InitObj(underlyingType);
			codeGenerator.Ldloc(localBuilder);
		}
		else
		{
			ConstructorInfo constructorInfo = GetConstructor();
			if (!constructorInfo.IsPublic && underlyingType.FullName == "System.Xml.Linq.XElement")
			{
				Type type = underlyingType.Assembly.GetType("System.Xml.Linq.XName");
				if (type != null)
				{
					MethodInfo method = type.GetMethod("op_Implicit", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, new Type[1] { typeof(string) });
					ConstructorInfo constructor = underlyingType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new Type[1] { type });
					if (method != null && constructor != null)
					{
						codeGenerator.Ldstr("default");
						codeGenerator.Call(method);
						constructorInfo = constructor;
					}
				}
			}
			codeGenerator.New(constructorInfo);
		}
		codeGenerator.ConvertValue(base.UnderlyingType, Globals.TypeOfIXmlSerializable);
		codeGenerator.Ret();
		return (CreateXmlSerializableDelegate)codeGenerator.EndMethod();
	}

	private bool RequiresMemberAccessForCreate(SecurityException securityException)
	{
		if (!DataContract.IsTypeVisible(base.UnderlyingType))
		{
			if (securityException != null)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new SecurityException(System.SR.Format(System.SR.PartialTrustIXmlSerializableTypeNotPublic, DataContract.GetClrTypeFullName(base.UnderlyingType)), securityException));
			}
			return true;
		}
		if (DataContract.ConstructorRequiresMemberAccess(GetConstructor()))
		{
			if (securityException != null)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new SecurityException(System.SR.Format(System.SR.PartialTrustIXmlSerialzableNoPublicConstructor, DataContract.GetClrTypeFullName(base.UnderlyingType)), securityException));
			}
			return true;
		}
		return false;
	}

	internal IXmlSerializable ReflectionCreateXmlSerializable(Type type)
	{
		if (type.IsValueType)
		{
			throw new NotImplementedException("ReflectionCreateXmlSerializable - value type");
		}
		object obj = null;
		if (type == typeof(XElement))
		{
			obj = new XElement("default");
		}
		else
		{
			ConstructorInfo constructor = GetConstructor();
			obj = constructor.Invoke(Array.Empty<object>());
		}
		return (IXmlSerializable)obj;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public override void WriteXmlValue(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context)
	{
		if (context == null)
		{
			XmlObjectSerializerWriteContext.WriteRootIXmlSerializable(xmlWriter, obj);
		}
		else
		{
			context.WriteIXmlSerializable(xmlWriter, obj);
		}
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public override object ReadXmlValue(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context)
	{
		object obj;
		if (context == null)
		{
			obj = XmlObjectSerializerReadContext.ReadRootIXmlSerializable(xmlReader, this, isMemberType: true);
		}
		else
		{
			obj = context.ReadIXmlSerializable(xmlReader, this, isMemberType: true);
			context.AddNewObject(obj);
		}
		xmlReader.ReadEndElement();
		return obj;
	}
}
