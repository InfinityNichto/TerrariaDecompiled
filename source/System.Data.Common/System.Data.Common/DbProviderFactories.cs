using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace System.Data.Common;

public static class DbProviderFactories
{
	private struct ProviderRegistration
	{
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
		internal string FactoryTypeAssemblyQualifiedName { get; }

		internal DbProviderFactory FactoryInstance { get; }

		internal ProviderRegistration([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] string factoryTypeAssemblyQualifiedName, DbProviderFactory factoryInstance)
		{
			FactoryTypeAssemblyQualifiedName = factoryTypeAssemblyQualifiedName;
			FactoryInstance = factoryInstance;
		}
	}

	private static readonly ConcurrentDictionary<string, ProviderRegistration> _registeredFactories = new ConcurrentDictionary<string, ProviderRegistration>();

	public static bool TryGetFactory(string providerInvariantName, [NotNullWhen(true)] out DbProviderFactory? factory)
	{
		factory = GetFactory(providerInvariantName, throwOnError: false);
		return factory != null;
	}

	public static DbProviderFactory GetFactory(string providerInvariantName)
	{
		return GetFactory(providerInvariantName, throwOnError: true);
	}

	[RequiresUnreferencedCode("Provider type and its members might be trimmed if not referenced directly.")]
	public static DbProviderFactory GetFactory(DataRow providerRow)
	{
		ADP.CheckArgumentNull(providerRow, "providerRow");
		DataColumn dataColumn = providerRow.Table.Columns["AssemblyQualifiedName"];
		if (dataColumn == null)
		{
			throw ADP.Argument(System.SR.ADP_DbProviderFactories_NoAssemblyQualifiedName);
		}
		string text = providerRow[dataColumn] as string;
		if (string.IsNullOrWhiteSpace(text))
		{
			throw ADP.Argument(System.SR.ADP_DbProviderFactories_NoAssemblyQualifiedName);
		}
		return GetFactoryInstance(GetProviderTypeFromTypeName(text));
	}

	public static DbProviderFactory? GetFactory(DbConnection connection)
	{
		ADP.CheckArgumentNull(connection, "connection");
		return connection.ProviderFactory;
	}

	public static DataTable GetFactoryClasses()
	{
		DataColumn dataColumn = new DataColumn("Name", typeof(string))
		{
			ReadOnly = true
		};
		DataColumn dataColumn2 = new DataColumn("Description", typeof(string))
		{
			ReadOnly = true
		};
		DataColumn dataColumn3 = new DataColumn("InvariantName", typeof(string))
		{
			ReadOnly = true
		};
		DataColumn dataColumn4 = new DataColumn("AssemblyQualifiedName", typeof(string))
		{
			ReadOnly = true
		};
		DataTable dataTable = new DataTable("DbProviderFactories")
		{
			Locale = CultureInfo.InvariantCulture
		};
		dataTable.Columns.AddRange(new DataColumn[4] { dataColumn, dataColumn2, dataColumn3, dataColumn4 });
		dataTable.PrimaryKey = new DataColumn[1] { dataColumn3 };
		foreach (KeyValuePair<string, ProviderRegistration> registeredFactory in _registeredFactories)
		{
			DataRow dataRow = dataTable.NewRow();
			dataRow["InvariantName"] = registeredFactory.Key;
			dataRow["AssemblyQualifiedName"] = registeredFactory.Value.FactoryTypeAssemblyQualifiedName;
			dataRow["Name"] = string.Empty;
			dataRow["Description"] = string.Empty;
			dataTable.AddRow(dataRow);
		}
		return dataTable;
	}

	public static IEnumerable<string> GetProviderInvariantNames()
	{
		return _registeredFactories.Keys.ToList();
	}

	public static void RegisterFactory(string providerInvariantName, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] string factoryTypeAssemblyQualifiedName)
	{
		ADP.CheckArgumentLength(providerInvariantName, "providerInvariantName");
		ADP.CheckArgumentLength(factoryTypeAssemblyQualifiedName, "factoryTypeAssemblyQualifiedName");
		_registeredFactories[providerInvariantName] = new ProviderRegistration(factoryTypeAssemblyQualifiedName, null);
	}

	public static void RegisterFactory(string providerInvariantName, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] Type providerFactoryClass)
	{
		RegisterFactory(providerInvariantName, GetFactoryInstance(providerFactoryClass));
	}

	public static void RegisterFactory(string providerInvariantName, DbProviderFactory factory)
	{
		ADP.CheckArgumentLength(providerInvariantName, "providerInvariantName");
		ADP.CheckArgumentNull(factory, "factory");
		string assemblyQualifiedName = factory.GetType().AssemblyQualifiedName;
		_registeredFactories[providerInvariantName] = new ProviderRegistration(assemblyQualifiedName, factory);
	}

	public static bool UnregisterFactory(string providerInvariantName)
	{
		ProviderRegistration value;
		if (!string.IsNullOrWhiteSpace(providerInvariantName))
		{
			return _registeredFactories.TryRemove(providerInvariantName, out value);
		}
		return false;
	}

	private static DbProviderFactory GetFactory(string providerInvariantName, bool throwOnError)
	{
		if (throwOnError)
		{
			ADP.CheckArgumentLength(providerInvariantName, "providerInvariantName");
		}
		else if (string.IsNullOrWhiteSpace(providerInvariantName))
		{
			return null;
		}
		if (!_registeredFactories.TryGetValue(providerInvariantName, out var value))
		{
			if (!throwOnError)
			{
				return null;
			}
			throw ADP.Argument(System.SR.Format(System.SR.ADP_DbProviderFactories_InvariantNameNotFound, providerInvariantName));
		}
		DbProviderFactory factoryInstance = value.FactoryInstance;
		if (factoryInstance == null)
		{
			factoryInstance = GetFactoryInstance(GetProviderTypeFromTypeName(value.FactoryTypeAssemblyQualifiedName));
			RegisterFactory(providerInvariantName, factoryInstance);
		}
		return factoryInstance;
	}

	private static DbProviderFactory GetFactoryInstance([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] Type providerFactoryClass)
	{
		ADP.CheckArgumentNull(providerFactoryClass, "providerFactoryClass");
		if (!providerFactoryClass.IsSubclassOf(typeof(DbProviderFactory)))
		{
			throw ADP.Argument(System.SR.Format(System.SR.ADP_DbProviderFactories_NotAFactoryType, providerFactoryClass.FullName));
		}
		FieldInfo field = providerFactoryClass.GetField("Instance", BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public);
		if (null == field)
		{
			throw ADP.InvalidOperation(System.SR.ADP_DbProviderFactories_NoInstance);
		}
		if (!field.FieldType.IsSubclassOf(typeof(DbProviderFactory)))
		{
			throw ADP.InvalidOperation(System.SR.ADP_DbProviderFactories_NoInstance);
		}
		object value = field.GetValue(null);
		if (value == null)
		{
			throw ADP.InvalidOperation(System.SR.ADP_DbProviderFactories_NoInstance);
		}
		return (DbProviderFactory)value;
	}

	[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
	private static Type GetProviderTypeFromTypeName([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] string assemblyQualifiedName)
	{
		Type type = Type.GetType(assemblyQualifiedName);
		if (null == type)
		{
			throw ADP.Argument(System.SR.Format(System.SR.ADP_DbProviderFactories_FactoryNotLoadable, assemblyQualifiedName));
		}
		return type;
	}
}
