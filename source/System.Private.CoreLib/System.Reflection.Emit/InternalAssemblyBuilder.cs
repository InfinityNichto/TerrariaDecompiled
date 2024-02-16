using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace System.Reflection.Emit;

internal sealed class InternalAssemblyBuilder : RuntimeAssembly
{
	public override string Location
	{
		get
		{
			throw new NotSupportedException(SR.NotSupported_DynamicAssembly);
		}
	}

	[RequiresAssemblyFiles("This member throws an exception for assemblies embedded in a single-file app")]
	public override string CodeBase
	{
		get
		{
			throw new NotSupportedException(SR.NotSupported_DynamicAssembly);
		}
	}

	public override string ImageRuntimeVersion => Assembly.GetExecutingAssembly().ImageRuntimeVersion;

	private InternalAssemblyBuilder()
	{
	}

	public override bool Equals(object obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (obj is InternalAssemblyBuilder)
		{
			return this == obj;
		}
		return obj.Equals(this);
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	public override string[] GetManifestResourceNames()
	{
		throw new NotSupportedException(SR.NotSupported_DynamicAssembly);
	}

	[RequiresAssemblyFiles("This member throws an exception for assemblies embedded in a single-file app")]
	public override FileStream GetFile(string name)
	{
		throw new NotSupportedException(SR.NotSupported_DynamicAssembly);
	}

	[RequiresAssemblyFiles("This member throws an exception for assemblies embedded in a single-file app")]
	public override FileStream[] GetFiles(bool getResourceModules)
	{
		throw new NotSupportedException(SR.NotSupported_DynamicAssembly);
	}

	public override Stream GetManifestResourceStream(Type type, string name)
	{
		throw new NotSupportedException(SR.NotSupported_DynamicAssembly);
	}

	public override Stream GetManifestResourceStream(string name)
	{
		throw new NotSupportedException(SR.NotSupported_DynamicAssembly);
	}

	public override ManifestResourceInfo GetManifestResourceInfo(string resourceName)
	{
		throw new NotSupportedException(SR.NotSupported_DynamicAssembly);
	}

	[RequiresUnreferencedCode("Types might be removed")]
	public override Type[] GetExportedTypes()
	{
		throw new NotSupportedException(SR.NotSupported_DynamicAssembly);
	}
}
