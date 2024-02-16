using System.Reflection;

namespace System.Runtime.Serialization.Formatters.Binary;

internal sealed class BinaryAssemblyInfo
{
	internal string _assemblyString;

	private Assembly _assembly;

	internal BinaryAssemblyInfo(string assemblyString)
	{
		_assemblyString = assemblyString;
	}

	internal BinaryAssemblyInfo(string assemblyString, Assembly assembly)
		: this(assemblyString)
	{
		_assembly = assembly;
	}

	internal Assembly GetAssembly()
	{
		if (_assembly == null)
		{
			_assembly = FormatterServices.LoadAssemblyFromStringNoThrow(_assemblyString);
			if (_assembly == null)
			{
				throw new SerializationException(System.SR.Format(System.SR.Serialization_AssemblyNotFound, _assemblyString));
			}
		}
		return _assembly;
	}
}
