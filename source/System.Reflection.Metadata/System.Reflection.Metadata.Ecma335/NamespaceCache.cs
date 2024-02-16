using System.Collections.Generic;
using System.Collections.Immutable;

namespace System.Reflection.Metadata.Ecma335;

internal sealed class NamespaceCache
{
	private sealed class NamespaceDataBuilder
	{
		public readonly NamespaceDefinitionHandle Handle;

		public readonly StringHandle Name;

		public readonly string FullName;

		public NamespaceDefinitionHandle Parent;

		public ImmutableArray<NamespaceDefinitionHandle>.Builder Namespaces;

		public ImmutableArray<TypeDefinitionHandle>.Builder TypeDefinitions;

		public ImmutableArray<ExportedTypeHandle>.Builder ExportedTypes;

		private NamespaceData _frozen;

		public NamespaceDataBuilder(NamespaceDefinitionHandle handle, StringHandle name, string fullName)
		{
			Handle = handle;
			Name = name;
			FullName = fullName;
			Namespaces = ImmutableArray.CreateBuilder<NamespaceDefinitionHandle>();
			TypeDefinitions = ImmutableArray.CreateBuilder<TypeDefinitionHandle>();
			ExportedTypes = ImmutableArray.CreateBuilder<ExportedTypeHandle>();
		}

		public NamespaceData Freeze()
		{
			if (_frozen == null)
			{
				ImmutableArray<NamespaceDefinitionHandle> namespaceDefinitions = Namespaces.ToImmutable();
				Namespaces = null;
				ImmutableArray<TypeDefinitionHandle> typeDefinitions = TypeDefinitions.ToImmutable();
				TypeDefinitions = null;
				ImmutableArray<ExportedTypeHandle> exportedTypes = ExportedTypes.ToImmutable();
				ExportedTypes = null;
				_frozen = new NamespaceData(Name, FullName, Parent, namespaceDefinitions, typeDefinitions, exportedTypes);
			}
			return _frozen;
		}

		public void MergeInto(NamespaceDataBuilder other)
		{
			Parent = default(NamespaceDefinitionHandle);
			other.Namespaces.AddRange(Namespaces);
			other.TypeDefinitions.AddRange(TypeDefinitions);
			other.ExportedTypes.AddRange(ExportedTypes);
		}
	}

	private readonly MetadataReader _metadataReader;

	private readonly object _namespaceTableAndListLock = new object();

	private volatile Dictionary<NamespaceDefinitionHandle, NamespaceData> _namespaceTable;

	private NamespaceData _rootNamespace;

	private uint _virtualNamespaceCounter;

	internal bool CacheIsRealized => _namespaceTable != null;

	internal NamespaceCache(MetadataReader reader)
	{
		_metadataReader = reader;
	}

	internal string GetFullName(NamespaceDefinitionHandle handle)
	{
		NamespaceData namespaceData = GetNamespaceData(handle);
		return namespaceData.FullName;
	}

	internal NamespaceData GetRootNamespace()
	{
		EnsureNamespaceTableIsPopulated();
		return _rootNamespace;
	}

	internal NamespaceData GetNamespaceData(NamespaceDefinitionHandle handle)
	{
		EnsureNamespaceTableIsPopulated();
		if (!_namespaceTable.TryGetValue(handle, out var value))
		{
			Throw.InvalidHandle();
		}
		return value;
	}

	private StringHandle GetSimpleName(NamespaceDefinitionHandle fullNamespaceHandle, int segmentIndex = int.MaxValue)
	{
		StringHandle fullName = fullNamespaceHandle.GetFullName();
		int num = fullNamespaceHandle.GetHeapOffset() - 1;
		for (int i = 0; i < segmentIndex; i++)
		{
			int num2 = _metadataReader.StringHeap.IndexOfRaw(num + 1, '.');
			if (num2 == -1)
			{
				break;
			}
			num = num2;
		}
		int heapOffset = num + 1;
		return StringHandle.FromOffset(heapOffset).WithDotTermination();
	}

