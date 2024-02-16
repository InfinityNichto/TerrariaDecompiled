using System.Reflection.Emit;

namespace System.Linq.Expressions.Compiler;

internal interface ILocalCache
{
	LocalBuilder GetLocal(Type type);

	void FreeLocal(LocalBuilder local);
}
