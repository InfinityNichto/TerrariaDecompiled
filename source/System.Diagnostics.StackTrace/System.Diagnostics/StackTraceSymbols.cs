using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;

namespace System.Diagnostics;

internal sealed class StackTraceSymbols : IDisposable
{
	private readonly ConditionalWeakTable<Assembly, MetadataReaderProvider> _metadataCache;

	public StackTraceSymbols()
	{
		_metadataCache = new ConditionalWeakTable<Assembly, MetadataReaderProvider>();
	}

	void IDisposable.Dispose()
	{
		foreach (KeyValuePair<Assembly, MetadataReaderProvider> item in (IEnumerable<KeyValuePair<Assembly, MetadataReaderProvider>>)_metadataCache)
		{
			item.Deconstruct(out var _, out var value);
			value?.Dispose();
		}
		_metadataCache.Clear();
	}

	internal void GetSourceLineInfo(Assembly assembly, string assemblyPath, IntPtr loadedPeAddress, int loadedPeSize, bool isFileLayout, IntPtr inMemoryPdbAddress, int inMemoryPdbSize, int methodToken, int ilOffset, out string sourceFile, out int sourceLine, out int sourceColumn)
	{
		sourceFile = null;
		sourceLine = 0;
		sourceColumn = 0;
		MetadataReader metadataReader = TryGetReader(assembly, assemblyPath, loadedPeAddress, loadedPeSize, isFileLayout, inMemoryPdbAddress, inMemoryPdbSize);
		if (metadataReader == null)
		{
			return;
		}
		Handle handle = MetadataTokens.Handle(methodToken);
		if (handle.Kind != HandleKind.MethodDefinition)
		{
			return;
		}
		MethodDebugInformationHandle handle2 = ((MethodDefinitionHandle)handle).ToDebugInformationHandle();
		MethodDebugInformation methodDebugInformation = metadataReader.GetMethodDebugInformation(handle2);
		if (methodDebugInformation.SequencePointsBlob.IsNil)
		{
			return;
		}
		SequencePointCollection sequencePoints = methodDebugInformation.GetSequencePoints();
		SequencePoint? sequencePoint = null;
		foreach (SequencePoint item in sequencePoints)
		{
			if (item.Offset > ilOffset)
			{
				break;
			}
			if (item.StartLine != 16707566)
			{
				sequencePoint = item;
			}
		}
		if (sequencePoint.HasValue)
		{
			sourceLine = sequencePoint.Value.StartLine;
			sourceColumn = sequencePoint.Value.StartColumn;
			sourceFile = metadataReader.GetString(metadataReader.GetDocument(sequencePoint.Value.Document).Name);
		}
	}

	private MetadataReader TryGetReader(Assembly assembly, string assemblyPath, IntPtr loadedPeAddress, int loadedPeSize, bool isFileLayout, IntPtr inMemoryPdbAddress, int inMemoryPdbSize)
	{
		if ((loadedPeAddress == IntPtr.Zero || assemblyPath == null) && inMemoryPdbAddress == IntPtr.Zero)
		{
			return null;
		}
		return _metadataCache.GetValue(assembly, (Assembly assembly) => (!(inMemoryPdbAddress != IntPtr.Zero)) ? TryOpenReaderFromAssemblyFile(assemblyPath, loadedPeAddress, loadedPeSize, isFileLayout) : TryOpenReaderForInMemoryPdb(inMemoryPdbAddress, inMemoryPdbSize))?.GetMetadataReader();
	}

	private unsafe static MetadataReaderProvider TryOpenReaderForInMemoryPdb(IntPtr inMemoryPdbAddress, int inMemoryPdbSize)
	{
		if (inMemoryPdbSize < 4 || *(uint*)(void*)inMemoryPdbAddress != 1112167234)
		{
			return null;
		}
		MetadataReaderProvider metadataReaderProvider = MetadataReaderProvider.FromMetadataImage((byte*)(void*)inMemoryPdbAddress, inMemoryPdbSize);
		try
		{
			metadataReaderProvider.GetMetadataReader();
			return metadataReaderProvider;
		}
		catch (BadImageFormatException)
		{
			metadataReaderProvider.Dispose();
			return null;
		}
	}

	private unsafe static PEReader TryGetPEReader(string assemblyPath, IntPtr loadedPeAddress, int loadedPeSize, bool isFileLayout)
	{
		if (loadedPeAddress != IntPtr.Zero && loadedPeSize > 0)
		{
			return new PEReader((byte*)(void*)loadedPeAddress, loadedPeSize, !isFileLayout);
		}
		Stream stream = TryOpenFile(assemblyPath);
		if (stream != null)
		{
			return new PEReader(stream);
		}
		return null;
	}

	private static MetadataReaderProvider TryOpenReaderFromAssemblyFile(string assemblyPath, IntPtr loadedPeAddress, int loadedPeSize, bool isFileLayout)
	{
		using (PEReader pEReader = TryGetPEReader(assemblyPath, loadedPeAddress, loadedPeSize, isFileLayout))
		{
			if (pEReader == null)
			{
				return null;
			}
			if (pEReader.TryOpenAssociatedPortablePdb(assemblyPath, TryOpenFile, out MetadataReaderProvider pdbReaderProvider, out string _))
			{
				return pdbReaderProvider;
			}
		}
		return null;
	}

	private static Stream TryOpenFile(string path)
	{
		if (!File.Exists(path))
		{
			return null;
		}
		try
		{
			return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
		}
		catch
		{
			return null;
		}
	}
}