	private void PopulateNamespaceTable()
	{
		lock (_namespaceTableAndListLock)
		{
			if (_namespaceTable != null)
			{
				return;
			}
			Dictionary<NamespaceDefinitionHandle, NamespaceDataBuilder> dictionary = new Dictionary<NamespaceDefinitionHandle, NamespaceDataBuilder>();
			NamespaceDefinitionHandle namespaceDefinitionHandle = NamespaceDefinitionHandle.FromFullNameOffset(0);
			dictionary.Add(namespaceDefinitionHandle, new NamespaceDataBuilder(namespaceDefinitionHandle, namespaceDefinitionHandle.GetFullName(), string.Empty));
			PopulateTableWithTypeDefinitions(dictionary);
			PopulateTableWithExportedTypes(dictionary);
			MergeDuplicateNamespaces(dictionary, out var stringTable);
			ResolveParentChildRelationships(stringTable, out var virtualNamespaces);
			Dictionary<NamespaceDefinitionHandle, NamespaceData> dictionary2 = new Dictionary<NamespaceDefinitionHandle, NamespaceData>();
			foreach (KeyValuePair<NamespaceDefinitionHandle, NamespaceDataBuilder> item in dictionary)
			{
				dictionary2.Add(item.Key, item.Value.Freeze());
			}
			if (virtualNamespaces != null)
			{
				foreach (NamespaceDataBuilder item2 in virtualNamespaces)
				{
					dictionary2.Add(item2.Handle, item2.Freeze());
				}
			}
			_rootNamespace = dictionary2[namespaceDefinitionHandle];
			_namespaceTable = dictionary2;
		}
	}

	private void MergeDuplicateNamespaces(Dictionary<NamespaceDefinitionHandle, NamespaceDataBuilder> table, out Dictionary<string, NamespaceDataBuilder> stringTable)
	{
		Dictionary<string, NamespaceDataBuilder> dictionary = new Dictionary<string, NamespaceDataBuilder>();
		List<KeyValuePair<NamespaceDefinitionHandle, NamespaceDataBuilder>> list = null;
		foreach (KeyValuePair<NamespaceDefinitionHandle, NamespaceDataBuilder> item in table)
		{
			NamespaceDataBuilder value = item.Value;
			if (dictionary.TryGetValue(value.FullName, out var value2))
			{
				value.MergeInto(value2);
				if (list == null)
				{
					list = new List<KeyValuePair<NamespaceDefinitionHandle, NamespaceDataBuilder>>();
				}
				list.Add(new KeyValuePair<NamespaceDefinitionHandle, NamespaceDataBuilder>(item.Key, value2));
			}
			else
			{
				dictionary.Add(value.FullName, value);
			}
		}
		if (list != null)
		{
			foreach (KeyValuePair<NamespaceDefinitionHandle, NamespaceDataBuilder> item2 in list)
			{
				table[item2.Key] = item2.Value;
			}
		}
		stringTable = dictionary;
	}

	private NamespaceDataBuilder SynthesizeNamespaceData(string fullName, NamespaceDefinitionHandle realChild)
	{
		int num = 0;
		foreach (char c in fullName)
		{
			if (c == '.')
			{
				num++;
			}
		}
		StringHandle simpleName = GetSimpleName(realChild, num);
		NamespaceDefinitionHandle handle = NamespaceDefinitionHandle.FromVirtualIndex(++_virtualNamespaceCounter);
		return new NamespaceDataBuilder(handle, simpleName, fullName);
	}

	private void LinkChildDataToParentData(NamespaceDataBuilder child, NamespaceDataBuilder parent)
	{
		child.Parent = parent.Handle;
		parent.Namespaces.Add(child.Handle);
	}

