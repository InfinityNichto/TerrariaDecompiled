using System.Diagnostics.CodeAnalysis;
using System.Dynamic.Utils;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;

namespace System.Linq.Expressions.Compiler;

internal sealed class AssemblyGen
{
	private static AssemblyGen s_assembly;

	private readonly ModuleBuilder _myModule;

	private int _index;

	private static AssemblyGen Assembly
	{
		get
		{
			if (s_assembly == null)
			{
				Interlocked.CompareExchange(ref s_assembly, new AssemblyGen(), null);
			}
			return s_assembly;
		}
	}

	private AssemblyGen()
	{
		AssemblyName assemblyName = new AssemblyName("Snippets");
		AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
		_myModule = assemblyBuilder.DefineDynamicModule(assemblyName.Name);
	}

	private TypeBuilder DefineType(string name, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type parent, TypeAttributes attr)
	{
		ContractUtils.RequiresNotNull(name, "name");
		ContractUtils.RequiresNotNull(parent, "parent");
		StringBuilder stringBuilder = new StringBuilder(name);
		int value = Interlocked.Increment(ref _index);
		stringBuilder.Append('$');
		stringBuilder.Append(value);
		stringBuilder.Replace('+', '_').Replace('[', '_').Replace(']', '_')
			.Replace('*', '_')
			.Replace('&', '_')
			.Replace(',', '_')
			.Replace('\\', '_');
		name = stringBuilder.ToString();
		return _myModule.DefineType(name, attr, parent);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "MulticastDelegate has a ctor with RequiresUnreferencedCode, but the generated derived type doesn't reference this ctor, so this is trim compatible.")]
	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2111:ReflectionToDynamicallyAccessedMembers", Justification = "MulticastDelegate and Delegate have multiple methods with DynamicallyAccessedMembers annotations. But the generated codein this case will not call any of them (it only defines a .ctor and Invoke method both of which are runtime implemented.")]
	internal static TypeBuilder DefineDelegateType(string name)
	{
		return Assembly.DefineType(name, typeof(MulticastDelegate), TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.AutoClass);
	}
}
