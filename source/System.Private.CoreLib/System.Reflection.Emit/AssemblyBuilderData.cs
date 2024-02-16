using System.Collections.Generic;

namespace System.Reflection.Emit;

internal sealed class AssemblyBuilderData
{
	public readonly List<ModuleBuilder> _moduleBuilderList;

	public readonly AssemblyBuilderAccess _access;

	internal AssemblyBuilderData(AssemblyBuilderAccess access)
	{
		_access = access;
		_moduleBuilderList = new List<ModuleBuilder>();
	}

	public void CheckTypeNameConflict(string strTypeName, TypeBuilder enclosingType)
	{
		for (int i = 0; i < _moduleBuilderList.Count; i++)
		{
			ModuleBuilder moduleBuilder = _moduleBuilderList[i];
			moduleBuilder.CheckTypeNameConflict(strTypeName, enclosingType);
		}
	}
}
