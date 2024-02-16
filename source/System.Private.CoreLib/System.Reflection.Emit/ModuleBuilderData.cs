namespace System.Reflection.Emit;

internal sealed class ModuleBuilderData
{
	public readonly TypeBuilder _globalTypeBuilder;

	public readonly string _moduleName;

	public bool _hasGlobalBeenCreated;

	internal ModuleBuilderData(ModuleBuilder module, string moduleName)
	{
		_globalTypeBuilder = new TypeBuilder(module);
		_moduleName = moduleName;
	}
}
