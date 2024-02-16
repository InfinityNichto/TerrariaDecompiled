using System.Linq;

namespace System.Reflection.Emit;

internal static class IgnoreAccessChecksToAttributeBuilder
{
	public static ConstructorInfo AddToModule(ModuleBuilder mb)
	{
		TypeBuilder typeBuilder = mb.DefineType("System.Runtime.CompilerServices.IgnoresAccessChecksToAttribute", TypeAttributes.Public, typeof(Attribute));
		FieldBuilder fieldBuilder = typeBuilder.DefineField("assemblyName", typeof(string), FieldAttributes.Private);
		ConstructorBuilder constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, new Type[1] { fieldBuilder.FieldType });
		ILGenerator iLGenerator = constructorBuilder.GetILGenerator();
		iLGenerator.Emit(OpCodes.Ldarg_0);
		iLGenerator.Emit(OpCodes.Ldarg, 1);
		iLGenerator.Emit(OpCodes.Stfld, fieldBuilder);
		iLGenerator.Emit(OpCodes.Ret);
		PropertyBuilder propertyBuilder = typeBuilder.DefineProperty("AssemblyName", PropertyAttributes.None, CallingConventions.HasThis, typeof(string), null);
		MethodBuilder methodBuilder = typeBuilder.DefineMethod("get_AssemblyName", MethodAttributes.Public, CallingConventions.HasThis, typeof(string), null);
		propertyBuilder.SetGetMethod(methodBuilder);
		iLGenerator = methodBuilder.GetILGenerator();
		iLGenerator.Emit(OpCodes.Ldarg_0);
		iLGenerator.Emit(OpCodes.Ldfld, fieldBuilder);
		iLGenerator.Emit(OpCodes.Ret);
		TypeInfo typeInfo = typeof(AttributeUsageAttribute).GetTypeInfo();
		ConstructorInfo con = typeInfo.DeclaredConstructors.Single((ConstructorInfo c) => c.GetParameters().Length == 1 && c.GetParameters()[0].ParameterType == typeof(AttributeTargets));
		PropertyInfo propertyInfo = typeInfo.DeclaredProperties.Single((PropertyInfo f) => string.Equals(f.Name, "AllowMultiple"));
		CustomAttributeBuilder customAttribute = new CustomAttributeBuilder(con, new object[1] { AttributeTargets.Assembly }, new PropertyInfo[1] { propertyInfo }, new object[1] { true });
		typeBuilder.SetCustomAttribute(customAttribute);
		return typeBuilder.CreateTypeInfo().DeclaredConstructors.Single();
	}
}
