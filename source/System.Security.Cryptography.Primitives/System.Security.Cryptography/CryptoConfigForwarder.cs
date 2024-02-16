using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.Security.Cryptography;

internal static class CryptoConfigForwarder
{
	private static readonly Func<string, object> s_createFromName = BindCreateFromName();

	[RequiresUnreferencedCode("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
	private static Func<string, object> BindCreateFromName()
	{
		Type type = Type.GetType("System.Security.Cryptography.CryptoConfig, System.Security.Cryptography.Algorithms", throwOnError: true);
		MethodInfo method = type.GetMethod("CreateFromName", new Type[1] { typeof(string) });
		if (method == null)
		{
			throw new MissingMethodException(type.FullName, "CreateFromName");
		}
		return method.CreateDelegate<Func<string, object>>();
	}

	[RequiresUnreferencedCode("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
	internal static object CreateFromName(string name)
	{
		return s_createFromName(name);
	}

	internal static HashAlgorithm CreateDefaultHashAlgorithm()
	{
		throw new PlatformNotSupportedException(System.SR.Cryptography_DefaultAlgorithm_NotSupported);
	}
}
