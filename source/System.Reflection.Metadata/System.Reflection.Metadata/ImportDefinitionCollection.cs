using System.Collections;
using System.Collections.Generic;
using System.Reflection.Internal;
using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Metadata;

public readonly struct ImportDefinitionCollection : IEnumerable<ImportDefinition>, IEnumerable
{
	public struct Enumerator : IEnumerator<ImportDefinition>, IEnumerator, IDisposable
	{
		private BlobReader _reader;

		private ImportDefinition _current;

		public ImportDefinition Current => _current;

		object IEnumerator.Current => _current;

		internal Enumerator(MemoryBlock block)
		{
			_reader = new BlobReader(block);
			_current = default(ImportDefinition);
		}

		public bool MoveNext()
		{
			if (_reader.RemainingBytes == 0)
			{
				return false;
			}
			ImportDefinitionKind importDefinitionKind = (ImportDefinitionKind)_reader.ReadByte();
			switch (importDefinitionKind)
			{
			case ImportDefinitionKind.ImportType:
			{
				Handle typeOrNamespace = _reader.ReadTypeHandle();
				_current = new ImportDefinition(importDefinitionKind, default(BlobHandle), default(AssemblyReferenceHandle), typeOrNamespace);
				break;
			}
			case ImportDefinitionKind.ImportNamespace:
			{
				Handle typeOrNamespace = MetadataTokens.BlobHandle(_reader.ReadCompressedInteger());
				_current = new ImportDefinition(importDefinitionKind, default(BlobHandle), default(AssemblyReferenceHandle), typeOrNamespace);
				break;
			}
			case ImportDefinitionKind.ImportAssemblyNamespace:
			{
				AssemblyReferenceHandle assembly = MetadataTokens.AssemblyReferenceHandle(_reader.ReadCompressedInteger());
				Handle typeOrNamespace = MetadataTokens.BlobHandle(_reader.ReadCompressedInteger());
				_current = new ImportDefinition(importDefinitionKind, default(BlobHandle), assembly, typeOrNamespace);
				break;
			}
			case ImportDefinitionKind.ImportAssemblyReferenceAlias:
				_current = new ImportDefinition(importDefinitionKind, MetadataTokens.BlobHandle(_reader.ReadCompressedInteger()));
				break;
			case ImportDefinitionKind.AliasAssemblyReference:
				_current = new ImportDefinition(importDefinitionKind, MetadataTokens.BlobHandle(_reader.ReadCompressedInteger()), MetadataTokens.AssemblyReferenceHandle(_reader.ReadCompressedInteger()));
				break;
			case ImportDefinitionKind.AliasType:
			{
				BlobHandle alias2 = MetadataTokens.BlobHandle(_reader.ReadCompressedInteger());
				Handle typeOrNamespace = _reader.ReadTypeHandle();
				_current = new ImportDefinition(importDefinitionKind, alias2, default(AssemblyReferenceHandle), typeOrNamespace);
				break;
			}
			case ImportDefinitionKind.ImportXmlNamespace:
			case ImportDefinitionKind.AliasNamespace:
			{
				BlobHandle alias = MetadataTokens.BlobHandle(_reader.ReadCompressedInteger());
				Handle typeOrNamespace = MetadataTokens.BlobHandle(_reader.ReadCompressedInteger());
				_current = new ImportDefinition(importDefinitionKind, alias, default(AssemblyReferenceHandle), typeOrNamespace);
				break;
			}
			case ImportDefinitionKind.AliasAssemblyNamespace:
				_current = new ImportDefinition(importDefinitionKind, MetadataTokens.BlobHandle(_reader.ReadCompressedInteger()), MetadataTokens.AssemblyReferenceHandle(_reader.ReadCompressedInteger()), MetadataTokens.BlobHandle(_reader.ReadCompressedInteger()));
				break;
			default:
				throw new BadImageFormatException(System.SR.Format(System.SR.InvalidImportDefinitionKind, importDefinitionKind));
			}
			return true;
		}

		public void Reset()
		{
			_reader.Reset();
			_current = default(ImportDefinition);
		}

		void IDisposable.Dispose()
		{
		}
	}

	private readonly MemoryBlock _block;

	internal ImportDefinitionCollection(MemoryBlock block)
	{
		_block = block;
	}

	public Enumerator GetEnumerator()
	{
		return new Enumerator(_block);
	}

	IEnumerator<ImportDefinition> IEnumerable<ImportDefinition>.GetEnumerator()
	{
		return GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
