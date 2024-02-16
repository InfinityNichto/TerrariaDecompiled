using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;

namespace System.Runtime.Serialization;

public class XsdDataContractExporter
{
	private ExportOptions _options;

	private XmlSchemaSet _schemas;

	private DataContractSet _dataContractSet;

	public ExportOptions? Options
	{
		get
		{
			return _options;
		}
		set
		{
			_options = value;
		}
	}

	public XmlSchemaSet Schemas
	{
		get
		{
			throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_SchemaImporter);
		}
	}

	private DataContractSet DataContractSet
	{
		get
		{
			throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_IDataContractSurrogate);
		}
	}

	public XsdDataContractExporter()
	{
	}

	public XsdDataContractExporter(XmlSchemaSet? schemas)
	{
		_schemas = schemas;
	}

	private XmlSchemaSet GetSchemaSet()
	{
		if (_schemas == null)
		{
			_schemas = new XmlSchemaSet();
			_schemas.XmlResolver = null;
		}
		return _schemas;
	}

	private void TraceExportBegin()
	{
	}

	private void TraceExportEnd()
	{
	}

	private void TraceExportError(Exception exception)
	{
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public void Export(ICollection<Assembly> assemblies)
	{
		if (assemblies == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("assemblies"));
		}
		TraceExportBegin();
		DataContractSet dataContractSet = ((_dataContractSet == null) ? null : new DataContractSet(_dataContractSet));
		try
		{
			foreach (Assembly assembly in assemblies)
			{
				if (assembly == null)
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(System.SR.Format(System.SR.CannotExportNullAssembly, "assemblies")));
				}
				Type[] types = assembly.GetTypes();
				for (int i = 0; i < types.Length; i++)
				{
					CheckAndAddType(types[i]);
				}
			}
			Export();
		}
		catch (Exception exception)
		{
			_dataContractSet = dataContractSet;
			TraceExportError(exception);
			throw;
		}
		TraceExportEnd();
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public void Export(ICollection<Type> types)
	{
		if (types == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("types"));
		}
		TraceExportBegin();
		DataContractSet dataContractSet = ((_dataContractSet == null) ? null : new DataContractSet(_dataContractSet));
		try
		{
			foreach (Type type in types)
			{
				if (type == null)
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(System.SR.Format(System.SR.CannotExportNullType, "types")));
				}
				AddType(type);
			}
			Export();
		}
		catch (Exception exception)
		{
			_dataContractSet = dataContractSet;
			TraceExportError(exception);
			throw;
		}
		TraceExportEnd();
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public void Export(Type type)
	{
		if (type == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("type"));
		}
		TraceExportBegin();
		DataContractSet dataContractSet = ((_dataContractSet == null) ? null : new DataContractSet(_dataContractSet));
		try
		{
			AddType(type);
			Export();
		}
		catch (Exception exception)
		{
			_dataContractSet = dataContractSet;
			TraceExportError(exception);
			throw;
		}
		TraceExportEnd();
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public XmlQualifiedName GetSchemaTypeName(Type type)
	{
		if (type == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("type"));
		}
		type = GetSurrogatedType(type);
		DataContract dataContract = DataContract.GetDataContract(type);
		System.Runtime.Serialization.DataContractSet.EnsureTypeNotGeneric(dataContract.UnderlyingType);
		if (dataContract is XmlDataContract { IsAnonymous: not false })
		{
			return XmlQualifiedName.Empty;
		}
		return dataContract.StableName;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public XmlSchemaType? GetSchemaType(Type type)
	{
		if (type == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("type"));
		}
		type = GetSurrogatedType(type);
		DataContract dataContract = DataContract.GetDataContract(type);
		System.Runtime.Serialization.DataContractSet.EnsureTypeNotGeneric(dataContract.UnderlyingType);
		if (dataContract is XmlDataContract { IsAnonymous: not false } xmlDataContract)
		{
			return xmlDataContract.XsdType;
		}
		return null;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public XmlQualifiedName? GetRootElementName(Type type)
	{
		if (type == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("type"));
		}
		type = GetSurrogatedType(type);
		DataContract dataContract = DataContract.GetDataContract(type);
		System.Runtime.Serialization.DataContractSet.EnsureTypeNotGeneric(dataContract.UnderlyingType);
		if (dataContract.HasRoot)
		{
			return new XmlQualifiedName(dataContract.TopLevelElementName.Value, dataContract.TopLevelElementNamespace.Value);
		}
		return null;
	}

	private Type GetSurrogatedType(Type type)
	{
		return type;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private void CheckAndAddType(Type type)
	{
		type = GetSurrogatedType(type);
		if (!type.ContainsGenericParameters && DataContract.IsTypeSerializable(type))
		{
			AddType(type);
		}
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private void AddType(Type type)
	{
		DataContractSet.Add(type);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private void Export()
	{
		AddKnownTypes();
		SchemaExporter schemaExporter = new SchemaExporter(GetSchemaSet(), DataContractSet);
		schemaExporter.Export();
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private void AddKnownTypes()
	{
		if (Options == null)
		{
			return;
		}
		Collection<Type> knownTypes = Options.KnownTypes;
		if (knownTypes == null)
		{
			return;
		}
		for (int i = 0; i < knownTypes.Count; i++)
		{
			Type type = knownTypes[i];
			if (type == null)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(System.SR.CannotExportNullKnownType));
			}
			AddType(type);
		}
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public bool CanExport(ICollection<Assembly> assemblies)
	{
		if (assemblies == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("assemblies"));
		}
		DataContractSet dataContractSet = ((_dataContractSet == null) ? null : new DataContractSet(_dataContractSet));
		try
		{
			foreach (Assembly assembly in assemblies)
			{
				if (assembly == null)
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(System.SR.Format(System.SR.CannotExportNullAssembly, "assemblies")));
				}
				Type[] types = assembly.GetTypes();
				for (int i = 0; i < types.Length; i++)
				{
					CheckAndAddType(types[i]);
				}
			}
			AddKnownTypes();
			return true;
		}
		catch (InvalidDataContractException)
		{
			_dataContractSet = dataContractSet;
			return false;
		}
		catch (Exception exception)
		{
			_dataContractSet = dataContractSet;
			TraceExportError(exception);
			throw;
		}
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public bool CanExport(ICollection<Type> types)
	{
		if (types == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("types"));
		}
		DataContractSet dataContractSet = ((_dataContractSet == null) ? null : new DataContractSet(_dataContractSet));
		try
		{
			foreach (Type type in types)
			{
				if (type == null)
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(System.SR.Format(System.SR.CannotExportNullType, "types")));
				}
				AddType(type);
			}
			AddKnownTypes();
			return true;
		}
		catch (InvalidDataContractException)
		{
			_dataContractSet = dataContractSet;
			return false;
		}
		catch (Exception exception)
		{
			_dataContractSet = dataContractSet;
			TraceExportError(exception);
			throw;
		}
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public bool CanExport(Type type)
	{
		if (type == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("type"));
		}
		DataContractSet dataContractSet = ((_dataContractSet == null) ? null : new DataContractSet(_dataContractSet));
		try
		{
			AddType(type);
			AddKnownTypes();
			return true;
		}
		catch (InvalidDataContractException)
		{
			_dataContractSet = dataContractSet;
			return false;
		}
		catch (Exception exception)
		{
			_dataContractSet = dataContractSet;
			TraceExportError(exception);
			throw;
		}
	}
}