	private void LinkChildToParentNamespace(Dictionary<string, NamespaceDataBuilder> existingNamespaces, NamespaceDataBuilder realChild, ref List<NamespaceDataBuilder> virtualNamespaces)
	{
		string fullName = realChild.FullName;
		NamespaceDataBuilder child = realChild;
		NamespaceDataBuilder value;
		while (true)
		{
			int num = fullName.LastIndexOf('.');
			string text;
			if (num == -1)
			{
				if (fullName.Length == 0)
				{
					return;
				}
				text = string.Empty;
			}
			else
			{
				text = fullName.Substring(0, num);
			}
			if (existingNamespaces.TryGetValue(text, out value))
			{
				break;
			}
			if (virtualNamespaces != null)
			{
				foreach (NamespaceDataBuilder virtualNamespace in virtualNamespaces)
				{
					if (virtualNamespace.FullName == text)
					{
						LinkChildDataToParentData(child, virtualNamespace);
						return;
					}
				}
			}
			else
			{
				virtualNamespaces = new List<NamespaceDataBuilder>();
			}
			NamespaceDataBuilder namespaceDataBuilder = SynthesizeNamespaceData(text, realChild.Handle);
			LinkChildDataToParentData(child, namespaceDataBuilder);
			virtualNamespaces.Add(namespaceDataBuilder);
			fullName = namespaceDataBuilder.FullName;
			child = namespaceDataBuilder;
		}
		LinkChildDataToParentData(child, value);
	}

	private void ResolveParentChildRelationships(Dictionary<string, NamespaceDataBuilder> namespaces, out List<NamespaceDataBuilder> virtualNamespaces)
	{
		virtualNamespaces = null;
		foreach (KeyValuePair<string, NamespaceDataBuilder> @namespace in namespaces)
		{
			LinkChildToParentNamespace(namespaces, @namespace.Value, ref virtualNamespaces);
		}
	}

	private void PopulateTableWithTypeDefinitions(Dictionary<NamespaceDefinitionHandle, NamespaceDataBuilder> table)
	{
		foreach (TypeDefinitionHandle typeDefinition in _metadataReader.TypeDefinitions)
		{
			if (!_metadataReader.GetTypeDefinition(typeDefinition).Attributes.IsNested())
			{
				NamespaceDefinitionHandle namespaceDefinition = _metadataReader.TypeDefTable.GetNamespaceDefinition(typeDefinition);
				if (table.TryGetValue(namespaceDefinition, out var value))
				{
					value.TypeDefinitions.Add(typeDefinition);
					continue;
				}
				StringHandle simpleName = GetSimpleName(namespaceDefinition);
				string @string = _metadataReader.GetString(namespaceDefinition);
				NamespaceDataBuilder namespaceDataBuilder = new NamespaceDataBuilder(namespaceDefinition, simpleName, @string);
				namespaceDataBuilder.TypeDefinitions.Add(typeDefinition);
				table.Add(namespaceDefinition, namespaceDataBuilder);
			}
		}
	}

	private void PopulateTableWithExportedTypes(Dictionary<NamespaceDefinitionHandle, NamespaceDataBuilder> table)
	{
		foreach (ExportedTypeHandle exportedType2 in _metadataReader.ExportedTypes)
		{
			ExportedType exportedType = _metadataReader.GetExportedType(exportedType2);
			if (exportedType.Implementation.Kind != HandleKind.ExportedType)
			{
				NamespaceDefinitionHandle namespaceDefinition = exportedType.NamespaceDefinition;
				if (table.TryGetValue(namespaceDefinition, out var value))
				{
					value.ExportedTypes.Add(exportedType2);
					continue;
				}
				StringHandle simpleName = GetSimpleName(namespaceDefinition);
				string @string = _metadataReader.GetString(namespaceDefinition);
				NamespaceDataBuilder namespaceDataBuilder = new NamespaceDataBuilder(namespaceDefinition, simpleName, @string);
				namespaceDataBuilder.ExportedTypes.Add(exportedType2);
				table.Add(namespaceDefinition, namespaceDataBuilder);
			}
		}
	}

	private void EnsureNamespaceTableIsPopulated()
	{
		if (_namespaceTable == null)
		{
			PopulateNamespaceTable();
		}
	}
}
