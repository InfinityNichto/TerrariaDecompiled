namespace System.Reflection;

public class AssemblyNameProxy : MarshalByRefObject
{
	public AssemblyName GetAssemblyName(string assemblyFile)
	{
		return AssemblyName.GetAssemblyName(assemblyFile);
	}
}
